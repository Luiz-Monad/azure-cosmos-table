using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Cosmos.Table
{
	public sealed class TableBatchOperation : IList<TableOperation>, ICollection<TableOperation>, IEnumerable<TableOperation>, IEnumerable
	{
		private bool hasQuery;

		private List<TableOperation> operations = new List<TableOperation>();

		internal string batchPartitionKey;

		internal bool ContainsWrites
		{
			get;
			private set;
		}

		public int Count => operations.Count;

		public bool IsReadOnly => false;

		public TableOperation this[int index]
		{
			get
			{
				return operations[index];
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		public void Delete(ITableEntity entity)
		{
			CommonUtility.AssertNotNull("entity", entity);
			if (string.IsNullOrEmpty(entity.ETag))
			{
				throw new ArgumentException("Delete requires an ETag (which may be the '*' wildcard).");
			}
			Add(new TableOperation(entity, TableOperationType.Delete));
		}

		public void Insert(ITableEntity entity)
		{
			Insert(entity, echoContent: false);
		}

		public void Insert(ITableEntity entity, bool echoContent)
		{
			CommonUtility.AssertNotNull("entity", entity);
			Add(new TableOperation(entity, TableOperationType.Insert, echoContent));
		}

		public void InsertOrMerge(ITableEntity entity)
		{
			CommonUtility.AssertNotNull("entity", entity);
			Add(new TableOperation(entity, TableOperationType.InsertOrMerge));
		}

		public void InsertOrReplace(ITableEntity entity)
		{
			CommonUtility.AssertNotNull("entity", entity);
			Add(new TableOperation(entity, TableOperationType.InsertOrReplace));
		}

		public void Merge(ITableEntity entity)
		{
			CommonUtility.AssertNotNull("entity", entity);
			if (string.IsNullOrEmpty(entity.ETag))
			{
				throw new ArgumentException("Merge requires an ETag (which may be the '*' wildcard).");
			}
			Add(new TableOperation(entity, TableOperationType.Merge));
		}

		public void Replace(ITableEntity entity)
		{
			CommonUtility.AssertNotNull("entity", entity);
			if (string.IsNullOrEmpty(entity.ETag))
			{
				throw new ArgumentException("Replace requires an ETag (which may be the '*' wildcard).");
			}
			Add(new TableOperation(entity, TableOperationType.Replace));
		}

		public void Retrieve(string partitionKey, string rowKey)
		{
			CommonUtility.AssertNotNull("partitionKey", partitionKey);
			CommonUtility.AssertNotNull("rowkey", rowKey);
			Add(new TableOperation(null, TableOperationType.Retrieve)
			{
				RetrievePartitionKey = partitionKey,
				RetrieveRowKey = rowKey
			});
		}

		public int IndexOf(TableOperation item)
		{
			return operations.IndexOf(item);
		}

		public void Insert(int index, TableOperation item)
		{
			CommonUtility.AssertNotNull("item", item);
			CheckSingleQueryPerBatch(item);
			LockToPartitionKey(item.PartitionKey);
			CheckPartitionKeyRowKeyPresent(item);
			operations.Insert(index, item);
		}

		public void RemoveAt(int index)
		{
			operations.RemoveAt(index);
			if (operations.Count == 0)
			{
				batchPartitionKey = null;
				hasQuery = false;
			}
		}

		public void Add(TableOperation item)
		{
			CommonUtility.AssertNotNull("item", item);
			CheckSingleQueryPerBatch(item);
			LockToPartitionKey(item.PartitionKey);
			CheckPartitionKeyRowKeyPresent(item);
			operations.Add(item);
		}

		public void Clear()
		{
			operations.Clear();
			batchPartitionKey = null;
			hasQuery = false;
		}

		public bool Contains(TableOperation item)
		{
			return operations.Contains(item);
		}

		public void CopyTo(TableOperation[] array, int arrayIndex)
		{
			operations.CopyTo(array, arrayIndex);
		}

		public bool Remove(TableOperation item)
		{
			CommonUtility.AssertNotNull("item", item);
			bool result = operations.Remove(item);
			if (operations.Count == 0)
			{
				batchPartitionKey = null;
				hasQuery = false;
			}
			return result;
		}

		public IEnumerator<TableOperation> GetEnumerator()
		{
			return operations.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return operations.GetEnumerator();
		}

		public void Retrieve<TElement>(string partitionKey, string rowKey, List<string> selectedColumns = null) where TElement : ITableEntity
		{
			CommonUtility.AssertNotNull("partitionKey", partitionKey);
			CommonUtility.AssertNotNull("rowkey", rowKey);
			Add(new TableOperation(null, TableOperationType.Retrieve)
			{
				RetrievePartitionKey = partitionKey,
				RetrieveRowKey = rowKey,
				SelectColumns = selectedColumns,
				RetrieveResolver = ((string pk, string rk, DateTimeOffset ts, IDictionary<string, EntityProperty> prop, string etag) => EntityUtilities.ResolveEntityByType<TElement>(pk, rk, ts, prop, etag))
			});
		}

		public void Retrieve<TResult>(string partitionKey, string rowKey, EntityResolver<TResult> resolver, List<string> selectedColumns = null)
		{
			CommonUtility.AssertNotNull("partitionKey", partitionKey);
			CommonUtility.AssertNotNull("rowkey", rowKey);
			Add(new TableOperation(null, TableOperationType.Retrieve)
			{
				RetrievePartitionKey = partitionKey,
				RetrieveRowKey = rowKey,
				SelectColumns = selectedColumns,
				RetrieveResolver = ((string pk, string rk, DateTimeOffset ts, IDictionary<string, EntityProperty> prop, string etag) => resolver(pk, rk, ts, prop, etag))
			});
		}

		internal TableBatchResult Execute(CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext)
		{
			TableRequestOptions requestOptions2 = TableRequestOptions.ApplyDefaults(requestOptions, client);
			operationContext = (operationContext ?? new OperationContext());
			CommonUtility.AssertNotNullOrEmpty("tableName", table.Name);
			if (operations.Count == 0)
			{
				throw new InvalidOperationException("Cannot execute an empty batch operation");
			}
			if (operations.Count > 100)
			{
				throw new InvalidOperationException("The maximum number of operations allowed in one batch has been exceeded.");
			}
			return client.Executor.ExecuteTableBatchOperation<TableBatchResult>(this, client, table, requestOptions2, operationContext);
		}

		internal Task<TableBatchResult> ExecuteAsync(CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			TableRequestOptions requestOptions2 = TableRequestOptions.ApplyDefaults(requestOptions, client);
			operationContext = (operationContext ?? new OperationContext());
			CommonUtility.AssertNotNullOrEmpty("tableName", table.Name);
			if (operations.Count == 0)
			{
				throw new InvalidOperationException("Cannot execute an empty batch operation");
			}
			if (operations.Count > 100)
			{
				throw new InvalidOperationException("The maximum number of operations allowed in one batch has been exceeded.");
			}
			return client.Executor.ExecuteTableBatchOperationAsync<TableBatchResult>(this, client, table, requestOptions2, operationContext, cancellationToken);
		}

		private void CheckSingleQueryPerBatch(TableOperation item)
		{
			if (hasQuery)
			{
				throw new ArgumentException("A batch transaction with a retrieve operation cannot contain any other operations.");
			}
			if (item.OperationType == TableOperationType.Retrieve)
			{
				if (operations.Count > 0)
				{
					throw new ArgumentException("A batch transaction with a retrieve operation cannot contain any other operations.");
				}
				hasQuery = true;
			}
			ContainsWrites = (item.OperationType != TableOperationType.Retrieve);
		}

		private void LockToPartitionKey(string partitionKey)
		{
			if (batchPartitionKey == null)
			{
				batchPartitionKey = partitionKey;
			}
			else if (partitionKey != batchPartitionKey)
			{
				throw new ArgumentException("All entities in a given batch must have the same partition key.");
			}
		}

		private static void CheckPartitionKeyRowKeyPresent(TableOperation item)
		{
			if (item.OperationType == TableOperationType.Retrieve || (item.PartitionKey != null && item.RowKey != null))
			{
				return;
			}
			throw new ArgumentNullException("item", "A batch non-retrieve operation requires a non-null partition key and row key.");
		}
	}
}
