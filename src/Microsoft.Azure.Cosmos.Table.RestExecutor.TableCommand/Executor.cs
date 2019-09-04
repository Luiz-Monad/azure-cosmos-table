using Microsoft.Azure.Cosmos.Table.RestExecutor.Common;
using Microsoft.Azure.Cosmos.Table.RestExecutor.Utils;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.TableCommand
{
	internal class Executor
	{
		public static T ExecuteSync<T>(RESTCommand<T> cmd, IRetryPolicy policy, OperationContext operationContext)
		{
			using (new ExecutionState<T>(cmd, policy, operationContext))
			{
				return RestUtility.RunWithoutSynchronizationContext(() => ExecuteAsync(cmd, policy, operationContext, CancellationToken.None).GetAwaiter().GetResult());
			}
		}

		public static async Task<T> ExecuteAsync<T>(RESTCommand<T> cmd, IRetryPolicy policy, OperationContext operationContext, CancellationToken token)
		{
			using (ExecutionState<T> executionState = new ExecutionState<T>(cmd, policy, operationContext))
			{
				using (CancellationTokenSource timeoutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token))
				{
					CancellationToken timeoutToken = timeoutTokenSource.Token;
					bool shouldRetry = false;
					TimeSpan delay = TimeSpan.Zero;
					HttpClient client = cmd.HttpClient ?? HttpClientFactory.Instance;
					do
					{
						try
						{
							executionState.Init();
							ExecutionStateUtils.StartRequestAttempt(executionState);
							ProcessStartOfRequest(executionState, "Starting asynchronous request to {0}.", timeoutTokenSource);
							ExecutionStateUtils.CheckTimeout(executionState, throwOnTimeout: true);
						}
						catch (Exception ex)
						{
							Logger.LogError(executionState.OperationContext, "Exception thrown while initializing request: {0}.", ex.Message);
							StorageException ex2 = await ExecutionStateUtils.TranslateExceptionBasedOnParseErrorAsync(ex, executionState.Cmd.CurrentResult, executionState.Resp, executionState.Cmd.ParseErrorAsync, timeoutToken).ConfigureAwait(continueOnCapturedContext: false);
							ex2.IsRetryable = false;
							executionState.ExceptionRef = ex2;
							throw executionState.ExceptionRef;
						}
						try
						{
							executionState.CurrentOperation = ExecutorOperation.BeginGetResponse;
							Logger.LogInformational(executionState.OperationContext, "Waiting for response.");
							ExecutionState<T> executionState2 = executionState;
							executionState2.Resp = await client.SendAsync(executionState.Req, HttpCompletionOption.ResponseHeadersRead, timeoutToken).ConfigureAwait(continueOnCapturedContext: false);
							executionState.CurrentOperation = ExecutorOperation.EndGetResponse;
							if (!executionState.Resp.IsSuccessStatusCode)
							{
								executionState2 = executionState;
								executionState2.ExceptionRef = await StorageExceptionTranslator.PopulateStorageExceptionFromHttpResponseMessage(executionState.Resp, executionState.Cmd.CurrentResult, timeoutToken, executionState.Cmd.ParseErrorAsync).ConfigureAwait(continueOnCapturedContext: false);
							}
							Logger.LogInformational(executionState.OperationContext, "Response received. Status code = {0}, Request ID = {1}, Content-MD5 = {2}, ETag = {3}.", executionState.Cmd.CurrentResult.HttpStatusCode, executionState.Cmd.CurrentResult.ServiceRequestID, executionState.Cmd.CurrentResult.ContentMd5, executionState.Cmd.CurrentResult.Etag);
							ExecutionStateUtils.FireResponseReceived(executionState);
							if (cmd.PreProcessResponse != null)
							{
								executionState.CurrentOperation = ExecutorOperation.PreProcess;
								try
								{
									executionState.Result = cmd.PreProcessResponse(cmd, executionState.Resp, executionState.ExceptionRef, executionState.OperationContext);
									executionState.ExceptionRef = null;
								}
								catch (Exception exceptionRef)
								{
									executionState.ExceptionRef = exceptionRef;
								}
								Logger.LogInformational(executionState.OperationContext, "Response headers were processed successfully, proceeding with the rest of the operation.");
							}
							executionState.CurrentOperation = ExecutorOperation.GetResponseStream;
							cmd.ResponseStream = new MemoryStream();
							await(await executionState.Resp.Content.ReadAsStreamAsync()).WriteToAsync(cmd.ResponseStream, executionState, timeoutTokenSource.Token);
							cmd.ResponseStream.Position = 0L;
							if (executionState.ExceptionRef != null)
							{
								executionState.CurrentOperation = ExecutorOperation.BeginDownloadResponse;
								Logger.LogInformational(executionState.OperationContext, "Downloading error response body.");
								try
								{
									executionState2 = executionState;
									executionState2.ExceptionRef = await StorageExceptionTranslator.TranslateExceptionWithPreBufferedStreamAsync(executionState.ExceptionRef, executionState.Cmd.CurrentResult, cmd.ResponseStream, executionState.Resp, executionState.Cmd.ParseErrorAsync, timeoutToken);
									throw executionState.ExceptionRef;
								}
								finally
								{
									cmd.ResponseStream.Dispose();
									cmd.ResponseStream = null;
								}
							}
							await ProcessEndOfRequestAsync(executionState, timeoutToken).ConfigureAwait(continueOnCapturedContext: false);
							ExecutionStateUtils.FinishRequestAttempt(executionState);
							return executionState.Result;
						}
						catch (Exception ex3)
						{
							Exception ex4 = ex3;
							Logger.LogWarning(executionState.OperationContext, "Exception thrown during the operation: {0}.", ex4.Message);
							ExecutionStateUtils.FinishRequestAttempt(executionState);
							if (ex4 is OperationCanceledException && executionState.OperationExpiryTime.HasValue && DateTime.Now.CompareTo(executionState.OperationExpiryTime.Value) > 0)
							{
								ex4 = new TimeoutException("The client could not finish the operation within specified timeout.", ex4);
							}
							Exception ex6 = executionState.ExceptionRef = await ExecutionStateUtils.TranslateExceptionBasedOnParseErrorAsync(ex4, executionState.Cmd.CurrentResult, executionState.Resp, executionState.Cmd.ParseErrorAsync, timeoutToken).ConfigureAwait(continueOnCapturedContext: false);
							Logger.LogInformational(executionState.OperationContext, "Checking if the operation should be retried. Retry count = {0}, HTTP status code = {1}, Retryable exception = {2}, Exception = {3}.", executionState.RetryCount, executionState.Cmd.CurrentResult.HttpStatusCode, ((StorageException)ex6).IsRetryable ? "yes" : "no", ex6.Message);
							shouldRetry = false;
							if (((StorageException)ex6).IsRetryable && executionState.RetryPolicy != null)
							{
								executionState.CurrentLocation = ExecutionStateUtils.GetNextLocation(executionState.CurrentLocation, cmd.LocationMode);
								Logger.LogInformational(executionState.OperationContext, "The next location has been set to {0}, based on the location mode.", executionState.CurrentLocation);
								IExtendedRetryPolicy extendedRetryPolicy = executionState.RetryPolicy as IExtendedRetryPolicy;
								if (extendedRetryPolicy != null)
								{
									RetryInfo retryInfo = extendedRetryPolicy.Evaluate(new RetryContext(executionState.RetryCount++, cmd.CurrentResult, executionState.CurrentLocation, cmd.LocationMode), executionState.OperationContext);
									if (retryInfo != null)
									{
										Logger.LogInformational(executionState.OperationContext, "The extended retry policy set the next location to {0} and updated the location mode to {1}.", retryInfo.TargetLocation, retryInfo.UpdatedLocationMode);
										shouldRetry = true;
										executionState.CurrentLocation = retryInfo.TargetLocation;
										cmd.LocationMode = retryInfo.UpdatedLocationMode;
										delay = retryInfo.RetryInterval;
									}
								}
								else
								{
									shouldRetry = executionState.RetryPolicy.ShouldRetry(executionState.RetryCount++, cmd.CurrentResult.HttpStatusCode, executionState.ExceptionRef, out delay, executionState.OperationContext);
								}
								if (delay < TimeSpan.Zero || delay > TableRestConstants.MaximumRetryBackoff)
								{
									delay = TableRestConstants.MaximumRetryBackoff;
								}
							}
						}
						finally
						{
							if (executionState.Resp != null)
							{
								executionState.Resp.Dispose();
								executionState.Resp = null;
							}
						}
						if (!shouldRetry || (executionState.OperationExpiryTime.HasValue && (DateTime.Now + delay).CompareTo(executionState.OperationExpiryTime.Value) > 0))
						{
							Logger.LogError(executionState.OperationContext, shouldRetry ? "Operation cannot be retried because the maximum execution time has been reached. Failing with {0}." : "Retry policy did not allow for a retry. Failing with {0}.", executionState.ExceptionRef.Message);
							throw executionState.ExceptionRef;
						}
						cmd.RecoveryAction?.Invoke(cmd, executionState.Cmd.CurrentResult.Exception, executionState.OperationContext);
						if (delay > TimeSpan.Zero)
						{
							await Task.Delay(delay, timeoutToken).ConfigureAwait(continueOnCapturedContext: false);
						}
						Logger.LogInformational(executionState.OperationContext, "Retrying failed operation.");
						ExecutionStateUtils.FireRetrying(executionState);
					}
					while (shouldRetry);
					throw new NotImplementedException("Unexpected internal storage client error.");
				}
			}
		}

		private static void ProcessStartOfRequest<T>(ExecutionState<T> executionState, string startLogMessage, CancellationTokenSource timeoutTokenSource = null)
		{
			RESTCommand<T> restCMD = executionState.RestCMD;
			executionState.CurrentOperation = ExecutorOperation.BeginOperation;
			HttpContent arg = (restCMD.BuildContent != null) ? restCMD.BuildContent(restCMD, executionState.OperationContext) : null;
			Uri uri = restCMD.StorageUri.GetUri(executionState.CurrentLocation);
			Uri uri2 = restCMD.Credentials.TransformUri(uri);
			Logger.LogInformational(executionState.OperationContext, "Starting asynchronous request to {0}.", uri2);
			UriQueryBuilder arg2 = new UriQueryBuilder(executionState.RestCMD.Builder);
			executionState.Req = restCMD.BuildRequest(restCMD, uri2, arg2, arg, restCMD.ServerTimeoutInSeconds, executionState.OperationContext);
			ExecutionStateUtils.ApplyUserHeaders(executionState);
			ExecutionStateUtils.FireSendingRequest(executionState);
			if (executionState.OperationExpiryTime.HasValue)
			{
				timeoutTokenSource?.CancelAfter(executionState.RemainingTimeout);
			}
			else
			{
				timeoutTokenSource?.CancelAfter(int.MaxValue);
			}
		}

		private static void TryToForceStalledCancellation<T>(ExecutionState<T> executionState)
		{
			Logger.LogWarning(executionState.OperationContext, "Attempting to force a stalled cancellation.");
			if (executionState != null)
			{
				executionState.Resp?.Content?.Dispose();
			}
		}

		private static async Task ProcessEndOfRequestAsync<T>(ExecutionState<T> executionState, CancellationToken cancellationToken)
		{
			if (executionState.RestCMD.PostProcessResponseAsync != null)
			{
				executionState.CurrentOperation = ExecutorOperation.PostProcess;
				Logger.LogInformational(executionState.OperationContext, "Processing response body.");
				Task<T> task = executionState.RestCMD.PostProcessResponseAsync(executionState.RestCMD, executionState.Resp, executionState.OperationContext, cancellationToken);
				if (task.IsCompleted)
				{
					ExecutionState<T> executionState2 = executionState;
					executionState2.Result = await task;
				}
				else
				{
					using (CancellationTokenSource fallbackCancellation = new CancellationTokenSource())
					{
						using (cancellationToken.Register(delegate(object fcts)
						{
							((CancellationTokenSource)fcts).CancelAfter(TableRestConstants.ResponseParserCancellationFallbackDelay);
						}, fallbackCancellation))
						{
							try
							{
								using (fallbackCancellation.Token.Register(delegate(object s)
								{
									TryToForceStalledCancellation((ExecutionState<T>)s);
								}, executionState))
								{
									ExecutionState<T> executionState2 = executionState;
									executionState2.Result = await task.ConfigureAwait(continueOnCapturedContext: false);
								}
							}
							catch (Exception ex) when (fallbackCancellation.IsCancellationRequested && !(ex is OperationCanceledException))
							{
								Logger.LogWarning(executionState.OperationContext, $"Fallback cancellation induced {ex}");
								cancellationToken.ThrowIfCancellationRequested();
							}
						}
					}
				}
			}
			executionState.CurrentOperation = ExecutorOperation.EndOperation;
			Logger.LogInformational(executionState.OperationContext, "Operation completed successfully.");
			executionState.CancelDelegate = null;
		}
	}
}
