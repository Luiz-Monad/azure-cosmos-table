using Microsoft.Azure.Cosmos.Table.RestExecutor.Common;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.TableCommand
{
	internal static class ExecutionStateUtils
	{
		internal static void ApplyUserHeaders<T>(ExecutionState<T> executionState)
		{
			if (!string.IsNullOrEmpty(executionState.OperationContext.ClientRequestID))
			{
				executionState.Req.Headers.Add("x-ms-client-request-id", executionState.OperationContext.ClientRequestID);
			}
			if (!string.IsNullOrEmpty(executionState.OperationContext.CustomUserAgent))
			{
				executionState.Req.Headers.UserAgent.TryParseAdd(executionState.OperationContext.CustomUserAgent);
				executionState.Req.Headers.UserAgent.Add(new ProductInfoHeaderValue("Azure-Cosmos-Table", "1.0.2"));
				executionState.Req.Headers.UserAgent.Add(new ProductInfoHeaderValue(TableRestConstants.HeaderConstants.UserAgentComment));
			}
			if (executionState.OperationContext.UserHeaders != null && executionState.OperationContext.UserHeaders.Count > 0)
			{
				foreach (string key in executionState.OperationContext.UserHeaders.Keys)
				{
					executionState.Req.Headers.Add(key, executionState.OperationContext.UserHeaders[key]);
				}
			}
		}

		internal static void StartRequestAttempt<T>(ExecutionState<T> executionState)
		{
			executionState.ExceptionRef = null;
			executionState.Cmd.CurrentResult = new RequestResult
			{
				StartTime = DateTime.Now
			};
			lock (executionState.OperationContext.RequestResults)
			{
				executionState.OperationContext.RequestResults.Add(executionState.Cmd.CurrentResult);
				executionState.Cmd.RequestResults.Add(executionState.Cmd.CurrentResult);
			}
			RESTCommand<T> restCMD = executionState.RestCMD;
			if (restCMD != null)
			{
				if (!restCMD.StorageUri.ValidateLocationMode(restCMD.LocationMode))
				{
					throw new InvalidOperationException("The Uri for the target storage location is not specified. Please consider changing the request's location mode.");
				}
				switch (restCMD.CommandLocationMode)
				{
				case CommandLocationMode.PrimaryOnly:
					if (restCMD.LocationMode == LocationMode.SecondaryOnly)
					{
						throw new InvalidOperationException("This operation can only be executed against the primary storage location.");
					}
					Logger.LogInformational(executionState.OperationContext, "This operation can only be executed against the primary storage location.");
					executionState.CurrentLocation = StorageLocation.Primary;
					restCMD.LocationMode = LocationMode.PrimaryOnly;
					break;
				case CommandLocationMode.SecondaryOnly:
					if (restCMD.LocationMode == LocationMode.PrimaryOnly)
					{
						throw new InvalidOperationException("This operation can only be executed against the secondary storage location.");
					}
					Logger.LogInformational(executionState.OperationContext, "This operation can only be executed against the secondary storage location.");
					executionState.CurrentLocation = StorageLocation.Secondary;
					restCMD.LocationMode = LocationMode.SecondaryOnly;
					break;
				}
			}
			executionState.Cmd.CurrentResult.TargetLocation = executionState.CurrentLocation;
		}

		internal static StorageLocation GetNextLocation(StorageLocation lastLocation, LocationMode locationMode)
		{
			switch (locationMode)
			{
			case LocationMode.PrimaryOnly:
				return StorageLocation.Primary;
			case LocationMode.PrimaryThenSecondary:
			case LocationMode.SecondaryThenPrimary:
				if (lastLocation == StorageLocation.Primary)
				{
					return StorageLocation.Secondary;
				}
				return StorageLocation.Primary;
			case LocationMode.SecondaryOnly:
				return StorageLocation.Secondary;
			default:
				CommonUtility.ArgumentOutOfRange("LocationMode", locationMode);
				return StorageLocation.Primary;
			}
		}

		internal static void FinishRequestAttempt<T>(ExecutionState<T> executionState)
		{
			executionState.Cmd.CurrentResult.EndTime = DateTime.Now;
			executionState.OperationContext.EndTime = DateTime.Now;
			FireRequestCompleted(executionState);
		}

		internal static void FireSendingRequest<T>(ExecutionState<T> executionState)
		{
			RequestEventArgs args = GenerateRequestEventArgs(executionState);
			executionState.OperationContext.FireSendingRequest(args);
		}

		internal static void FireResponseReceived<T>(ExecutionState<T> executionState)
		{
			RequestEventArgs args = GenerateRequestEventArgs(executionState);
			executionState.OperationContext.FireResponseReceived(args);
		}

		internal static void FireRequestCompleted<T>(ExecutionState<T> executionState)
		{
			RequestEventArgs args = GenerateRequestEventArgs(executionState);
			executionState.OperationContext.FireRequestCompleted(args);
		}

		internal static void FireRetrying<T>(ExecutionState<T> executionState)
		{
			RequestEventArgs args = GenerateRequestEventArgs(executionState);
			executionState.OperationContext.FireRetrying(args);
		}

		private static RequestEventArgs GenerateRequestEventArgs<T>(ExecutionState<T> executionState)
		{
			return new RequestEventArgs(executionState.Cmd.CurrentResult)
			{
				Request = executionState.Req,
				Response = executionState.Resp
			};
		}

		internal static bool CheckTimeout<T>(ExecutionState<T> executionState, bool throwOnTimeout)
		{
			if (!executionState.ReqTimedOut && (!executionState.OperationExpiryTime.HasValue || executionState.Cmd.CurrentResult.StartTime.CompareTo(executionState.OperationExpiryTime.Value) <= 0))
			{
				return false;
			}
			executionState.ReqTimedOut = true;
			StorageException ex2 = (StorageException)(executionState.ExceptionRef = StorageExceptionTranslator.GenerateTimeoutException(executionState.Cmd.CurrentResult, null));
			if (throwOnTimeout)
			{
				throw executionState.ExceptionRef;
			}
			return true;
		}

		internal static async Task<StorageException> TranslateExceptionBasedOnParseErrorAsync(Exception ex, RequestResult currentResult, HttpResponseMessage response, Func<Stream, HttpResponseMessage, string, CancellationToken, Task<StorageExtendedErrorInformation>> parseErrorAsync, CancellationToken cancellationToken)
		{
			if (parseErrorAsync != null)
			{
				return await StorageExceptionTranslator.TranslateExceptionAsync(ex, currentResult, async (Stream stream, CancellationToken token) => await parseErrorAsync(stream, response, null, token).ConfigureAwait(continueOnCapturedContext: false), cancellationToken, response).ConfigureAwait(continueOnCapturedContext: false);
			}
			return await StorageExceptionTranslator.TranslateExceptionAsync(ex, currentResult, null, cancellationToken, response).ConfigureAwait(continueOnCapturedContext: false);
		}
	}
}
