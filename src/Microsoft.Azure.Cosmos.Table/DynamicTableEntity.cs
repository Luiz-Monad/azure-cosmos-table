using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Cosmos.Table
{
	public sealed class DynamicTableEntity : ITableEntity
	{
		public IDictionary<string, EntityProperty> Properties
		{
			get;
			set;
		}

		public string PartitionKey
		{
			get;
			set;
		}

		public string RowKey
		{
			get;
			set;
		}

		public DateTimeOffset Timestamp
		{
			get;
			set;
		}

		public string ETag
		{
			get;
			set;
		}

		public EntityProperty this[string key]
		{
			get
			{
				return Properties[key];
			}
			set
			{
				Properties[key] = value;
			}
		}

		public DynamicTableEntity()
		{
			Properties = new Dictionary<string, EntityProperty>();
		}

		public DynamicTableEntity(string partitionKey, string rowKey)
			: this(partitionKey, rowKey, DateTimeOffset.MinValue, null, new Dictionary<string, EntityProperty>())
		{
		}

		public DynamicTableEntity(string partitionKey, string rowKey, string etag, IDictionary<string, EntityProperty> properties)
			: this(partitionKey, rowKey, DateTimeOffset.MinValue, etag, properties)
		{
		}

		internal DynamicTableEntity(string partitionKey, string rowKey, DateTimeOffset timestamp, string etag, IDictionary<string, EntityProperty> properties)
		{
			CommonUtility.AssertNotNull("properties", properties);
			PartitionKey = partitionKey;
			RowKey = rowKey;
			Timestamp = timestamp;
			ETag = etag;
			Properties = properties;
		}

		public void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
		{
			Properties = properties;
		}

		public IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
		{
			return Properties;
		}
	}
}
