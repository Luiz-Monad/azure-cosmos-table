using Microsoft.Azure.Cosmos.Table.RestExecutor.Utils;
using System;
using System.Globalization;

namespace Microsoft.Azure.Cosmos.Table
{
	public sealed class RetryInfo
	{
		private TimeSpan interval = TimeSpan.FromSeconds(3.0);

		public StorageLocation TargetLocation
		{
			get;
			set;
		}

		public LocationMode UpdatedLocationMode
		{
			get;
			set;
		}

		public TimeSpan RetryInterval
		{
			get
			{
				return interval;
			}
			set
			{
				interval = RestUtility.MaxTimeSpan(value, TimeSpan.Zero);
			}
		}

		public RetryInfo()
		{
			TargetLocation = StorageLocation.Primary;
			UpdatedLocationMode = LocationMode.PrimaryOnly;
		}

		public RetryInfo(RetryContext retryContext)
		{
			CommonUtility.AssertNotNull("retryContext", retryContext);
			TargetLocation = retryContext.NextLocation;
			UpdatedLocationMode = retryContext.LocationMode;
		}

		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "({0},{1})", new object[2]
			{
				TargetLocation,
				RetryInterval
			});
		}
	}
}
