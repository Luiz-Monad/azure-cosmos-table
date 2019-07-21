using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Cosmos.Tables.SharedFiles
{
	internal sealed class DocumentEntityCollectionBaseHelpers
	{
		public static async Task<ResourceResponse<Document>> HandleEntityRetrieveAsync(string tableName, string partitionKey, string rowKey, IDocumentClient client, RequestOptions requestOptions, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Uri uri = UriFactory.CreateDocumentUri("TablesDB", tableName, rowKey);
			requestOptions = SetPartitionKey(requestOptions, partitionKey);
			return await client.ReadDocumentAsync(uri.ToString(), requestOptions);
		}

		public static async Task<ResourceResponse<Document>> HandleEntityReplaceOnlyAsync(string tableName, string partitionKey, string rowKey, string ifMatch, IDocumentClient client, Document document, RequestOptions requestOptions)
		{
			Uri uri = UriFactory.CreateDocumentUri("TablesDB", tableName, rowKey);
			requestOptions = SetPartitionKey(requestOptions, partitionKey);
			if (!string.IsNullOrEmpty(ifMatch))
			{
				requestOptions.AccessCondition = new AccessCondition
				{
					Type = AccessConditionType.IfMatch,
					Condition = ifMatch
				};
			}
			return await client.ReplaceDocumentAsync(uri.ToString(), document, requestOptions);
		}

		public static async Task<ResourceResponse<Document>> HandleEntityDeleteAsync(string tableName, string partitionKey, string rowKey, string ifMatch, IDocumentClient client, RequestOptions requestOptions, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Uri uri = UriFactory.CreateDocumentUri("TablesDB", tableName, rowKey);
			requestOptions = SetPartitionKey(requestOptions, partitionKey);
			if (!string.IsNullOrEmpty(ifMatch))
			{
				requestOptions.AccessCondition = new AccessCondition
				{
					Type = AccessConditionType.IfMatch,
					Condition = ifMatch
				};
			}
			return await client.DeleteDocumentAsync(uri.ToString(), requestOptions);
		}

		public static async Task<StoredProcedureResponse<string>> HandleEntityMergeAsync(string tableName, TableOperationType operationType, string partitionKey, string ifMatch, IDocumentClient client, Document document, RequestOptions requestOptions, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Uri uri = UriFactory.CreateStoredProcedureUri("TablesDB", tableName, "__.sys.tablesBatchOperation");
			List<Document> list = new List<Document>();
			list.Add(document);
			List<TableOperationType> list2 = new List<TableOperationType>();
			list2.Add(operationType);
			List<string> list3 = new List<string>();
			list3.Add(ifMatch);
			AccessCondition accessCondition = null;
			if (operationType == TableOperationType.Merge)
			{
				accessCondition = new AccessCondition
				{
					Type = AccessConditionType.IfMatch,
					Condition = ifMatch
				};
			}
			requestOptions = SetPartitionKey(requestOptions, partitionKey);
			requestOptions.AccessCondition = accessCondition;
			return await client.ExecuteStoredProcedureAsync<string>(uri.ToString(), requestOptions, new object[3]
			{
				list.ToArray(),
				list2.ToArray(),
				list3.ToArray()
			});
		}

		public static async Task<ResourceResponse<Document>> HandleEntityFeedInsertAsync(string tableName, IDocumentClient client, Document document, RequestOptions requestOptions, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Uri uri = UriFactory.CreateDocumentCollectionUri("TablesDB", tableName);
			return await client.CreateDocumentAsync(uri.ToString(), document, requestOptions, disableAutomaticIdGeneration: true);
		}

		private static RequestOptions SetPartitionKey(RequestOptions requestOptions, string partitionKey)
		{
			if (requestOptions == null)
			{
				requestOptions = new RequestOptions();
			}
			requestOptions.PartitionKey = new PartitionKey(partitionKey);
			return requestOptions;
		}
	}
}
