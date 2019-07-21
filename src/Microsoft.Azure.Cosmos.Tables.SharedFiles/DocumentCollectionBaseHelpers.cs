using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Internals;
using Microsoft.Azure.Documents.Linq;
using System;
using System.Linq;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Cosmos.Tables.SharedFiles
{
	internal sealed class DocumentCollectionBaseHelpers
	{
		private const int DefaultOfferThroughput = 800;

		private const string TestPartitionKey = "{D345FA99-84DC-48CC-BFCE-2CF8ED99D80D}";

		private const string TestRowKey = "{E18FAFBA-1953-4CB9-83B8-066D2121717D}";

		public static async Task<ResourceResponse<DocumentCollection>> HandleDocumentCollectionRetrieveAsync(string tableName, IDocumentClient client)
		{
			Uri uri = UriFactory.CreateDocumentCollectionUri("TablesDB", tableName);
			return await client.ReadDocumentCollectionAsync(uri.ToString());
		}

		public static async Task<ResourceResponse<DocumentCollection>> HandleCollectionFeedInsertAsync(
			IDocumentClient client, string collectionName, IndexingPolicy indexingPolicy, RequestOptions requestOption)
		{
			await ThrowIfCollectionExists(client, collectionName);
			RequestOptions requestOptions = requestOption;
			if ((requestOptions == null || !requestOptions.OfferThroughput.HasValue) && !(await IsSharedThroughputEnabledAsync(client)))
			{
				if (requestOption == null)
				{
					requestOption = new RequestOptions();
				}
				requestOption.OfferThroughput = 800;
			}
			ResourceResponse<DocumentCollection> collectionResponse2 = null;
			try
			{
				collectionResponse2 = await CreateDocumentCollectionAsync(collectionName, client, indexingPolicy, requestOption);
			}
			catch (DocumentClientException ex)
			{
				if (ex.StatusCode != HttpStatusCode.NotFound)
				{
					Exception obj = ex as Exception;
					if (obj == null)
					{
						throw ex;
					}
					ExceptionDispatchInfo.Capture(obj).Throw();
				}
				await CreateTablesDB(client);
				collectionResponse2 = await CreateDocumentCollectionAsync(collectionName, client, indexingPolicy, requestOption);
			}
			return collectionResponse2;
		}

		public static async Task<ResourceResponse<Database>> CreateTablesDB(IDocumentClient client)
		{
			ResourceResponse<Database> response = null;
			try
			{
				response = await client.CreateDatabaseAsync(new Documents.Database
				{
					Id = "TablesDB"
				});
				return response;
			}
			catch (DocumentClientException ex)
			{
				if (ex.StatusCode != HttpStatusCode.Conflict)
				{
					throw;
				}
				return response;
			}
		}

		private static async Task<ResourceResponse<DocumentCollection>> CreateDocumentCollectionAsync(
			string collectionName, IDocumentClient client, IndexingPolicy indexingPolicy, RequestOptions requestOption)
		{
			Uri uri = UriFactory.CreateDatabaseUri("TablesDB");
			return await client.CreateDocumentCollectionAsync(uri.ToString(), new DocumentCollection
			{
				Id = collectionName,
				PartitionKey = new PartitionKeyDefinition
				{
					Paths = 
					{
						"/'$pk'"
					}
				},
				IndexingPolicy = (indexingPolicy != null
					? new Documents.IndexingPolicy() {
						IndexingMode = (Microsoft.Azure.Documents.IndexingMode)indexingPolicy.IndexingMode
					}
					: new Documents.IndexingPolicy(
						Index.Range(Documents.DataType.Number, -1), Index.Range(Documents.DataType.String, -1)))
			}, requestOption);
		}

		private static async Task<bool> IsSharedThroughputEnabledAsync(IDocumentClient client)
		{
			Uri databaseUri = UriFactory.CreateDatabaseUri("TablesDB");
			try
			{
				return (await client.CreateOfferQuery($"SELECT * FROM offers o WHERE o.resource = '{(await client.ReadDatabaseAsync(databaseUri)).Resource.SelfLink}'")
					.AsDocumentQuery().ExecuteNextAsync<Offer>()).AsEnumerable().SingleOrDefault() != null;
			}
			// catch (NotFoundException)
			// {
			// 	return false;
			// }
			catch (DocumentClientException ex2)
			{
				if (ex2.StatusCode.HasValue && ex2.StatusCode.Value.Equals(HttpStatusCode.NotFound))
				{
					return false;
				}
				throw;
			}
			catch (Exception ex) 
			{
				if (ex.GetType().Name == "NotFoundException") 
				{
					return false;
				}
				throw;
			}
		}

		private static async Task ThrowIfCollectionExists(IDocumentClient client, string tableName)
		{
			bool collectionExists = false;
			try
			{
				await DocumentEntityCollectionBaseHelpers.HandleEntityRetrieveAsync(tableName, 
					"{D345FA99-84DC-48CC-BFCE-2CF8ED99D80D}", "{E18FAFBA-1953-4CB9-83B8-066D2121717D}", client, null, CancellationToken.None);
				collectionExists = true;
			}
			catch (DocumentClientException)
			{
				// if (ex.StatusCode == HttpStatusCode.NotFound && !string.IsNullOrWhiteSpace(ex.ResourceAddress) && 
				// 	ex.ResourceAddress.ToLowerInvariant().Contains("/docs/"))
				// {
				// 	collectionExists = true;
				// }
			}
			if (collectionExists)
			{
				throw new DocumentClientExceptionInternal("The specified table already exists.", HttpStatusCode.Conflict, null);
			}
		}
	}
}
