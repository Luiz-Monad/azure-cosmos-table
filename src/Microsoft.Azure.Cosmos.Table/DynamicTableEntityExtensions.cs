using Microsoft.Azure.Cosmos.Table.Protocol;
using Microsoft.Azure.Documents;
using Newtonsoft.Json;

namespace Microsoft.Azure.Cosmos.Table
{
	internal static class DynamicTableEntityExtensions
	{
		public static string GetCosmosTableName(this TableOperation operation)
		{
			return ((DynamicTableEntity)operation.Entity).Properties["TableName"].StringValue;
		}

		public static string GetCosmosTableName(this DynamicTableEntity tblEntity)
		{
			return tblEntity.Properties["TableName"].StringValue;
		}

		public static void SetCosmosTableName(this DynamicTableEntity tblEntity, string value)
		{
			tblEntity.Properties.Add("TableName", new EntityProperty(value));
		}

		public static int? GetCosmosTableThroughput(this DynamicTableEntity tblEntity)
		{
			int? result = null;
			if (tblEntity.Properties.ContainsKey(TableConstants.Throughput))
			{
				return tblEntity.Properties[TableConstants.Throughput].Int32Value;
			}
			return result;
		}

		public static void SetCosmosTableThroughput(this DynamicTableEntity tblEntity, int? throughput)
		{
			if (throughput.HasValue)
			{
				tblEntity.Properties.Add(TableConstants.Throughput, new EntityProperty(throughput.Value));
			}
		}

		public static void SetCosmosTableIndexingPolicy(this DynamicTableEntity tblEntity, string serializedIndexingPolicy)
		{
			if (!string.IsNullOrEmpty(serializedIndexingPolicy))
			{
				JsonConvert.DeserializeObject<IndexingPolicy>(serializedIndexingPolicy);
				tblEntity.Properties.Add(TableConstants.IndexingPolicy, new EntityProperty(serializedIndexingPolicy));
			}
		}

		public static IndexingPolicy GetCosmosTableIndexingPolicy(this DynamicTableEntity tblEntity)
		{
			if (tblEntity.Properties.ContainsKey(TableConstants.IndexingPolicy))
			{
				return JsonConvert.DeserializeObject<IndexingPolicy>(tblEntity.Properties[TableConstants.IndexingPolicy].StringValue);
			}
			return null;
		}
	}
}
