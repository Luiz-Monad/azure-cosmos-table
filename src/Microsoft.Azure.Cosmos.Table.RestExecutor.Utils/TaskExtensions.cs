using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.Utils
{
	internal static class TaskExtensions
	{
		internal static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
		{
			TaskCompletionSource<bool> taskCompletionSource2 = new TaskCompletionSource<bool>();
			using (cancellationToken.Register(delegate(object taskCompletionSource)
			{
				((TaskCompletionSource<bool>)taskCompletionSource).TrySetResult(result: true);
			}, taskCompletionSource2))
			{
				if (task != await Task.WhenAny(task, taskCompletionSource2.Task).ConfigureAwait(continueOnCapturedContext: false))
				{
					throw new OperationCanceledException(cancellationToken);
				}
			}
			return await task.ConfigureAwait(continueOnCapturedContext: false);
		}

		internal static async Task WithCancellation(this Task task, CancellationToken cancellationToken)
		{
			TaskCompletionSource<bool> taskCompletionSource2 = new TaskCompletionSource<bool>();
			using (cancellationToken.Register(delegate(object taskCompletionSource)
			{
				((TaskCompletionSource<bool>)taskCompletionSource).TrySetResult(result: true);
			}, taskCompletionSource2))
			{
				if (task != await Task.WhenAny(task, taskCompletionSource2.Task).ConfigureAwait(continueOnCapturedContext: false))
				{
					throw new OperationCanceledException(cancellationToken);
				}
			}
			await task.ConfigureAwait(continueOnCapturedContext: false);
		}
	}
}
