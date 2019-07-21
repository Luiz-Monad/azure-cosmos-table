using System;

namespace Microsoft.Azure.Cosmos.Table
{
	public sealed class NoRetry : IRetryPolicy
	{
		public bool ShouldRetry(int currentRetryCount, int statusCode, Exception lastException, out TimeSpan retryInterval, OperationContext operationContext)
		{
			retryInterval = TimeSpan.Zero;
			return false;
		}

		public IRetryPolicy CreateInstance()
		{
			return new NoRetry();
		}
	}
}
