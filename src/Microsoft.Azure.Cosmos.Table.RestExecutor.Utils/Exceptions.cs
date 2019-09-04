using System;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.Utils
{
	internal static class Exceptions
	{
		internal static StorageException GenerateTimeoutException(RequestResult res, Exception inner)
		{
			if (res != null)
			{
				res.HttpStatusCode = 408;
			}
			TimeoutException ex = new TimeoutException("The client could not finish the operation within specified timeout.", inner);
			return new StorageException(res, ex.Message, ex)
			{
				IsRetryable = false
			};
		}

		internal static StorageException GenerateCancellationException(RequestResult res, Exception inner)
		{
			if (res != null)
			{
				res.HttpStatusCode = 306;
				res.HttpStatusMessage = "Unused";
			}
			OperationCanceledException ex = new OperationCanceledException("Operation was canceled by user.", inner);
			return new StorageException(res, ex.Message, ex)
			{
				IsRetryable = false
			};
		}
	}
}
