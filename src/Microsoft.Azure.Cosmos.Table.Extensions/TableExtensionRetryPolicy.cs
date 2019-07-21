using Microsoft.Azure.Documents;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Cosmos.Table.Extensions
{
	internal static class TableExtensionRetryPolicy
	{
		internal static readonly TimeSpan MaximumRetryBackoff = TimeSpan.FromHours(1.0);

		public static Task<TResult> Execute<TResult>(Func<Task<TResult>> executionMethod, CancellationToken cancellationToken, OperationContext operationContext, TableRequestOptions requestOptions)
		{
			if (requestOptions != null && requestOptions.RetryPolicy != null)
			{
				return ExecuteUnderRetryPolicy(executionMethod, cancellationToken, operationContext, requestOptions);
			}
			return executionMethod();
		}

		private static async Task<TResult> ExecuteUnderRetryPolicy<TResult>(Func<Task<TResult>> executionMethod, CancellationToken cancellationToken, OperationContext operationContext, TableRequestOptions requestOptions)
		{
			IRetryPolicy retryPolicyInstance = null;
			int retryCount = 0;
			DateTime startTime = DateTime.UtcNow;
			StorageException previousException = null;
			while (true)
			{
				operationContext = (operationContext ?? new OperationContext());
				TimeSpan retryInterval;
				try
				{
					ThrowTimeoutIfElapsed(requestOptions.MaximumExecutionTime, startTime, operationContext, previousException);
					ThrowCancellationIfRequested(cancellationToken);
					return await executionMethod();
				}
				// catch (NotFoundException ex)
				// {
				// 	Logger.LogError(operationContext, "Retry policy did not allow for a retry. Failing with {0}.", ex.Message);
				// 	throw;
				// }
				catch (StorageException ex2)
				{
					if (retryPolicyInstance == null)
					{
						retryPolicyInstance = requestOptions.RetryPolicy.CreateInstance();
					}
					if (!ex2.IsRetryable)
					{
						Logger.LogError(operationContext, "Retry policy did not allow for a retry. Failing with {0}.", ex2.Message);
						throw;
					}
					int statusCode = (ex2.RequestInformation != null) ? ex2.RequestInformation.HttpStatusCode : 500;
					if (!retryPolicyInstance.ShouldRetry(retryCount++, statusCode, ex2, out retryInterval, operationContext))
					{
						Logger.LogError(operationContext, "Retry policy did not allow for a retry. Failing with {0}.", ex2.Message);
						throw;
					}
					previousException = ex2;
				}	
				catch (Exception ex)
				{
					if (ex.GetType().Name == "NotFoundException") 
					{
						Logger.LogError(operationContext, "Retry policy did not allow for a retry. Failing with {0}.", ex.Message);
						throw;
					}
				}
				if (retryInterval < TimeSpan.Zero || retryInterval > MaximumRetryBackoff)
				{
					retryInterval = MaximumRetryBackoff;
				}
				if (retryInterval != TimeSpan.Zero)
				{
					ThrowTimeoutIfElapsed(requestOptions.MaximumExecutionTime, startTime, operationContext, previousException);
					Logger.LogInformational(operationContext, "Operation will be retried after {0}ms.", (int)retryInterval.TotalMilliseconds);
					await Task.Delay(retryInterval);
				}
				operationContext?.FireRetrying(new RequestEventArgs(operationContext.LastResult));
			}
		}

		private static void ThrowTimeoutIfElapsed(TimeSpan? maxExecutionTime, DateTime startTime, OperationContext operationContext, StorageException previousException)
		{
			if (maxExecutionTime.HasValue && DateTime.UtcNow - startTime > maxExecutionTime.Value)
			{
				Logger.LogError(operationContext, "Operation cannot be retried because the maximum execution time has been reached. Failing with {0}.", "The client could not finish the operation within specified timeout.");
				if (previousException != null)
				{
					throw previousException;
				}
				RequestResult res = new RequestResult
				{
					HttpStatusCode = 408
				};
				TimeoutException ex = new TimeoutException("The client could not finish the operation within specified timeout.");
				throw new StorageException(res, ex.Message, ex)
				{
					IsRetryable = false
				};
			}
		}

		private static void ThrowCancellationIfRequested(CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				RequestResult res = new RequestResult
				{
					HttpStatusCode = 306,
					HttpStatusMessage = "Unused"
				};
				OperationCanceledException ex = new OperationCanceledException("Operation was canceled by user.", null);
				throw new StorageException(res, ex.Message, ex)
				{
					IsRetryable = false
				};
			}
		}
	}
}
