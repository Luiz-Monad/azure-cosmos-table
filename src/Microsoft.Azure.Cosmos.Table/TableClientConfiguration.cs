namespace Microsoft.Azure.Cosmos.Table
{
	public class TableClientConfiguration
	{
		public CosmosExecutorConfiguration CosmosExecutorConfiguration
		{
			get;
			set;
		}

		public RestExecutorConfiguration RestExecutorConfiguration
		{
			get;
			set;
		}

		public TableClientConfiguration()
		{
			CosmosExecutorConfiguration = new CosmosExecutorConfiguration();
			RestExecutorConfiguration = new RestExecutorConfiguration();
		}
	}
}
