using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Cosmos.Table
{
	public sealed class TableOperation
	{
		private Func<string, string, DateTimeOffset, IDictionary<string, EntityProperty>, string, object> retrieveResolver;

		public ITableEntity Entity
		{
			get;
			private set;
		}

		public TableOperationType OperationType
		{
			get;
			private set;
		}

		internal bool IsTableEntity
		{
			get;
			set;
		}

		internal bool IsPrimaryOnlyRetrieve
		{
			get;
			set;
		}

		internal string RetrievePartitionKey
		{
			get;
			set;
		}

		internal string RetrieveRowKey
		{
			get;
			set;
		}

		internal string PartitionKey
		{
			get
			{
				TableOperationType operationType = OperationType;
				if (operationType == TableOperationType.Retrieve)
				{
					return RetrievePartitionKey;
				}
				return Entity.PartitionKey;
			}
		}

		internal string RowKey
		{
			get
			{
				TableOperationType operationType = OperationType;
				if (operationType == TableOperationType.Retrieve)
				{
					return RetrieveRowKey;
				}
				return Entity.RowKey;
			}
		}

		internal string ETag
		{
			get
			{
				TableOperationType operationType = OperationType;
				return Entity.ETag;
			}
		}

		internal Func<string, string, DateTimeOffset, IDictionary<string, EntityProperty>, string, object> RetrieveResolver
		{
			get
			{
				if (retrieveResolver == null)
				{
					retrieveResolver = DynamicEntityResolver;
				}
				return retrieveResolver;
			}
			set
			{
				retrieveResolver = value;
			}
		}

		internal Type PropertyResolverType
		{
			get;
			set;
		}

		internal bool EchoContent
		{
			get;
			set;
		}

		internal List<string> SelectColumns
		{
			get;
			set;
		}

		internal TableOperation(ITableEntity entity, TableOperationType operationType)
			: this(entity, operationType, echoContent: true)
		{
		}

		internal TableOperation(ITableEntity entity, TableOperationType operationType, bool echoContent)
		{
			if (entity == null && operationType != TableOperationType.Retrieve)
			{
				throw new ArgumentNullException("entity");
			}
			Entity = entity;
			OperationType = operationType;
			EchoContent = echoContent;
		}

		public static TableOperation Delete(ITableEntity entity)
		{
			CommonUtility.AssertNotNull("entity", entity);
			if (string.IsNullOrEmpty(entity.ETag))
			{
				throw new ArgumentException("Delete requires an ETag (which may be the '*' wildcard).");
			}
			return new TableOperation(entity, TableOperationType.Delete);
		}

		public static TableOperation Insert(ITableEntity entity)
		{
			return Insert(entity, echoContent: false);
		}

		public static TableOperation Insert(ITableEntity entity, bool echoContent)
		{
			CommonUtility.AssertNotNull("entity", entity);
			return new TableOperation(entity, TableOperationType.Insert, echoContent);
		}

		public static TableOperation InsertOrMerge(ITableEntity entity)
		{
			CommonUtility.AssertNotNull("entity", entity);
			return new TableOperation(entity, TableOperationType.InsertOrMerge);
		}

		public static TableOperation InsertOrReplace(ITableEntity entity)
		{
			CommonUtility.AssertNotNull("entity", entity);
			return new TableOperation(entity, TableOperationType.InsertOrReplace);
		}

		public static TableOperation Merge(ITableEntity entity)
		{
			CommonUtility.AssertNotNull("entity", entity);
			if (string.IsNullOrEmpty(entity.ETag))
			{
				throw new ArgumentException("Merge requires an ETag (which may be the '*' wildcard).");
			}
			return new TableOperation(entity, TableOperationType.Merge);
		}

		public static TableOperation Replace(ITableEntity entity)
		{
			CommonUtility.AssertNotNull("entity", entity);
			if (string.IsNullOrEmpty(entity.ETag))
			{
				throw new ArgumentException("Replace requires an ETag (which may be the '*' wildcard).");
			}
			return new TableOperation(entity, TableOperationType.Replace);
		}

		public static TableOperation Retrieve<TElement>(string partitionKey, string rowkey, List<string> selectColumns = null) where TElement : ITableEntity
		{
			CommonUtility.AssertNotNull("partitionKey", partitionKey);
			CommonUtility.AssertNotNull("rowkey", rowkey);
			return new TableOperation(null, TableOperationType.Retrieve)
			{
				RetrievePartitionKey = partitionKey,
				RetrieveRowKey = rowkey,
				SelectColumns = selectColumns,
				RetrieveResolver = ((string pk, string rk, DateTimeOffset ts, IDictionary<string, EntityProperty> prop, string etag) => EntityUtilities.ResolveEntityByType<TElement>(pk, rk, ts, prop, etag)),
				PropertyResolverType = typeof(TElement)
			};
		}

		public static TableOperation Retrieve<TResult>(string partitionKey, string rowkey, EntityResolver<TResult> resolver, List<string> selectedColumns = null)
		{
			CommonUtility.AssertNotNull("partitionKey", partitionKey);
			CommonUtility.AssertNotNull("rowkey", rowkey);
			return new TableOperation(null, TableOperationType.Retrieve)
			{
				RetrievePartitionKey = partitionKey,
				RetrieveRowKey = rowkey,
				RetrieveResolver = ((string pk, string rk, DateTimeOffset ts, IDictionary<string, EntityProperty> prop, string etag) => resolver(pk, rk, ts, prop, etag)),
				SelectColumns = selectedColumns
			};
		}

		public static TableOperation Retrieve(string partitionKey, string rowkey, List<string> selectedColumns = null)
		{
			CommonUtility.AssertNotNull("partitionKey", partitionKey);
			CommonUtility.AssertNotNull("rowkey", rowkey);
			return new TableOperation(null, TableOperationType.Retrieve)
			{
				RetrievePartitionKey = partitionKey,
				RetrieveRowKey = rowkey,
				SelectColumns = selectedColumns
			};
		}

		internal TableResult Execute(CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext)
		{
			TableRequestOptions requestOptions2 = TableRequestOptions.ApplyDefaults(requestOptions, client);
			operationContext = (operationContext ?? new OperationContext());
			CommonUtility.AssertNotNullOrEmpty("tableName", table.Name);
			return client.Executor.ExecuteTableOperation<TableResult>(this, client, table, requestOptions2, operationContext);
		}

		internal Task<TableResult> ExecuteAsync(CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			TableRequestOptions requestOptions2 = TableRequestOptions.ApplyDefaults(requestOptions, client);
			operationContext = (operationContext ?? new OperationContext());
			CommonUtility.AssertNotNullOrEmpty("tableName", table.Name);
			return client.Executor.ExecuteTableOperationAsync<TableResult>(this, client, table, requestOptions2, operationContext, cancellationToken);
		}

		private static object DynamicEntityResolver(string partitionKey, string rowKey, DateTimeOffset timestamp, IDictionary<string, EntityProperty> properties, string etag)
		{
			DynamicTableEntity dynamicTableEntity = new DynamicTableEntity();
			((ITableEntity)dynamicTableEntity).PartitionKey = partitionKey;
			((ITableEntity)dynamicTableEntity).RowKey = rowKey;
			((ITableEntity)dynamicTableEntity).Timestamp = timestamp;
			((ITableEntity)dynamicTableEntity).ReadEntity(properties, (OperationContext)null);
			((ITableEntity)dynamicTableEntity).ETag = etag;
			return dynamicTableEntity;
		}
	}
}
