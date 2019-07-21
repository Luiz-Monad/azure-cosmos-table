using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Internals;

namespace Microsoft.Azure.Documents.Client.Internals
{
	/// <summary>
	/// The helper function relates to the async Task.
	/// </summary>
	internal static class TaskHelper
	{
		public static Task InlineIfPossible(Func<Task> function, IRetryPolicy retryPolicy, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (SynchronizationContext.Current == null)
			{
				if (retryPolicy == null)
				{
					return function();
				}
				return BackoffRetryUtility<int>.ExecuteAsync(async delegate
				{
					await function();
					return 0;
				}, retryPolicy, cancellationToken);
			}
			if (retryPolicy == null)
			{
				return Task.Run(function);
			}
			return Task.Run(() => BackoffRetryUtility<int>.ExecuteAsync(async delegate
			{
				await function();
				return 0;
			}, retryPolicy, cancellationToken));
		}

		public static Task<TResult> InlineIfPossible<TResult>(Func<Task<TResult>> function, IRetryPolicy retryPolicy, 
			CancellationToken cancellationToken = default(CancellationToken))
		{
			if (SynchronizationContext.Current == null)
			{
				if (retryPolicy == null)
				{
					return function();
				}
				return BackoffRetryUtility<TResult>.ExecuteAsync(() => function(), retryPolicy, cancellationToken);
			}
			if (retryPolicy == null)
			{
				return Task.Run(function);
			}
			return Task.Run(() => BackoffRetryUtility<TResult>.ExecuteAsync(() => function(), retryPolicy, cancellationToken));
		}
	}
}
