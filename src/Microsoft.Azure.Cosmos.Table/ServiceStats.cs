using System.Threading.Tasks;
using System.Xml.Linq;

namespace Microsoft.Azure.Cosmos.Table
{
	public sealed class ServiceStats
	{
		private const string StorageServiceStatsName = "StorageServiceStats";

		private const string GeoReplicationName = "GeoReplication";

		public GeoReplicationStats GeoReplication
		{
			get;
			private set;
		}

		private ServiceStats()
		{
		}

		internal static Task<ServiceStats> FromServiceXmlAsync(XDocument serviceStatsDocument)
		{
			XElement xElement = serviceStatsDocument.Element("StorageServiceStats");
			return Task.FromResult(new ServiceStats
			{
				GeoReplication = GeoReplicationStats.ReadGeoReplicationStatsFromXml(xElement.Element("GeoReplication"))
			});
		}
	}
}
