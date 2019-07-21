using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Cosmos.Table
{
	internal static class EntityUtilities
	{
		internal static TElement ResolveEntityByType<TElement>(string partitionKey, string rowKey, DateTimeOffset timestamp, IDictionary<string, EntityProperty> properties, string etag)
		{
			ITableEntity obj = (ITableEntity)InstantiateEntityFromType(typeof(TElement));
			obj.PartitionKey = partitionKey;
			obj.RowKey = rowKey;
			obj.Timestamp = timestamp;
			obj.ReadEntity(properties, null);
			obj.ETag = etag;
			return (TElement)obj;
		}

		internal static DynamicTableEntity ResolveDynamicEntity(string partitionKey, string rowKey, DateTimeOffset timestamp, IDictionary<string, EntityProperty> properties, string etag)
		{
			DynamicTableEntity dynamicTableEntity = new DynamicTableEntity(partitionKey, rowKey);
			dynamicTableEntity.Timestamp = timestamp;
			dynamicTableEntity.ReadEntity(properties, null);
			dynamicTableEntity.ETag = etag;
			return dynamicTableEntity;
		}

		internal static object InstantiateEntityFromType(Type type)
		{
			return Activator.CreateInstance(type);
		}
	}
}
