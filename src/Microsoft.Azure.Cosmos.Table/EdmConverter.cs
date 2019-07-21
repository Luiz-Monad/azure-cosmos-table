using Microsoft.Azure.Documents.Interop.Common.Schema;
using Microsoft.Azure.Documents.Interop.Common.Schema.Edm;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Azure.Cosmos.Table
{
	internal static class EdmConverter
	{
		public static void GetPropertiesFromDocument(IList<string> selectColumns, string serializedDocument, out IDictionary<string, EntityProperty> entityProperties, out IDictionary<string, EntityProperty> documentDBProperties)
		{
			entityProperties = new Dictionary<string, EntityProperty>();
			documentDBProperties = new Dictionary<string, EntityProperty>();
			using (ITableEntityReader tableEntityReader = new TableEntityReader(serializedDocument))
			{
				tableEntityReader.Start();
				while (tableEntityReader.MoveNext())
				{
					string currentName = tableEntityReader.CurrentName;
					EntityProperty entityProperty;
					switch (tableEntityReader.CurrentType)
					{
					case DataType.Double:
						entityProperty = new EntityProperty(tableEntityReader.ReadDouble());
						break;
					case DataType.String:
						entityProperty = new EntityProperty(tableEntityReader.ReadString());
						break;
					case DataType.Binary:
						entityProperty = new EntityProperty(tableEntityReader.ReadBinary());
						break;
					case DataType.Boolean:
						entityProperty = new EntityProperty(tableEntityReader.ReadBoolean());
						break;
					case DataType.DateTime:
						entityProperty = new EntityProperty(tableEntityReader.ReadDateTime());
						break;
					case DataType.Int32:
						entityProperty = new EntityProperty(tableEntityReader.ReadInt32());
						break;
					case DataType.Int64:
						entityProperty = new EntityProperty(tableEntityReader.ReadInt64());
						break;
					case DataType.Guid:
						entityProperty = new EntityProperty(tableEntityReader.ReadGuid());
						break;
					default:
						throw new Exception(string.Format(CultureInfo.CurrentCulture, "Unexpected Edm type '{0}'", (int)tableEntityReader.CurrentType));
					}
					if (!entityProperty.IsNull())
					{
						if (EdmSchemaMapping.IsDocumentDBProperty(currentName) || currentName == "$id" || currentName == "$pk")
						{
							documentDBProperties.Add(currentName, entityProperty);
						}
						else if (selectColumns == null || selectColumns.Count == 0 || (selectColumns != null && selectColumns.Contains(currentName)))
						{
							entityProperties.Add(currentName, entityProperty);
						}
					}
				}
				if (selectColumns != null)
				{
					foreach (string selectColumn in selectColumns)
					{
						if (!entityProperties.ContainsKey(selectColumn))
						{
							entityProperties.Add(selectColumn, EntityProperty.GeneratePropertyForString(null));
						}
					}
				}
				tableEntityReader.End();
			}
		}

		public static void ValidateDocumentDBProperties(IDictionary<string, EntityProperty> documentDBProperties)
		{
			documentDBProperties.TryGetValue("$pk", out EntityProperty value);
			if (value != null && value.IsNull())
			{
				throw new Exception(string.Format(CultureInfo.CurrentCulture, "PartitionKey cannot be null"));
			}
			documentDBProperties.TryGetValue("$id", out EntityProperty value2);
			if (value2 != null && value2.IsNull())
			{
				throw new Exception(string.Format(CultureInfo.CurrentCulture, "Id cannot be null"));
			}
			documentDBProperties.TryGetValue("_etag", out EntityProperty value3);
			if (value3 != null && value3.IsNull())
			{
				throw new Exception(string.Format(CultureInfo.CurrentCulture, "Etag cannot be null"));
			}
			documentDBProperties.TryGetValue("_ts", out EntityProperty value4);
			if (value4 != null && value4.IsNull())
			{
				throw new Exception(string.Format(CultureInfo.CurrentCulture, "Timestamp cannot be null"));
			}
		}
	}
}
