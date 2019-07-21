using System.Globalization;

namespace Microsoft.Azure.Cosmos.Table
{
	public sealed class RetryContext
	{
		public StorageLocation NextLocation
		{
			get;
			private set;
		}

		public LocationMode LocationMode
		{
			get;
			private set;
		}

		public int CurrentRetryCount
		{
			get;
			private set;
		}

		public RequestResult LastRequestResult
		{
			get;
			private set;
		}

		internal RetryContext(int currentRetryCount, RequestResult lastRequestResult, StorageLocation nextLocation, LocationMode locationMode)
		{
			CurrentRetryCount = currentRetryCount;
			LastRequestResult = lastRequestResult;
			NextLocation = nextLocation;
			LocationMode = locationMode;
		}

		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "({0},{1})", new object[2]
			{
				CurrentRetryCount,
				LocationMode
			});
		}
	}
}
