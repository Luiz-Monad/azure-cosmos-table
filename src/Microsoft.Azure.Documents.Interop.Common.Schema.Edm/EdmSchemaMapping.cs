using System.Collections.Generic;

namespace Microsoft.Azure.Documents.Interop.Common.Schema.Edm
{
	internal static class EdmSchemaMapping
	{
		public static class Table
		{
			public const string RowKey = "RowKey";

			public const string PartitionKey = "PartitionKey";

			public const string Etag = "Etag";

			public const string Timestamp = "Timestamp";
		}

		public static class DocumentDB
		{
			public const string Id = "id";

			public const string Etag = "_etag";

			public const string Timestamp = "_ts";

			public const string Rid = "_rid";

			public const string Self = "_self";

			public const string Attachments = "_attachments";

			public const string PartitionKey = "$pk";

			public const string IndexedId = "$id";
		}

		public static readonly string EntityName;

		public static readonly string SystemPropertiesPrefix;

		public static readonly Dictionary<string, string> SystemPropertiesMapping;

		static EdmSchemaMapping()
		{
			EntityName = "entity";
			SystemPropertiesPrefix = "$";
			SystemPropertiesMapping = new Dictionary<string, string>();
			SystemPropertiesMapping.Add("PartitionKey", "$pk");
			SystemPropertiesMapping.Add("RowKey", "$id");
			SystemPropertiesMapping.Add("Etag", "_etag");
			SystemPropertiesMapping.Add("Timestamp", "_ts");
		}

		public static bool IsDocumentDBProperty(string name)
		{
			if (!(name == "_rid") && !(name == "_self") && !(name == "_etag") && !(name == "_attachments") && !(name == "_ts"))
			{
				return name == "id";
			}
			return true;
		}
	}
}
