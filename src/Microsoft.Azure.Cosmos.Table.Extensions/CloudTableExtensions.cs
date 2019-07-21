using Microsoft.Azure.Documents.Client;
using System;

namespace Microsoft.Azure.Cosmos.Table.Extensions
{
	internal static class CloudTableExtensions
	{
		public static Uri GetCollectionUri(this CloudTable table)
		{
			return UriFactory.CreateDocumentCollectionUri("TablesDB", table.Name);
		}
	}
}
