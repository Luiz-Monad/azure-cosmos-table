namespace Microsoft.Azure.Cosmos.Table
{
	public sealed class TableContinuationToken
	{
		public string NextPartitionKey
		{
			get;
			set;
		}

		public string NextRowKey
		{
			get;
			set;
		}

		public string NextTableName
		{
			get;
			set;
		}

		public StorageLocation? TargetLocation
		{
			get;
			set;
		}
	}
}
