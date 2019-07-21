using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Interop.Common.Schema.Edm;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Microsoft.Azure.Cosmos.Tables.SharedFiles
{
	internal static class EntityTranslator
	{
		internal static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
		{
			ContractResolver = new DefaultContractResolver(),
			NullValueHandling = NullValueHandling.Ignore
		};

		internal static readonly JsonSerializer JsonSerializer = JsonSerializer.Create(JsonSerializerSettings);

		internal static Document GetDocumentFromEntityProperties(IDictionary<string, EntityProperty> properties, string partitionKey, string rowKey, bool removeSystemGeneratedProperties)
		{
			if (properties == null)
			{
				throw new ArgumentException("Entity properties should not be null.");
			}
			if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
			{
				throw new ArgumentException("Partition/row key should not be null");
			}
			if (removeSystemGeneratedProperties)
			{
				if (properties.ContainsKey("Timestamp"))
				{
					properties.Remove("Timestamp");
				}
				if (properties.ContainsKey("Etag"))
				{
					properties.Remove("Etag");
				}
			}
			using (StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture))
			{
				using (ITableEntityWriter tableEntityWriter = new TableEntityWriter(stringWriter))
				{
					tableEntityWriter.Start();
					foreach (KeyValuePair<string, EntityProperty> property in properties)
					{
						if (!property.Value.IsNull)
						{
							tableEntityWriter.WriteName(property.Key);
							switch (property.Value.PropertyType)
							{
							case EdmType.String:
								tableEntityWriter.WriteString(property.Value.StringValue);
								break;
							case EdmType.Binary:
								tableEntityWriter.WriteBinary(property.Value.BinaryValue);
								break;
							case EdmType.Boolean:
								tableEntityWriter.WriteBoolean(property.Value.BooleanValue);
								break;
							case EdmType.DateTime:
								tableEntityWriter.WriteDateTime(property.Value.DateTime);
								break;
							case EdmType.Double:
								tableEntityWriter.WriteDouble(property.Value.DoubleValue);
								break;
							case EdmType.Guid:
								tableEntityWriter.WriteGuid(property.Value.GuidValue);
								break;
							case EdmType.Int32:
								tableEntityWriter.WriteInt32(property.Value.Int32Value);
								break;
							case EdmType.Int64:
								tableEntityWriter.WriteInt64(property.Value.Int64Value);
								break;
							default:
								throw new Exception(string.Format(CultureInfo.CurrentCulture, "Unexpected Edm type '{0}'", (int)property.Value.PropertyType));
							}
						}
					}
					tableEntityWriter.End();
				}
				Document document = DeserializeObject<Document>(stringWriter.ToString());
				document.Id = rowKey;
				document.SetPropertyValue("$pk", partitionKey);
				document.SetPropertyValue("$id", rowKey);
				return document;
			}
		}

		internal static Document GetDocumentWithPartitionAndRowKey(string partitionKey, string rowKey)
		{
			Document document = new Document();
			document.Id = rowKey;
			document.SetPropertyValue("$pk", partitionKey);
			return document;
		}

		internal static void GetEntityPropertiesFromDocument(Document document, IList<string> selectColumns, out string partitionKey, out string rowKey, out string eTag, out DateTimeOffset timestamp, out IDictionary<string, EntityProperty> entityProperties)
		{
			partitionKey = document.GetPropertyValue<string>("$pk");
			rowKey = document.GetPropertyValue<string>("$id");
			eTag = document.ETag;
			timestamp = document.Timestamp;
			entityProperties = GetEntityPropertiesFromDocument(document, selectColumns);
		}

		internal static IDictionary<string, EntityProperty> GetEntityPropertiesFromDocument(Document document, IList<string> selectColumns)
		{
			string serializedDocument = SerializeObject(document);
			EdmConverter.GetPropertiesFromDocument(selectColumns, serializedDocument, out IDictionary<string, EntityProperty> entityProperties, out IDictionary<string, EntityProperty> _);
			return entityProperties;
		}

		internal static string GetPropertyName(string propertyName, bool enableTimestampQuery = false)
		{
			if (string.Equals(propertyName, "RowKey", StringComparison.Ordinal))
			{
				return string.Format(CultureInfo.InvariantCulture, "{0}['{1}']", EdmSchemaMapping.EntityName, "$id");
			}
			if (string.Equals(propertyName, "PartitionKey", StringComparison.Ordinal))
			{
				return string.Format(CultureInfo.InvariantCulture, "{0}['{1}']", EdmSchemaMapping.EntityName, "$pk");
			}
			if (string.Equals(propertyName, "Timestamp", StringComparison.Ordinal) && enableTimestampQuery)
			{
				return string.Format(CultureInfo.InvariantCulture, "{0}['{1}']", EdmSchemaMapping.EntityName, "_ts");
			}
			return string.Format(CultureInfo.InvariantCulture, "{0}['{1}']['$v']", EdmSchemaMapping.EntityName, propertyName);
		}

		internal static string SerializeObject(object value)
		{
			StringWriter stringWriter = new StringWriter(new StringBuilder(256), CultureInfo.InvariantCulture);
			using (JsonTextWriter jsonTextWriter = new JsonTextWriter(stringWriter))
			{
				jsonTextWriter.Formatting = JsonSerializer.Formatting;
				JsonSerializer.Serialize(jsonTextWriter, value, null);
			}
			return stringWriter.ToString();
		}

		internal static TObject DeserializeObject<TObject>(string value)
		{
			using (JsonTextReader reader = new JsonTextReader(new StringReader(value)))
			{
				return (TObject)JsonSerializer.Deserialize(reader, typeof(TObject));
			}
		}
	}
}
