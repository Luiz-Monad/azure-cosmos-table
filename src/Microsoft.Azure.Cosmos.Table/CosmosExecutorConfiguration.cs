using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace Microsoft.Azure.Cosmos.Table
{
	using ConnectionProtocol = Microsoft.Azure.Documents.Client.Protocol;

	public class CosmosExecutorConfiguration
	{
		public bool UseConnectionModeDirect
		{
			get;
			set;
		} = true;


		public string UserAgentSuffix
		{
			get;
			set;
		}

		public string CurrentRegion
		{
			get;
			set;
		}

		public int MaxConnectionLimit
		{
			get;
			set;
		} = 50;


		public int? MaxRetryAttemptsOnThrottledRequests
		{
			get;
			set;
		}

		public int? MaxRetryWaitTimeOnThrottledRequests
		{
			get;
			set;
		}

		public ConsistencyLevel? ConsistencyLevel
		{
			get;
			set;
		}

		internal ConnectionPolicy GetConnectionPolicy()
		{
			ConnectionPolicy connectionPolicy = new ConnectionPolicy
			{
				EnableEndpointDiscovery = true,
				UseMultipleWriteLocations = true,
				UserAgentSuffix = string.Format(" {0}/{1} {2}", "cosmos-table-sdk", "1.0.4", UserAgentSuffix)
			};
			if (UseConnectionModeDirect)
			{
				connectionPolicy.ConnectionMode = ConnectionMode.Direct;
				connectionPolicy.ConnectionProtocol = Microsoft.Azure.Documents.Client.Protocol.Tcp;
			}
			else
			{
				connectionPolicy.ConnectionMode = ConnectionMode.Gateway;
				connectionPolicy.ConnectionProtocol = Microsoft.Azure.Documents.Client.Protocol.Https;
				MaxConnectionLimit = MaxConnectionLimit;
			}
			if (!string.IsNullOrEmpty(CurrentRegion))
			{
				connectionPolicy.SetCurrentLocation(CurrentRegion);
			}
			if (MaxRetryAttemptsOnThrottledRequests.HasValue)
			{
				connectionPolicy.RetryOptions.MaxRetryAttemptsOnThrottledRequests = MaxRetryAttemptsOnThrottledRequests.Value;
			}
			if (MaxRetryWaitTimeOnThrottledRequests.HasValue)
			{
				connectionPolicy.RetryOptions.MaxRetryWaitTimeInSeconds = MaxRetryWaitTimeOnThrottledRequests.Value;
			}
			return connectionPolicy;
		}
	}
}
