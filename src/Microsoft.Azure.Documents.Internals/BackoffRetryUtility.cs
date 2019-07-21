using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Internals
{
	internal static class BackoffRetryUtility<T>
	{
		public const string ExceptionSourceToIgnoreForIgnoreForRetry = "BackoffRetryUtility";

		public static Task<T> ExecuteAsync(Func<Task<T>> callbackMethod, IRetryPolicy retryPolicy, CancellationToken cancellationToken = default(CancellationToken), Action<Exception> preRetryCallback = null)
		{
			return ExecuteRetryAsync(() => callbackMethod(), (Exception exception, CancellationToken token) => retryPolicy.ShouldRetryAsync(exception, cancellationToken), null, TimeSpan.Zero, cancellationToken, preRetryCallback);
		}

		public static Task<T> ExecuteAsync<TPolicyArg1>(Func<TPolicyArg1, Task<T>> callbackMethod, IRetryPolicy<TPolicyArg1> retryPolicy, CancellationToken cancellationToken = default(CancellationToken), Action<Exception> preRetryCallback = null)
		{
			TPolicyArg1 policyArg = (TPolicyArg1)retryPolicy.InitialArgumentValue;
			return ExecuteRetryAsync(() => callbackMethod((TPolicyArg1)policyArg), async delegate(Exception exception, CancellationToken token)
			{
				ShouldRetryResult<TPolicyArg1> shouldRetryResult = await retryPolicy.ShouldRetryAsync(exception, cancellationToken);
				policyArg = (TPolicyArg1)shouldRetryResult.PolicyArg1;
				return shouldRetryResult;
			}, null, TimeSpan.Zero, cancellationToken, preRetryCallback);
		}

		public static Task<T> ExecuteAsync(Func<Task<T>> callbackMethod, IRetryPolicy retryPolicy, Func<Task<T>> inBackoffAlternateCallbackMethod, TimeSpan minBackoffForInBackoffCallback, CancellationToken cancellationToken = default(CancellationToken), Action<Exception> preRetryCallback = null)
		{
			Func<Task<T>> inBackoffAlternateCallbackMethod2 = null;
			if (inBackoffAlternateCallbackMethod != null)
			{
				inBackoffAlternateCallbackMethod2 = (() => inBackoffAlternateCallbackMethod());
			}
			return ExecuteRetryAsync(() => callbackMethod(), (Exception exception, CancellationToken token) => retryPolicy.ShouldRetryAsync(exception, cancellationToken), inBackoffAlternateCallbackMethod2, minBackoffForInBackoffCallback, cancellationToken, preRetryCallback);
		}

		public static Task<T> ExecuteAsync<TPolicyArg1>(Func<TPolicyArg1, Task<T>> callbackMethod, IRetryPolicy<TPolicyArg1> retryPolicy, Func<TPolicyArg1, Task<T>> inBackoffAlternateCallbackMethod, TimeSpan minBackoffForInBackoffCallback, CancellationToken cancellationToken = default(CancellationToken), Action<Exception> preRetryCallback = null)
		{
			TPolicyArg1 policyArg = (TPolicyArg1)retryPolicy.InitialArgumentValue;
			Func<Task<T>> inBackoffAlternateCallbackMethod2 = null;
			if (inBackoffAlternateCallbackMethod != null)
			{
				inBackoffAlternateCallbackMethod2 = (() => inBackoffAlternateCallbackMethod((TPolicyArg1)policyArg));
			}
			return ExecuteRetryAsync(() => callbackMethod((TPolicyArg1)policyArg), async delegate(Exception exception, CancellationToken token)
			{
				ShouldRetryResult<TPolicyArg1> shouldRetryResult = await retryPolicy.ShouldRetryAsync(exception, cancellationToken);
				policyArg = (TPolicyArg1)shouldRetryResult.PolicyArg1;
				return shouldRetryResult;
			}, inBackoffAlternateCallbackMethod2, minBackoffForInBackoffCallback, cancellationToken, preRetryCallback);
		}

		private static async Task<T> ExecuteRetryAsync(Func<Task<T>> callbackMethod, Func<Exception, CancellationToken, Task<ShouldRetryResult>> callShouldRetry, Func<Task<T>> inBackoffAlternateCallbackMethod, TimeSpan minBackoffForInBackoffCallback, CancellationToken cancellationToken, Action<Exception> preRetryCallback = null)
		{
			while (true)
			{
				ExceptionDispatchInfo exception;
				try
				{
					cancellationToken.ThrowIfCancellationRequested();
					return await callbackMethod();
				}
				catch (Exception source)
				{
					exception = ExceptionDispatchInfo.Capture(source);
				}
				cancellationToken.ThrowIfCancellationRequested();
				ShouldRetryResult result = await callShouldRetry(exception.SourceException, cancellationToken);
				result.ThrowIfDoneTrying(exception);
				TimeSpan timeSpan = result.BackoffTime;
				if (inBackoffAlternateCallbackMethod != null && result.BackoffTime >= minBackoffForInBackoffCallback)
				{
					Stopwatch stopwatch = new Stopwatch();
					try
					{
						stopwatch.Start();
						return await inBackoffAlternateCallbackMethod();
					}
					catch (Exception ex)
					{
						stopwatch.Stop();
						DefaultTrace.TraceInformation("Failed inBackoffAlternateCallback with {0}, proceeding with retry. Time taken: {1}ms", ex.ToString(), stopwatch.ElapsedMilliseconds);
					}
					timeSpan = ((result.BackoffTime > stopwatch.Elapsed) ? (result.BackoffTime - stopwatch.Elapsed) : TimeSpan.Zero);
				}
				preRetryCallback?.Invoke(exception.SourceException);
				if (timeSpan != TimeSpan.Zero)
				{
					await Task.Delay(timeSpan, cancellationToken);
				}
			}
		}
	}
}
