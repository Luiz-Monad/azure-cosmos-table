using Microsoft.Azure.Cosmos.Tables.ResourceModel;
using Microsoft.Azure.Cosmos.Tables.SharedFiles;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Interop.Common.Schema.Edm;
using Microsoft.Azure.Documents.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Microsoft.Azure.Cosmos.Table.Extensions
{
	internal static class TableExtensionQueryHelper
	{
		private static char[] SelectDelimiter = new char[1]
		{
			','
		};

		internal static async Task<TableQuerySegment<TResult>> QueryCollectionsAsync<TResult>(int? maxItemCount, string filterString, 
			TableContinuationToken token, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext)
		{
			ValidateContinuationToken(token);
			FeedOptions defaultFeedOptions = GetDefaultFeedOptions(requestOptions);
			defaultFeedOptions.RequestContinuation = token?.NextRowKey;
			Microsoft.Azure.Documents.Client.FeedResponse<DocumentCollection> feedResponse;
			if (string.IsNullOrEmpty(filterString))
			{
				feedResponse = await client.DocumentClient.CreateDocumentCollectionQuery(
					UriFactory.CreateDatabaseUri("TablesDB"), defaultFeedOptions).AsDocumentQuery().ExecuteNextAsync<DocumentCollection>();
			}
			else
			{
				string sqlQuery = QueryTranslator.GetSqlQuery("*", filterString, isLinqExpression: false, isTableQuery: true, null);
				feedResponse = await client.DocumentClient.CreateDocumentCollectionQuery(
					UriFactory.CreateDatabaseUri("TablesDB"), sqlQuery, defaultFeedOptions).AsDocumentQuery().ExecuteNextAsync<DocumentCollection>();
			}
			operationContext.RequestResults.Add(feedResponse.ToRequestResult());
			List<TResult> list = new List<TResult>();
			foreach (DocumentCollection item in feedResponse)
			{
				list.Add((TResult)(object)new DynamicTableEntity
				{
					Properties = 
					{
						{
							"TableName",
							new EntityProperty(item.Id)
						}
					}
				});
			}
			TableQuerySegment<TResult> tableQuerySegment = new TableQuerySegment<TResult>(list);
			if (!string.IsNullOrEmpty(feedResponse.ResponseContinuation))
			{
				tableQuerySegment.ContinuationToken = new TableContinuationToken
				{
					NextRowKey = feedResponse.ResponseContinuation
				};
			}
			tableQuerySegment.RequestCharge = feedResponse.RequestCharge;
			return tableQuerySegment;
		}

		internal static async Task<TableQuerySegment<TResult>> QueryDocumentsAsync<TResult>(int? maxItemCount, string filterString, IList<string> selectColumns, 
			TableContinuationToken token, CloudTableClient client, CloudTable table, EntityResolver<TResult> resolver, TableRequestOptions requestOptions, 
			OperationContext operationContext, bool isLinqExpression, IList<OrderByItem> orderByItems)
		{
			ValidateContinuationToken(token);
			selectColumns = ((selectColumns != null) ? new List<string>(selectColumns) : null);
			Dictionary<string, bool> selectedSystemProperties = new Dictionary<string, bool>();
			string sqlQuery = QueryTranslator.GetSqlQuery(GetSelectList(selectColumns, requestOptions, out selectedSystemProperties), filterString, 
				isLinqExpression, isTableQuery: false, orderByItems, enableTimestampQuery: true);
			FeedOptions defaultFeedOptions = GetDefaultFeedOptions(requestOptions);
			if (maxItemCount.HasValue)
			{
				defaultFeedOptions.MaxItemCount = maxItemCount;
			}
			defaultFeedOptions.SessionToken = requestOptions.SessionToken;
			defaultFeedOptions.RequestContinuation = token?.NextRowKey;
			Microsoft.Azure.Documents.Client.FeedResponse<Document> feedResponse = 
				await client.DocumentClient.CreateDocumentQuery<Document>(table.GetCollectionUri(), sqlQuery, defaultFeedOptions).AsDocumentQuery()
					.ExecuteNextAsync<Document>();
			operationContext.RequestResults.Add(feedResponse.ToRequestResult());
			List<TResult> list = new List<TResult>();
			foreach (Document item in feedResponse)
			{
				var itemETag = EtagHelper.ConvertFromBackEndETagFormat(item.ETag);
				item.SetPropertyValue("_etag", itemETag);
				IDictionary<string, EntityProperty> entityPropertiesFromDocument = EntityTranslator.GetEntityPropertiesFromDocument(item, selectColumns);
				list.Add(resolver(
					selectedSystemProperties["PartitionKey"] ? item.GetPropertyValue<string>("$pk") : null, 
					selectedSystemProperties["RowKey"] ? item.GetPropertyValue<string>("$id") : null, 
					selectedSystemProperties["Timestamp"] ? ((DateTimeOffset)item.Timestamp) : default(DateTimeOffset), 
					entityPropertiesFromDocument, selectedSystemProperties["Etag"] ? item.ETag : null));
			}
			TableQuerySegment<TResult> tableQuerySegment = new TableQuerySegment<TResult>(list);
			if (!string.IsNullOrEmpty(feedResponse.ResponseContinuation))
			{
				tableQuerySegment.ContinuationToken = new TableContinuationToken
				{
					NextRowKey = feedResponse.ResponseContinuation
				};
			}
			tableQuerySegment.RequestCharge = feedResponse.RequestCharge;
			return tableQuerySegment;
		}

		private static void ValidateContinuationToken(TableContinuationToken token)
		{
			if (token != null)
			{
				if (!string.IsNullOrEmpty(token.NextPartitionKey))
				{
					throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, 
					"Setting the value of the property '{0}' is not supported.", "NextPartitionKey"));
				}
				if (!string.IsNullOrEmpty(token.NextTableName))
				{
					throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, 
					"Setting the value of the property '{0}' is not supported.", "NextTableName"));
				}
			}
		}

		private static void TryAddIfNotExists(IList<string> list, string key)
		{
			if (!list.Contains(key))
			{
				list.Add(key);
			}
		}

		private static string GetSelectList(IList<string> selectColumns, TableRequestOptions requestOptions, out Dictionary<string, bool> selectedSystemProperties)
		{
			selectedSystemProperties = new Dictionary<string, bool>();
			foreach (string key in EdmSchemaMapping.SystemPropertiesMapping.Keys)
			{
				selectedSystemProperties.Add(key, selectColumns == null || selectColumns.Count == 0);
			}
			if (selectColumns == null || selectColumns.Count == 0)
			{
				return "*";
			}
			string text = string.Empty;
			foreach (string selectColumn in selectColumns)
			{
				string arg = selectColumn;
				if (selectedSystemProperties.ContainsKey(selectColumn))
				{
					arg = EdmSchemaMapping.SystemPropertiesMapping[selectColumn];
					selectedSystemProperties[selectColumn] = true;
				}
				text = string.Format(CultureInfo.InvariantCulture, "{0}{1}['{2}'],", text, EdmSchemaMapping.EntityName, arg);
			}
			if (requestOptions == null || !requestOptions.ProjectSystemProperties.HasValue || requestOptions.ProjectSystemProperties.Value)
			{
				foreach (string key2 in EdmSchemaMapping.SystemPropertiesMapping.Keys)
				{
					if (!selectedSystemProperties[key2])
					{
						selectedSystemProperties[key2] = true;
						text = string.Format(CultureInfo.InvariantCulture, "{0}{1}['{2}'],", text, EdmSchemaMapping.EntityName, 
							EdmSchemaMapping.SystemPropertiesMapping[key2]);
					}
				}
			}
			return text.Trim(SelectDelimiter);
		}

		private static FeedOptions GetDefaultFeedOptions(TableRequestOptions options)
		{
			if (options != null && !TableExtensionSettings.EnableAppSettingsBasedOptions)
			{
				return new FeedOptions
				{
					MaxItemCount = options.TableQueryMaxItemCount,
					EnableScanInQuery = options.TableQueryEnableScan,
					EnableCrossPartitionQuery = true,
					MaxDegreeOfParallelism = options.TableQueryMaxDegreeOfParallelism.Value,
					ResponseContinuationTokenLimitInKb = options.TableQueryContinuationTokenLimitInKb,
					ConsistencyLevel = CloudTableClient.ToDocDbConsistencyLevel(options.ConsistencyLevel)
				};
			}
			return new FeedOptions
			{
				MaxItemCount = TableExtensionSettings.MaxItemCount,
				EnableScanInQuery = TableExtensionSettings.EnableScan,
				EnableCrossPartitionQuery = true,
				MaxDegreeOfParallelism = TableExtensionSettings.MaxDegreeOfParallelism,
				ResponseContinuationTokenLimitInKb = TableExtensionSettings.ContinuationTokenLimitInKb
			};
		}
	}
}
