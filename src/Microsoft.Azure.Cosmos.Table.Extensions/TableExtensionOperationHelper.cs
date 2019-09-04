using Microsoft.Azure.Cosmos.Tables.ResourceModel;
using Microsoft.Azure.Cosmos.Tables.SharedFiles;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Cosmos.Table.Extensions
{
	internal static class TableExtensionOperationHelper
	{
		internal static async Task<TResult> ExecuteOperationAsync<TResult>(TableOperation operation, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken) where TResult : class
		{
			TableResult tableResult;
			try
			{
				switch (operation.OperationType)
				{
				case TableOperationType.Insert:
					tableResult = await HandleInsertAsync(operation, client, table, requestOptions, operationContext, cancellationToken);
					break;
				case TableOperationType.Merge:
				case TableOperationType.InsertOrMerge:
					tableResult = await HandleMergeAsync(operation, client, table, requestOptions, operationContext, cancellationToken);
					break;
				case TableOperationType.Delete:
					tableResult = await HandleDeleteAsync(operation, client, table, requestOptions, operationContext, cancellationToken);
					break;
				case TableOperationType.InsertOrReplace:
					tableResult = await HandleUpsertAsync(operation, client, table, requestOptions, operationContext, cancellationToken);
					break;
				case TableOperationType.Replace:
					tableResult = await HandleReplaceAsync(operation, client, table, requestOptions, operationContext, cancellationToken);
					break;
				case TableOperationType.Retrieve:
					tableResult = await HandleReadAsync(operation, client, table, requestOptions, operationContext, cancellationToken);
					break;
				default:
					throw new NotSupportedException();
				}
			}
			catch (Exception exception)
			{
				StorageException tableResultFromException = EntityHelpers.GetTableResultFromException(exception, operation);
				operationContext.RequestResults.Add(tableResultFromException.RequestInformation);
				throw tableResultFromException;
			}
			operationContext.FireRequestCompleted(new RequestEventArgs(operationContext.LastResult));
			return tableResult as TResult;
		}

		internal static async Task<TResult> ExecuteBatchOperationAsync<TResult>(TableBatchOperation batch, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken) where TResult : class
		{
			TableOperationType currentOperationType = batch.First().OperationType;
			batch.First();
			try
			{
				List<Document> list = new List<Document>();
				List<TableOperationType> list2 = new List<TableOperationType>();
				List<string> list3 = new List<string>();
				foreach (TableOperation item2 in batch)
				{
					currentOperationType = item2.OperationType;
					Document item = (item2.OperationType != TableOperationType.Retrieve) ? EntityHelpers.GetDocumentFromEntity(item2.Entity, operationContext, requestOptions) : EntityTranslator.GetDocumentWithPartitionAndRowKey(item2.RetrievePartitionKey, item2.RetrieveRowKey);
					list.Add(item);
					list2.Add(item2.OperationType);
					list3.Add((item2.Entity == null) ? string.Empty : EtagHelper.ConvertToBackEndETagFormat(item2.Entity.ETag));
				}
				RequestOptions requestOptions2 = GetRequestOptions(batch.batchPartitionKey, requestOptions);
				Uri storedProcedureUri = UriFactory.CreateStoredProcedureUri("TablesDB", table.Name, "__.sys.tablesBatchOperation");
				StoredProcedureResponse<string> storedProcedureResponse = await table.ServiceClient.DocumentClient.ExecuteStoredProcedureAsync<string>(storedProcedureUri, requestOptions2, new object[3]
				{
					list.ToArray(),
					list2.ToArray(),
					list3.ToArray()
				});
				JArray jArray = JArray.Parse(storedProcedureResponse.Response);
				TableBatchResult tableBatchResult = new TableBatchResult();
				tableBatchResult.RequestCharge = storedProcedureResponse.RequestCharge;
				for (int i = 0; i < jArray.Count; i++)
				{
					tableBatchResult.Add(GetTableResultFromDocument(batch[i], jArray[i].ToObject<Document>(), operationContext, requestOptions, storedProcedureResponse.SessionToken, 0.0));
				}
				return tableBatchResult as TResult;
			}
			catch (Exception ex)
			{
				DocumentClientException ex2 = ex as DocumentClientException;
				if (ex2 != null && ex2.StatusCode == HttpStatusCode.BadRequest && ex2.Message.Contains("Resource Not Found") && currentOperationType == TableOperationType.Retrieve)
				{
					TableBatchResult tableBatchResult2 = new TableBatchResult();
					tableBatchResult2.Add(new TableResult
					{
						Etag = null,
						HttpStatusCode = 404,
						Result = null
					});
					tableBatchResult2.RequestCharge = ex2.RequestCharge;
					return tableBatchResult2 as TResult;
				}
				TableErrorResult tableErrorResult = ex.TranslateDocumentErrorForStoredProcs(null, batch.Count);
				RequestResult requestResult = GenerateRequestResult(tableErrorResult.ExtendedErroMessage, tableErrorResult.HttpStatusCode, tableErrorResult.ExtendedErrorCode, tableErrorResult.ExtendedErroMessage, tableErrorResult.ServiceRequestID, tableErrorResult.RequestCharge);
				StorageException ex3 = new StorageException(requestResult, requestResult.ExtendedErrorInformation.ErrorMessage, ex);
				if (ex2 != null)
				{
					PopulateOperationContextForBatchOperations(operationContext, ex3, ex2.ActivityId);
				}
				throw ex3;
			}
		}

		internal static RequestResult GenerateRequestResult(string httpStatusMessage, int httpStatusCode, string extendedInfoErrorCode, string extendedInfoErrorMessage, string serviceRequestId, double? requestCharge)
		{
			return new RequestResult
			{
				HttpStatusMessage = httpStatusMessage,
				HttpStatusCode = httpStatusCode,
				ExtendedErrorInformation = new StorageExtendedErrorInformation
				{
					ErrorCode = extendedInfoErrorCode,
					ErrorMessage = extendedInfoErrorMessage
				},
				ServiceRequestID = serviceRequestId,
				RequestCharge = requestCharge
			};
		}

		private static void PopulateOperationContextForBatchOperations(OperationContext operationContext, StorageException storageException, string serviceRequestId)
		{
			operationContext?.RequestResults.Add(storageException.ToRequestResult(serviceRequestId));
		}

		private static async Task<TableResult> HandleMergeAsync(TableOperation operation, CloudTableClient client, CloudTable table, TableRequestOptions options, OperationContext context, CancellationToken cancellationToken)
		{
			Document documentFromEntity = EntityHelpers.GetDocumentFromEntity(operation.Entity, context, options);
			RequestOptions requestOptions = GetRequestOptions(operation, options);
			StoredProcedureResponse<string> storedProcedureResponse = await DocumentEntityCollectionBaseHelpers.HandleEntityMergeAsync(table.Name, operation.OperationType, operation.PartitionKey, EtagHelper.ConvertToBackEndETagFormat(operation.ETag), client.DocumentClient, documentFromEntity, requestOptions, cancellationToken);
			Document response = JsonConvert.DeserializeObject<List<Document>>(storedProcedureResponse.Response).FirstOrDefault();
			return GetTableResultFromDocument(operation, response, context, options, storedProcedureResponse.SessionToken, storedProcedureResponse.RequestCharge);
		}

		private static async Task<TableResult> HandleInsertAsync(TableOperation operation, CloudTableClient client, CloudTable table, TableRequestOptions options, OperationContext context, CancellationToken cancellationToken)
		{
			if (operation.IsTableEntity)
			{
				DynamicTableEntity tblEntity = (DynamicTableEntity)operation.Entity;
				string cosmosTableName = tblEntity.GetCosmosTableName();
				int? cosmosTableThroughput = tblEntity.GetCosmosTableThroughput();
				IndexingPolicy cosmosTableIndexingPolicy = tblEntity.GetCosmosTableIndexingPolicy();
				RequestOptions defaultRequestOptions = GetDefaultRequestOptions(null, cosmosTableThroughput);
				return EntityHelpers.GetTableResultFromResponse(await DocumentCollectionBaseHelpers.HandleCollectionFeedInsertAsync(client.DocumentClient, cosmosTableName, cosmosTableIndexingPolicy, defaultRequestOptions, cancellationToken), context);
			}
			Document documentFromEntity = EntityHelpers.GetDocumentFromEntity(operation.Entity, context, options);
			RequestOptions requestOptions = GetRequestOptions(operation, options);
			ResourceResponse<Document> resourceResponse = await DocumentEntityCollectionBaseHelpers.HandleEntityFeedInsertAsync(table.Name, client.DocumentClient, documentFromEntity, requestOptions, cancellationToken);
			return GetTableResultFromResponse(operation, resourceResponse, context, options, operation.SelectColumns, resourceResponse.SessionToken);
		}

		private static async Task<TableResult> HandleUpsertAsync(TableOperation operation, CloudTableClient client, CloudTable table, TableRequestOptions options, OperationContext context, CancellationToken cancellationToken)
		{
			Document documentFromEntity = EntityHelpers.GetDocumentFromEntity(operation.Entity, context, options);
			RequestOptions requestOptions = GetRequestOptions(operation, options);
			ResourceResponse<Document> resourceResponse = await client.DocumentClient.UpsertDocumentAsync(table.GetCollectionUri(), documentFromEntity, requestOptions, disableAutomaticIdGeneration: true, cancellationToken);
			return GetTableResultFromResponse(operation, resourceResponse, context, options, operation.SelectColumns, resourceResponse.SessionToken);
		}

		private static async Task<TableResult> HandleReplaceAsync(TableOperation operation, CloudTableClient client, CloudTable table, TableRequestOptions options, OperationContext context, CancellationToken cancellationToken)
		{
			Document documentFromEntity = EntityHelpers.GetDocumentFromEntity(operation.Entity, context, options);
			RequestOptions requestOptions = GetRequestOptions(operation, options);
			ResourceResponse<Document> resourceResponse = await DocumentEntityCollectionBaseHelpers.HandleEntityReplaceOnlyAsync(table.Name, operation.PartitionKey, operation.RowKey, EtagHelper.ConvertToBackEndETagFormat(operation.ETag), client.DocumentClient, documentFromEntity, requestOptions, cancellationToken);
			return GetTableResultFromResponse(operation, resourceResponse, context, options, operation.SelectColumns, resourceResponse.SessionToken);
		}

		private static async Task<TableResult> HandleDeleteAsync(TableOperation operation, CloudTableClient client, CloudTable table, TableRequestOptions options, OperationContext context, CancellationToken cancellationToken)
		{
			if (operation.IsTableEntity)
			{
				string stringValue = ((DynamicTableEntity)operation.Entity).Properties["TableName"].StringValue;
				Uri documentCollectionUri = UriFactory.CreateDocumentCollectionUri("TablesDB", stringValue);
				return EntityHelpers.GetTableResultFromResponse(await client.DocumentClient.DeleteDocumentCollectionAsync(documentCollectionUri), context);
			}
			RequestOptions requestOptions = GetRequestOptions(operation, options);
			ResourceResponse<Document> resourceResponse = await DocumentEntityCollectionBaseHelpers.HandleEntityDeleteAsync(table.Name, operation.PartitionKey, operation.RowKey, EtagHelper.ConvertToBackEndETagFormat(operation.ETag), client.DocumentClient, requestOptions, cancellationToken);
			return GetTableResultFromResponse(operation, resourceResponse, context, options, operation.SelectColumns, resourceResponse.SessionToken);
		}

		private static async Task<TableResult> HandleReadAsync(TableOperation operation, CloudTableClient client, CloudTable table, TableRequestOptions options, OperationContext context, CancellationToken cancellationToken)
		{
			try
			{
				if (operation.IsTableEntity)
				{
					return EntityHelpers.GetTableResultFromResponse(await DocumentCollectionBaseHelpers.HandleDocumentCollectionRetrieveAsync(operation.GetCosmosTableName(), client.DocumentClient), context);
				}
				RequestOptions requestOptions = GetRequestOptions(operation, options);
				ResourceResponse<Document> resourceResponse = await DocumentEntityCollectionBaseHelpers.HandleEntityRetrieveAsync(table.Name, operation.PartitionKey, operation.RowKey, client.DocumentClient, requestOptions, cancellationToken);
				return GetTableResultFromResponse(operation, resourceResponse, context, options, operation.SelectColumns, resourceResponse.SessionToken);
			}
			catch (DocumentClientException ex)
			{
				if (ex.StatusCode == HttpStatusCode.NotFound)
				{
					return new TableResult
					{
						HttpStatusCode = 404,
						RequestCharge = ex.RequestCharge
					};
				}
				throw;
			}
		}

		private static TableResult GetTableResultFromResponse(TableOperation operation, ResourceResponse<Document> response, OperationContext context, TableRequestOptions options, List<string> selectColumns, string sessionToken)
		{
			TableResult tableResult = new TableResult();
			tableResult.Etag = EtagHelper.ConvertFromBackEndETagFormat(response.ResponseHeaders["ETag"]);
			tableResult.HttpStatusCode = (int)response.StatusCode;
			tableResult.SessionToken = sessionToken;
			tableResult.RequestCharge = response.RequestCharge;
			if (operation.Entity != null && !string.IsNullOrEmpty(tableResult.Etag))
			{
				operation.Entity.ETag = tableResult.Etag;
			}
			if (operation.OperationType == TableOperationType.InsertOrReplace || operation.OperationType == TableOperationType.Replace || !operation.EchoContent)
			{
				tableResult.HttpStatusCode = 204;
			}
			if (operation.OperationType != TableOperationType.Retrieve)
			{
				if (operation.OperationType != TableOperationType.Delete)
				{
					tableResult.Result = operation.Entity;
					ITableEntity tableEntity = tableResult.Result as ITableEntity;
					if (tableEntity != null)
					{
						if (tableResult.Etag != null)
						{
							tableEntity.ETag = tableResult.Etag;
						}
						tableEntity.Timestamp = response.Resource.Timestamp;
					}
				}
			}
			else if (response.Resource != null)
			{
				if (operation.RetrieveResolver == null)
				{
					tableResult.Result = EntityHelpers.GetEntityFromResourceStream(response.ResponseStream, context, operation.SelectColumns);
				}
				else
				{
					EntityHelpers.GetPropertiesFromResourceStream(response.ResponseStream, operation.SelectColumns, out IDictionary<string, EntityProperty> entityProperties, out IDictionary<string, EntityProperty> documentDBProperties);
					if (options.ProjectSystemProperties.Value)
					{
						EdmConverter.ValidateDocumentDBProperties(documentDBProperties);
						EntityProperty entityProperty = documentDBProperties["$pk"];
						EntityProperty entityProperty2 = documentDBProperties["$id"];
						EntityProperty entityProperty3 = documentDBProperties["_etag"];
						EntityProperty entityProperty4 = documentDBProperties["_ts"];
						string arg = EtagHelper.ConvertFromBackEndETagFormat(entityProperty3.StringValue);
						DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(entityProperty4.DoubleValue.Value);
						tableResult.Result = operation.RetrieveResolver(entityProperty.StringValue, entityProperty2.StringValue, dateTime, entityProperties, arg);
					}
					else
					{
						tableResult.Result = operation.RetrieveResolver(null, null, default(DateTimeOffset), entityProperties, null);
					}
				}
			}
			return tableResult;
		}

		private static TableResult GetTableResultFromDocument(TableOperation operation, Document response, OperationContext context, TableRequestOptions requestOptions, string sessionToken, double requestCharge)
		{
			var responseETag = EtagHelper.ConvertFromBackEndETagFormat(response.ETag);
			response.SetPropertyValue("_etag", responseETag);
			TableResult tableResult = new TableResult();
			tableResult.Etag = response.ETag;
			tableResult.HttpStatusCode = GetSuccessStatusCodeFromOperationType(operation.OperationType);
			tableResult.SessionToken = sessionToken;
			tableResult.RequestCharge = requestCharge;
			if (operation.OperationType != TableOperationType.Retrieve)
			{
				operation.Entity.ETag = response.ETag;
				tableResult.Result = operation.Entity;
			}
			else if (operation.RetrieveResolver != null)
			{
				tableResult.Result = operation.RetrieveResolver(response.GetPropertyValue<string>("$pk"), response.Id, response.Timestamp, EntityTranslator.GetEntityPropertiesFromDocument(response, operation.SelectColumns), response.ETag);
			}
			else
			{
				tableResult.Result = EntityHelpers.GetEntityFromDocument(response, context, operation.SelectColumns);
			}
			return tableResult;
		}

		private static int GetSuccessStatusCodeFromOperationType(TableOperationType operationType)
		{
			switch (operationType)
			{
			case TableOperationType.Insert:
				return 201;
			case TableOperationType.Delete:
			case TableOperationType.Replace:
			case TableOperationType.Merge:
			case TableOperationType.InsertOrReplace:
			case TableOperationType.InsertOrMerge:
				return 204;
			case TableOperationType.Retrieve:
				return 200;
			default:
				return 200;
			}
		}

		private static RequestOptions GetDefaultRequestOptions(string partitionKey, int? throughput = default(int?))
		{
			return new RequestOptions
			{
				PartitionKey = (string.IsNullOrEmpty(partitionKey) ? null : new PartitionKey(partitionKey)),
				OfferThroughput = throughput
			};
		}

		private static RequestOptions GetRequestOptions(TableOperation operation, TableRequestOptions options)
		{
			return GetRequestOptions(operation.PartitionKey, options);
		}

		private static RequestOptions GetRequestOptions(string partitionKey, TableRequestOptions options)
		{
			RequestOptions defaultRequestOptions = GetDefaultRequestOptions(partitionKey);
			if (options != null)
			{
				defaultRequestOptions.SessionToken = options.SessionToken;
				defaultRequestOptions.ConsistencyLevel = CloudTableClient.ToDocDbConsistencyLevel(options.ConsistencyLevel);
			}
			return defaultRequestOptions;
		}
	}
}
