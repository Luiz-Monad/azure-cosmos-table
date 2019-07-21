using Microsoft.Azure.Cosmos.Tables.SharedFiles;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Azure.Cosmos.Table.Extensions
{
	internal static class EntityHelpers
	{
		public static Document GetDocumentFromEntity(ITableEntity entity, OperationContext context, TableRequestOptions options)
		{
			if (entity == null)
			{
				throw new ArgumentException("Entity should not be null.");
			}
			TableEntityValidationHelper.ValidatePartitionKey(entity.PartitionKey);
			TableEntityValidationHelper.ValidateRowKey(entity.RowKey);
			return EntityTranslator.GetDocumentFromEntityProperties(entity.WriteEntity(context), entity.PartitionKey, entity.RowKey, removeSystemGeneratedProperties: false);
		}

		public static TableResult GetTableResultFromResponse(ResourceResponse<DocumentCollection> response, OperationContext context)
		{
			return new TableResult
			{
				Etag = response.ResponseHeaders["ETag"],
				HttpStatusCode = (int)response.StatusCode,
				RequestCharge = response.RequestCharge
			};
		}

		public static StorageException GetTableResultFromException(Exception exception, TableOperation tableOperation = null)
		{
			return exception.ToStorageException(tableOperation);
		}

		public static ITableEntity GetEntityFromResourceStream(Stream stream, OperationContext context, IList<string> selectColumns)
		{
			GetPropertiesFromResourceStream(stream, selectColumns, out IDictionary<string, EntityProperty> entityProperties, out IDictionary<string, EntityProperty> documentDBProperties);
			EdmConverter.ValidateDocumentDBProperties(documentDBProperties);
			EntityProperty entityProperty = documentDBProperties["$pk"];
			EntityProperty entityProperty2 = documentDBProperties["$id"];
			EntityProperty entityProperty3 = documentDBProperties["_etag"];
			EntityProperty entityProperty4 = documentDBProperties["_ts"];
			DynamicTableEntity dynamicTableEntity = new DynamicTableEntity(entityProperty.StringValue, entityProperty2.StringValue);
			dynamicTableEntity.ETag = entityProperty3.StringValue;
			dynamicTableEntity.Timestamp = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(entityProperty4.DoubleValue.Value);
			dynamicTableEntity.ReadEntity(entityProperties, context);
			return dynamicTableEntity;
		}

		public static ITableEntity GetEntityFromDocument(Document document, OperationContext context, IList<string> selectColumns)
		{
			string partitionKey = null;
			string rowKey = null;
			string eTag = null;
			DateTimeOffset timestamp = default(DateTimeOffset);
			IDictionary<string, EntityProperty> entityProperties = null;
			EntityTranslator.GetEntityPropertiesFromDocument(document, selectColumns, out partitionKey, out rowKey, out eTag, out timestamp, out entityProperties);
			DynamicTableEntity dynamicTableEntity = new DynamicTableEntity(partitionKey, rowKey);
			dynamicTableEntity.ETag = eTag;
			dynamicTableEntity.Timestamp = timestamp;
			dynamicTableEntity.PartitionKey = partitionKey;
			dynamicTableEntity.ReadEntity(entityProperties, context);
			return dynamicTableEntity;
		}

		public static void GetPropertiesFromResourceStream(Stream stream, IList<string> selectColumns, out IDictionary<string, EntityProperty> entityProperties, out IDictionary<string, EntityProperty> documentDBProperties)
		{
			using (StreamReader streamReader = new StreamReader(stream))
			{
				EdmConverter.GetPropertiesFromDocument(selectColumns, streamReader.ReadToEnd(), out entityProperties, out documentDBProperties);
			}
		}
	}
}
