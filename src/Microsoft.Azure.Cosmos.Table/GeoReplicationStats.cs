using System;
using System.Globalization;
using System.Xml.Linq;

namespace Microsoft.Azure.Cosmos.Table
{
	public sealed class GeoReplicationStats
	{
		private const string StatusName = "Status";

		private const string LastSyncTimeName = "LastSyncTime";

		public GeoReplicationStatus Status
		{
			get;
			private set;
		}

		public DateTimeOffset? LastSyncTime
		{
			get;
			private set;
		}

		private GeoReplicationStats()
		{
		}

		internal static GeoReplicationStatus GetGeoReplicationStatus(string geoReplicationStatus)
		{
			if (!(geoReplicationStatus == "unavailable"))
			{
				if (!(geoReplicationStatus == "live"))
				{
					if (geoReplicationStatus == "bootstrap")
					{
						return GeoReplicationStatus.Bootstrap;
					}
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Invalid geo-replication status in response: '{0}'", new object[1]
					{
						geoReplicationStatus
					}), "geoReplicationStatus");
				}
				return GeoReplicationStatus.Live;
			}
			return GeoReplicationStatus.Unavailable;
		}

		internal static GeoReplicationStats ReadGeoReplicationStatsFromXml(XElement element)
		{
			string value = element.Element("LastSyncTime").Value;
			return new GeoReplicationStats
			{
				Status = GetGeoReplicationStatus(element.Element("Status").Value),
				LastSyncTime = (string.IsNullOrEmpty(value) ? null : new DateTimeOffset?(DateTimeOffset.Parse(value, CultureInfo.InvariantCulture)))
			};
		}
	}
}
