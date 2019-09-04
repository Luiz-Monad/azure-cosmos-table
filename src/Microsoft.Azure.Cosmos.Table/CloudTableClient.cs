using Microsoft.Azure.Cosmos.Table.Extensions;
using Microsoft.Azure.Cosmos.Table.Queryable;
using Microsoft.Azure.Cosmos.Table.RestExecutor;
using Microsoft.Azure.Cosmos.Table.RestExecutor.TableCommand;
using Microsoft.Azure.Cosmos.Tables.SharedFiles;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Cosmos.Table
{
	public class CloudTableClient
	{
		private string _accountName;

		internal static Func<Uri, string, ConnectionPolicy, ConsistencyLevel?, IDocumentClient> DocClientCreator = 
			(accountUri, key, connectionPolicy, consistencyLevel) => 
				new DocumentClient(accountUri, key, EntityTranslator.JsonSerializerSettings, connectionPolicy, consistencyLevel);

		private Lazy<IDocumentClient> lazyDocumentClient;

		private Lazy<HttpClient> lazyHttpClient;

		internal const string LegacyCosmosTableDomain = ".table.cosmosdb.";

		internal const string CosmosTableDomain = ".table.cosmos.";

		internal const string CosmosDocumentsDomain = ".documents.";

		internal static readonly Dictionary<string, string> ReplaceMapping = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
		{
			{
				".table.cosmosdb.windows-int.net",
				".documents-test.windows-int.net"
			},
			{
				".table.cosmos.windows-int.net",
				".documents-test.windows-int.net"
			},
			{
				".table.cosmosdb.windows-ppe.net",
				".documents-staging.windows-ppe.net"
			},
			{
				".table.cosmos.windows-ppe.net",
				".documents-staging.windows-ppe.net"
			},
			{
				".table.cosmosdb.",
				".documents."
			},
			{
				".table.cosmos.",
				".documents."
			}
		};

		public StorageCredentials Credentials
		{
			get;
			private set;
		}

		public TableClientConfiguration TableClientConfiguration
		{
			get;
			private set;
		}

		public Uri BaseUri => StorageUri.PrimaryUri;

		public StorageUri StorageUri
		{
			get;
			private set;
		}

		public TableRequestOptions DefaultRequestOptions
		{
			get;
			set;
		}

		internal bool UsePathStyleUris
		{
			get;
			private set;
		}

		internal string AccountName
		{
			get
			{
				return _accountName ?? Credentials.AccountName;
			}
			private set
			{
				_accountName = value;
			}
		}

		internal IExecutor Executor
		{
			get;
			set;
		}

		internal HttpClient HttpClient => lazyHttpClient.Value;

		internal IDocumentClient DocumentClient => lazyDocumentClient.Value;

		public CloudTableClient(Uri baseUri, StorageCredentials credentials)
			: this(new StorageUri(baseUri), credentials)
		{
		}

		public CloudTableClient(StorageUri storageUri, StorageCredentials credentials)
			: this(storageUri, credentials, null)
		{
		}

		public CloudTableClient(StorageUri storageUri, StorageCredentials credentials, TableClientConfiguration configuration = null)
		{
			StorageUri = storageUri;
			TableClientConfiguration = (configuration ?? new TableClientConfiguration());
			Credentials = (credentials ?? new StorageCredentials());
			DefaultRequestOptions = new TableRequestOptions(TableRequestOptions.BaseDefaultRequestOptions)
			{
				RetryPolicy = new ExponentialRetry(),
				ConsistencyLevel = configuration?.CosmosExecutorConfiguration?.ConsistencyLevel
			};
			InitializeExecutor();
			UsePathStyleUris = CommonUtility.UsePathStyleAddressing(BaseUri);
			if (!Credentials.IsSharedKey)
			{
				AccountName = NavigationHelper.GetAccountNameFromUri(BaseUri, UsePathStyleUris);
			}
			lazyDocumentClient = new Lazy<IDocumentClient>(CreateDocumentClient);
			lazyHttpClient = new Lazy<HttpClient>(CreateHttpClient);
		}

		public virtual CloudTable GetTableReference(string tableName)
		{
			CommonUtility.AssertNotNullOrEmpty("tableName", tableName);
			return new CloudTable(tableName, this);
		}

		public virtual IEnumerable<CloudTable> ListTables(string prefix = null, TableRequestOptions requestOptions = null, OperationContext operationContext = null)
		{
			requestOptions = TableRequestOptions.ApplyDefaults(requestOptions, this);
			operationContext = (operationContext ?? new OperationContext());
			CloudTable tableReference = GetTableReference("Tables");
			return from tbl in GenerateListTablesQuery(prefix, null).ExecuteInternal(this, tableReference, requestOptions, operationContext)
			select new CloudTable(tbl["TableName"].StringValue, this);
		}

		public virtual TableResultSegment ListTablesSegmented(TableContinuationToken currentToken)
		{
			return ListTablesSegmented(null, currentToken);
		}

		public virtual TableResultSegment ListTablesSegmented(string prefix, TableContinuationToken currentToken)
		{
			return ListTablesSegmented(prefix, null, currentToken);
		}

		public virtual TableResultSegment ListTablesSegmented(string prefix, int? maxResults, TableContinuationToken currentToken, TableRequestOptions requestOptions = null, OperationContext operationContext = null)
		{
			requestOptions = TableRequestOptions.ApplyDefaults(requestOptions, this);
			operationContext = (operationContext ?? new OperationContext());
			CloudTable tableReference = GetTableReference("Tables");
			TableQuerySegment<DynamicTableEntity> tableQuerySegment = GenerateListTablesQuery(prefix, maxResults).ExecuteQuerySegmentedInternal(currentToken, this, tableReference, requestOptions, operationContext);
			return new TableResultSegment((from tbl in tableQuerySegment.Results
			select new CloudTable(tbl.Properties["TableName"].StringValue, this)).ToList())
			{
				ContinuationToken = tableQuerySegment.ContinuationToken
			};
		}

		public virtual Task<TableResultSegment> ListTablesSegmentedAsync(TableContinuationToken currentToken)
		{
			return ListTablesSegmentedAsync(currentToken, CancellationToken.None);
		}

		public virtual Task<TableResultSegment> ListTablesSegmentedAsync(TableContinuationToken currentToken, CancellationToken cancellationToken)
		{
			return ListTablesSegmentedAsync(null, currentToken, cancellationToken);
		}

		public virtual Task<TableResultSegment> ListTablesSegmentedAsync(string prefix, TableContinuationToken currentToken)
		{
			return ListTablesSegmentedAsync(prefix, currentToken, CancellationToken.None);
		}

		public virtual Task<TableResultSegment> ListTablesSegmentedAsync(string prefix, TableContinuationToken currentToken, CancellationToken cancellationToken)
		{
			return ListTablesSegmentedAsync(prefix, null, currentToken, cancellationToken);
		}

		public virtual Task<TableResultSegment> ListTablesSegmentedAsync(string prefix, int? maxResults, TableContinuationToken currentToken, TableRequestOptions requestOptions, OperationContext operationContext)
		{
			return ListTablesSegmentedAsync(prefix, maxResults, currentToken, requestOptions, operationContext, CancellationToken.None);
		}

		public virtual Task<TableResultSegment> ListTablesSegmentedAsync(string prefix, int? maxResults, TableContinuationToken currentToken, CancellationToken cancellationToken)
		{
			return ListTablesSegmentedAsync(prefix, maxResults, currentToken, null, null, cancellationToken);
		}

		public virtual async Task<TableResultSegment> ListTablesSegmentedAsync(string prefix, int? maxResults, TableContinuationToken currentToken, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			try
			{
				requestOptions = TableRequestOptions.ApplyDefaults(requestOptions, this);
				operationContext = (operationContext ?? new OperationContext());
				CloudTable tableReference = GetTableReference("Tables");
				TableQuerySegment<DynamicTableEntity> tableQuerySegment = await GenerateListTablesQuery(prefix, maxResults).ExecuteQuerySegmentedInternalAsync(currentToken, this, tableReference, requestOptions, operationContext, cancellationToken);
				return new TableResultSegment((from tbl in tableQuerySegment.Results
				select new CloudTable(tbl.Properties["TableName"].StringValue, this)).ToList())
				{
					ContinuationToken = tableQuerySegment.ContinuationToken
				};
			}
			catch (StorageException ex)
			{
				if (ex != null && ex.RequestInformation?.HttpStatusCode == 404)
				{
					return new TableResultSegment(new List<CloudTable>());
				}
				throw;
			}
		}

		internal ExpressionParser GetExpressionParser()
		{
			if (IsPremiumEndpoint())
			{
				return new TableExtensionExpressionParser();
			}
			return new ExpressionParser();
		}

		internal bool IsPremiumEndpoint()
		{
			CommonUtility.AssertNotNull("StorageUri", StorageUri);
			string text = StorageUri.PrimaryUri.OriginalString.ToLowerInvariant();
			if ((!text.Contains("https://localhost") || StorageUri.PrimaryUri.Port == 10002) && !text.Contains(".table.cosmosdb."))
			{
				return text.Contains(".table.cosmos.");
			}
			return true;
		}

		internal bool IsDocumentsEndPoint()
		{
			CommonUtility.AssertNotNull("StorageUri", StorageUri);
			return StorageUri.PrimaryUri.OriginalString.ToLowerInvariant().Contains(".documents.");
		}

		private static TableQuery<DynamicTableEntity> GenerateListTablesQuery(string prefix, int? maxResults)
		{
			TableQuery<DynamicTableEntity> tableQuery = new TableQuery<DynamicTableEntity>();
			if (!string.IsNullOrEmpty(prefix))
			{
				string givenValue = prefix + "{";
				tableQuery = tableQuery.Where(TableQuery.CombineFilters(TableQuery.GenerateFilterCondition("TableName", "ge", prefix), "and", TableQuery.GenerateFilterCondition("TableName", "lt", givenValue)));
			}
			if (maxResults.HasValue)
			{
				tableQuery = tableQuery.Take(maxResults.Value);
			}
			return tableQuery;
		}

		public virtual ServiceProperties GetServiceProperties(TableRequestOptions requestOptions = null, OperationContext operationContext = null)
		{
			requestOptions = TableRequestOptions.ApplyDefaults(requestOptions, this);
			operationContext = (operationContext ?? new OperationContext());
			return Executor.GetServicePropertiesOperation(this, requestOptions, operationContext);
		}

		public virtual Task<ServiceProperties> GetServicePropertiesAsync()
		{
			return GetServicePropertiesAsync(CancellationToken.None);
		}

		public virtual Task<ServiceProperties> GetServicePropertiesAsync(CancellationToken cancellationToken)
		{
			return GetServicePropertiesAsync(null, null, cancellationToken);
		}

		public virtual Task<ServiceProperties> GetServicePropertiesAsync(TableRequestOptions requestOptions, OperationContext operationContext)
		{
			return GetServicePropertiesAsync(requestOptions, operationContext, CancellationToken.None);
		}

		public virtual Task<ServiceProperties> GetServicePropertiesAsync(TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			requestOptions = TableRequestOptions.ApplyDefaults(requestOptions, this);
			operationContext = (operationContext ?? new OperationContext());
			return Executor.GetServicePropertiesOperationAsync(this, requestOptions, operationContext, cancellationToken);
		}

		public virtual void SetServiceProperties(ServiceProperties properties, TableRequestOptions requestOptions = null, OperationContext operationContext = null)
		{
			requestOptions = TableRequestOptions.ApplyDefaults(requestOptions, this);
			operationContext = (operationContext ?? new OperationContext());
			Executor.SetServicePropertiesOperation(properties, this, requestOptions, operationContext);
		}

		public virtual Task SetServicePropertiesAsync(ServiceProperties properties)
		{
			return SetServicePropertiesAsync(properties, CancellationToken.None);
		}

		public virtual Task SetServicePropertiesAsync(ServiceProperties properties, CancellationToken cancellationToken)
		{
			return SetServicePropertiesAsync(properties, null, null, cancellationToken);
		}

		public virtual Task SetServicePropertiesAsync(ServiceProperties properties, TableRequestOptions requestOptions, OperationContext operationContext)
		{
			return SetServicePropertiesAsync(properties, requestOptions, operationContext, CancellationToken.None);
		}

		public virtual Task SetServicePropertiesAsync(ServiceProperties properties, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			requestOptions = TableRequestOptions.ApplyDefaults(requestOptions, this);
			operationContext = (operationContext ?? new OperationContext());
			return Executor.SetServicePropertiesOperationAsync(properties, this, requestOptions, operationContext, cancellationToken);
		}

		public virtual ServiceStats GetServiceStats(TableRequestOptions requestOptions = null, OperationContext operationContext = null)
		{
			requestOptions = TableRequestOptions.ApplyDefaults(requestOptions, this);
			operationContext = (operationContext ?? new OperationContext());
			return Executor.GetServiceStats(this, requestOptions, operationContext);
		}

		public virtual Task<ServiceStats> GetServiceStatsAsync()
		{
			return GetServiceStatsAsync(CancellationToken.None);
		}

		public virtual Task<ServiceStats> GetServiceStatsAsync(CancellationToken cancellationToken)
		{
			return GetServiceStatsAsync(null, null, cancellationToken);
		}

		public virtual Task<ServiceStats> GetServiceStatsAsync(TableRequestOptions requestOptions, OperationContext operationContext)
		{
			return GetServiceStatsAsync(requestOptions, operationContext, CancellationToken.None);
		}

		public virtual Task<ServiceStats> GetServiceStatsAsync(TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			requestOptions = TableRequestOptions.ApplyDefaults(requestOptions, this);
			operationContext = (operationContext ?? new OperationContext());
			return Executor.GetServiceStatsAsync(this, requestOptions, operationContext, cancellationToken);
		}

		private IDocumentClient CreateDocumentClient()
		{
			if (!IsPremiumEndpoint())
			{
				throw new NotSupportedException(string.Format("{0} api is not supported in the current version.", "Direct mode"));
			}
			CosmosExecutorConfiguration cosmosExecutorConfiguration = TableClientConfiguration.CosmosExecutorConfiguration;
			ConnectionPolicy connectionPolicy = cosmosExecutorConfiguration.GetConnectionPolicy();
			Uri docDbDirectUrl = GetDocDbDirectUrl(StorageUri.PrimaryUri);
			return DocClientCreator(docDbDirectUrl, Credentials.Key, connectionPolicy, ToDocDbConsistencyLevel(cosmosExecutorConfiguration.ConsistencyLevel));
		}

		private HttpClient CreateHttpClient()
		{
			return HttpClientFactory.HttpClientFromConfiguration(TableClientConfiguration.RestExecutorConfiguration);
		}

		internal static Microsoft.Azure.Documents.ConsistencyLevel? ToDocDbConsistencyLevel(ConsistencyLevel? consistencyLevel)
		{
			if (consistencyLevel.HasValue)
			{
				return (Microsoft.Azure.Documents.ConsistencyLevel)consistencyLevel.Value;
			}
			return null;
		}

		internal static string ConvertToDocdbEndpoint(string tableEndpoint)
		{
			foreach (KeyValuePair<string, string> item in ReplaceMapping)
			{
				if (tableEndpoint.Contains(item.Key))
				{
					return tableEndpoint.Replace(item.Key, item.Value);
				}
			}
			return tableEndpoint;
		}

		internal static Uri GetDocDbDirectUrl(Uri tableUri)
		{
			string text = ConvertToDocdbEndpoint(tableUri.Host);
			return new Uri($"{tableUri.Scheme}://{text}:{tableUri.Port}{tableUri.PathAndQuery}");
		}

		internal void InitializeExecutor()
		{
			if (IsDocumentsEndPoint())
			{
				throw new NotSupportedException("Only Cosmos table endpoint or azure storage table endpoint are supported.");
			}
			if (IsPremiumEndpoint())
			{
				Executor = new TableExtensionExecutor();
			}
			else
			{
				Executor = new TableRestExecutor();
			}
		}

		internal void AssertPremiumFeaturesOnlyToCosmosTables(int? throughput, string serializedIndexingPolicy)
		{
			if ((throughput.HasValue || serializedIndexingPolicy != null) && !IsPremiumEndpoint())
			{
				throw new NotSupportedException("Only direct mode supports throughput and indexing policy.");
			}
		}
	}
}
