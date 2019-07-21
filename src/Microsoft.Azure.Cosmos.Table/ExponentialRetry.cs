using System;

namespace Microsoft.Azure.Cosmos.Table
{
	public sealed class ExponentialRetry : IExtendedRetryPolicy, IRetryPolicy
	{
		private const int DefaultClientRetryCount = 3;

		private static readonly TimeSpan DefaultClientBackoff = TimeSpan.FromSeconds(4.0);

		private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(120.0);

		private static readonly TimeSpan MinBackoff = TimeSpan.FromSeconds(3.0);

		private TimeSpan deltaBackoff;

		private int maximumAttempts;

		private DateTimeOffset? lastPrimaryAttempt;

		private DateTimeOffset? lastSecondaryAttempt;

		public ExponentialRetry()
			: this(DefaultClientBackoff, 3)
		{
		}

		public ExponentialRetry(TimeSpan deltaBackoff, int maxAttempts)
		{
			this.deltaBackoff = deltaBackoff;
			maximumAttempts = maxAttempts;
		}

		public bool ShouldRetry(int currentRetryCount, int statusCode, Exception lastException, out TimeSpan retryInterval, OperationContext operationContext)
		{
			CommonUtility.AssertNotNull("lastException", lastException);
			retryInterval = TimeSpan.Zero;
			if ((statusCode >= 300 && statusCode < 500 && statusCode != 408) || statusCode == 501 || statusCode == 505 || lastException.Message == "Blob type of the blob reference doesn't match blob type of the blob.")
			{
				return false;
			}
			if (currentRetryCount < maximumAttempts)
			{
				Random random = new Random();
				double num = (Math.Pow(2.0, currentRetryCount) - 1.0) * (double)random.Next((int)(deltaBackoff.TotalMilliseconds * 0.8), (int)(deltaBackoff.TotalMilliseconds * 1.2));
				retryInterval = ((num < 0.0) ? MaxBackoff : TimeSpan.FromMilliseconds(Math.Min(MaxBackoff.TotalMilliseconds, MinBackoff.TotalMilliseconds + num)));
				return true;
			}
			return false;
		}

		public RetryInfo Evaluate(RetryContext retryContext, OperationContext operationContext)
		{
			CommonUtility.AssertNotNull("retryContext", retryContext);
			if (retryContext.LastRequestResult.TargetLocation == StorageLocation.Primary)
			{
				lastPrimaryAttempt = retryContext.LastRequestResult.EndTime;
			}
			else
			{
				lastSecondaryAttempt = retryContext.LastRequestResult.EndTime;
			}
			bool flag = retryContext.LastRequestResult.TargetLocation == StorageLocation.Secondary && retryContext.LastRequestResult.HttpStatusCode == 404;
			if (ShouldRetry(retryContext.CurrentRetryCount, flag ? 500 : retryContext.LastRequestResult.HttpStatusCode, retryContext.LastRequestResult.Exception, out TimeSpan retryInterval, operationContext))
			{
				RetryInfo retryInfo = new RetryInfo(retryContext);
				if (flag && retryContext.LocationMode != LocationMode.SecondaryOnly)
				{
					retryInfo.UpdatedLocationMode = LocationMode.PrimaryOnly;
					retryInfo.TargetLocation = StorageLocation.Primary;
				}
				DateTimeOffset? dateTimeOffset = (retryInfo.TargetLocation == StorageLocation.Primary) ? lastPrimaryAttempt : lastSecondaryAttempt;
				if (dateTimeOffset.HasValue)
				{
					TimeSpan t = CommonUtility.MaxTimeSpan(DateTimeOffset.Now - dateTimeOffset.Value, TimeSpan.Zero);
					retryInfo.RetryInterval = retryInterval - t;
				}
				else
				{
					retryInfo.RetryInterval = TimeSpan.Zero;
				}
				return retryInfo;
			}
			return null;
		}

		public IRetryPolicy CreateInstance()
		{
			return new ExponentialRetry(deltaBackoff, maximumAttempts);
		}
	}
}
