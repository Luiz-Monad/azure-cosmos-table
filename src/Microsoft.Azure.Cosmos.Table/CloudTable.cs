using Microsoft.Azure.Cosmos.Table.Protocol;
using Microsoft.Azure.Cosmos.Table.Queryable;
using Microsoft.Azure.Cosmos.Tables.SharedFiles;
using Microsoft.Azure.Documents;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Cosmos.Table
{
	public class CloudTable
	{
		public CloudTableClient ServiceClient
		{
			get;
			private set;
		}

		public string Name
		{
			get;
			private set;
		}

		public Uri Uri => StorageUri.PrimaryUri;

		public StorageUri StorageUri
		{
			get;
			private set;
		}

		public CloudTable(Uri tableAddress)
			: this(tableAddress, null)
		{
		}

		public CloudTable(Uri tableAbsoluteUri, StorageCredentials credentials)
			: this(new StorageUri(tableAbsoluteUri), credentials)
		{
		}

		public CloudTable(StorageUri tableAddress, StorageCredentials credentials)
		{
			ParseQueryAndVerify(tableAddress, credentials);
		}

		internal CloudTable(string tableName, CloudTableClient client)
		{
			CommonUtility.AssertNotNull("tableName", tableName);
			CommonUtility.AssertNotNull("client", client);
			Name = tableName;
			StorageUri = NavigationHelper.AppendPathToUri(client.StorageUri, tableName);
			ServiceClient = client;
		}

		public override string ToString()
		{
			return Name;
		}

		public virtual TableResult Execute(TableOperation operation, TableRequestOptions requestOptions = null, OperationContext operationContext = null)
		{
			CommonUtility.AssertNotNull("operation", operation);
			return operation.Execute(ServiceClient, this, requestOptions, operationContext);
		}

		public virtual Task<TableResult> ExecuteAsync(TableOperation operation)
		{
			return ExecuteAsync(operation, CancellationToken.None);
		}

		public virtual Task<TableResult> ExecuteAsync(TableOperation operation, CancellationToken cancellationToken)
		{
			return ExecuteAsync(operation, null, null, cancellationToken);
		}

		public virtual Task<TableResult> ExecuteAsync(TableOperation operation, TableRequestOptions requestOptions, OperationContext operationContext)
		{
			return ExecuteAsync(operation, requestOptions, operationContext, CancellationToken.None);
		}

		public virtual Task<TableResult> ExecuteAsync(TableOperation operation, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			return operation.ExecuteAsync(ServiceClient, this, requestOptions, operationContext, cancellationToken);
		}

		public virtual TableBatchResult ExecuteBatch(TableBatchOperation batch, TableRequestOptions requestOptions = null, OperationContext operationContext = null)
		{
			CommonUtility.AssertNotNull("batch", batch);
			return batch.Execute(ServiceClient, this, requestOptions, operationContext);
		}

		public virtual Task<TableBatchResult> ExecuteBatchAsync(TableBatchOperation batch)
		{
			return ExecuteBatchAsync(batch, CancellationToken.None);
		}

		public virtual Task<TableBatchResult> ExecuteBatchAsync(TableBatchOperation batch, CancellationToken cancellationToken)
		{
			return ExecuteBatchAsync(batch, null, null, cancellationToken);
		}

		public virtual Task<TableBatchResult> ExecuteBatchAsync(TableBatchOperation batch, TableRequestOptions requestOptions, OperationContext operationContext)
		{
			return ExecuteBatchAsync(batch, requestOptions, operationContext, CancellationToken.None);
		}

		public virtual Task<TableBatchResult> ExecuteBatchAsync(TableBatchOperation batch, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			CommonUtility.AssertNotNull("batch", batch);
			return batch.ExecuteAsync(ServiceClient, this, requestOptions, operationContext, cancellationToken);
		}

		public virtual IEnumerable<DynamicTableEntity> ExecuteQuery(TableQuery query, TableRequestOptions requestOptions = null, OperationContext operationContext = null)
		{
			CommonUtility.AssertNotNull("query", query);
			return query.Execute(ServiceClient, this, requestOptions, operationContext);
		}

		public virtual TableQuerySegment<DynamicTableEntity> ExecuteQuerySegmented(TableQuery query, TableContinuationToken token, TableRequestOptions requestOptions = null, OperationContext operationContext = null)
		{
			CommonUtility.AssertNotNull("query", query);
			return query.ExecuteQuerySegmented(token, ServiceClient, this, requestOptions, operationContext);
		}

		public virtual Task<TableQuerySegment<DynamicTableEntity>> ExecuteQuerySegmentedAsync(TableQuery query, TableContinuationToken token)
		{
			return ExecuteQuerySegmentedAsync(query, token, CancellationToken.None);
		}

		public virtual Task<TableQuerySegment<DynamicTableEntity>> ExecuteQuerySegmentedAsync(TableQuery query, TableContinuationToken token, CancellationToken cancellationToken)
		{
			return ExecuteQuerySegmentedAsync(query, token, null, null, cancellationToken);
		}

		public virtual Task<TableQuerySegment<DynamicTableEntity>> ExecuteQuerySegmentedAsync(TableQuery query, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext)
		{
			return ExecuteQuerySegmentedAsync(query, token, requestOptions, operationContext, CancellationToken.None);
		}

		public virtual Task<TableQuerySegment<DynamicTableEntity>> ExecuteQuerySegmentedAsync(TableQuery query, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			CommonUtility.AssertNotNull("query", query);
			return query.ExecuteQuerySegmentedAsync(token, ServiceClient, this, requestOptions, operationContext, cancellationToken);
		}

		public virtual IEnumerable<TResult> ExecuteQuery<TResult>(TableQuery query, EntityResolver<TResult> resolver, TableRequestOptions requestOptions = null, OperationContext operationContext = null)
		{
			CommonUtility.AssertNotNull("query", query);
			CommonUtility.AssertNotNull("resolver", resolver);
			return query.Execute(ServiceClient, this, resolver, requestOptions, operationContext);
		}

		public virtual TableQuerySegment<TResult> ExecuteQuerySegmented<TResult>(TableQuery query, EntityResolver<TResult> resolver, TableContinuationToken token, TableRequestOptions requestOptions = null, OperationContext operationContext = null)
		{
			CommonUtility.AssertNotNull("query", query);
			return query.ExecuteQuerySegmented(token, ServiceClient, this, resolver, requestOptions, operationContext);
		}

		public virtual Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<TResult>(TableQuery query, EntityResolver<TResult> resolver, TableContinuationToken token)
		{
			return ExecuteQuerySegmentedAsync(query, resolver, token, CancellationToken.None);
		}

		public virtual Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<TResult>(TableQuery query, EntityResolver<TResult> resolver, TableContinuationToken token, CancellationToken cancellationToken)
		{
			return ExecuteQuerySegmentedAsync(query, resolver, token, null, null, cancellationToken);
		}

		public virtual Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<TResult>(TableQuery query, EntityResolver<TResult> resolver, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext)
		{
			return ExecuteQuerySegmentedAsync(query, resolver, token, requestOptions, operationContext, CancellationToken.None);
		}

		public virtual Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<TResult>(TableQuery query, EntityResolver<TResult> resolver, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			CommonUtility.AssertNotNull("query", query);
			CommonUtility.AssertNotNull("resolver", resolver);
			return query.ExecuteQuerySegmentedAsync(token, ServiceClient, this, resolver, requestOptions, operationContext, cancellationToken);
		}

		public virtual TableQuery<TElement> CreateQuery<TElement>() where TElement : ITableEntity, new()
		{
			return new TableQuery<TElement>(this);
		}

		public virtual IEnumerable<TElement> ExecuteQuery<TElement>(TableQuery<TElement> query, TableRequestOptions requestOptions = null, OperationContext operationContext = null) where TElement : ITableEntity, new()
		{
			CommonUtility.AssertNotNull("query", query);
			if (query.Provider != null)
			{
				return query.Execute(requestOptions, operationContext);
			}
			return query.ExecuteInternal(ServiceClient, this, requestOptions, operationContext);
		}

		public virtual TableQuerySegment<TElement> ExecuteQuerySegmented<TElement>(TableQuery<TElement> query, TableContinuationToken token, TableRequestOptions requestOptions = null, OperationContext operationContext = null) where TElement : ITableEntity, new()
		{
			CommonUtility.AssertNotNull("query", query);
			if (query.Provider != null)
			{
				return query.ExecuteSegmented(token, requestOptions, operationContext);
			}
			return query.ExecuteQuerySegmentedInternal(token, ServiceClient, this, requestOptions, operationContext);
		}

		public virtual Task<TableQuerySegment<TElement>> ExecuteQuerySegmentedAsync<TElement>(TableQuery<TElement> query, TableContinuationToken token) where TElement : ITableEntity, new()
		{
			return ExecuteQuerySegmentedAsync(query, token, CancellationToken.None);
		}

		public virtual Task<TableQuerySegment<TElement>> ExecuteQuerySegmentedAsync<TElement>(TableQuery<TElement> query, TableContinuationToken token, CancellationToken cancellationToken) where TElement : ITableEntity, new()
		{
			return ExecuteQuerySegmentedAsync(query, token, null, null, cancellationToken);
		}

		public virtual Task<TableQuerySegment<TElement>> ExecuteQuerySegmentedAsync<TElement>(TableQuery<TElement> query, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext) where TElement : ITableEntity, new()
		{
			return ExecuteQuerySegmentedAsync(query, token, requestOptions, operationContext, CancellationToken.None);
		}

		public virtual Task<TableQuerySegment<TElement>> ExecuteQuerySegmentedAsync<TElement>(TableQuery<TElement> query, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken) where TElement : ITableEntity, new()
		{
			CommonUtility.AssertNotNull("query", query);
			if (query.Provider != null)
			{
				return query.ExecuteSegmentedAsync(token, requestOptions, operationContext, cancellationToken);
			}
			return query.ExecuteQuerySegmentedInternalAsync(token, ServiceClient, this, requestOptions, operationContext, cancellationToken);
		}

		public virtual IEnumerable<TResult> ExecuteQuery<TElement, TResult>(TableQuery<TElement> query, EntityResolver<TResult> resolver, TableRequestOptions requestOptions = null, OperationContext operationContext = null) where TElement : ITableEntity, new()
		{
			CommonUtility.AssertNotNull("query", query);
			CommonUtility.AssertNotNull("resolver", resolver);
			if (query.Provider != null)
			{
				return query.Resolve(resolver).Execute(requestOptions, operationContext);
			}
			return query.ExecuteInternal(ServiceClient, this, resolver, requestOptions, operationContext);
		}

		public virtual TableQuerySegment<TResult> ExecuteQuerySegmented<TElement, TResult>(TableQuery<TElement> query, EntityResolver<TResult> resolver, TableContinuationToken token, TableRequestOptions requestOptions = null, OperationContext operationContext = null) where TElement : ITableEntity, new()
		{
			CommonUtility.AssertNotNull("query", query);
			CommonUtility.AssertNotNull("resolver", resolver);
			if (query.Provider != null)
			{
				return query.Resolve(resolver).ExecuteSegmented(token, requestOptions, operationContext);
			}
			return query.ExecuteQuerySegmentedInternal(token, ServiceClient, this, resolver, requestOptions, operationContext);
		}

		public virtual Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<TElement, TResult>(TableQuery<TElement> query, EntityResolver<TResult> resolver, TableContinuationToken token) where TElement : ITableEntity, new()
		{
			return ExecuteQuerySegmentedAsync(query, resolver, token, CancellationToken.None);
		}

		public virtual Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<TElement, TResult>(TableQuery<TElement> query, EntityResolver<TResult> resolver, TableContinuationToken token, CancellationToken cancellationToken) where TElement : ITableEntity, new()
		{
			return ExecuteQuerySegmentedAsync(query, resolver, token, null, null, cancellationToken);
		}

		public virtual Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<TElement, TResult>(TableQuery<TElement> query, EntityResolver<TResult> resolver, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext) where TElement : ITableEntity, new()
		{
			return ExecuteQuerySegmentedAsync(query, resolver, token, requestOptions, operationContext, CancellationToken.None);
		}

		public virtual Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<TElement, TResult>(TableQuery<TElement> query, EntityResolver<TResult> resolver, TableContinuationToken token, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken) where TElement : ITableEntity, new()
		{
			CommonUtility.AssertNotNull("query", query);
			CommonUtility.AssertNotNull("resolver", resolver);
			if (query.Provider != null)
			{
				return query.Resolve(resolver).ExecuteSegmentedAsync(token, requestOptions, operationContext, cancellationToken);
			}
			return query.ExecuteQuerySegmentedInternalAsync(token, ServiceClient, this, resolver, requestOptions, operationContext, cancellationToken);
		}

		public virtual void Create(IndexingMode? indexingMode, int? throughput = default(int?))
		{
			Create(null, null, ToSerializedIndexingPolicy(indexingMode), throughput);
		}

		public virtual void Create(TableRequestOptions requestOptions = null, OperationContext operationContext = null, string serializedIndexingPolicy = null, int? throughput = default(int?))
		{
			ServiceClient.AssertPremiumFeaturesOnlyToCosmosTables(throughput, serializedIndexingPolicy);
			requestOptions = TableRequestOptions.ApplyDefaults(requestOptions, ServiceClient);
			operationContext = (operationContext ?? new OperationContext());
			DynamicTableEntity dynamicTableEntity = new DynamicTableEntity();
			dynamicTableEntity.SetCosmosTableName(Name);
			dynamicTableEntity.SetCosmosTableThroughput(throughput);
			dynamicTableEntity.SetCosmosTableIndexingPolicy(serializedIndexingPolicy);
			new TableOperation(dynamicTableEntity, TableOperationType.Insert, echoContent: false)
			{
				IsTableEntity = true
			}.Execute(table: ServiceClient.GetTableReference("Tables"), client: ServiceClient, requestOptions: requestOptions, operationContext: operationContext);
		}

		public virtual Task CreateAsync()
		{
			return CreateAsync(CancellationToken.None);
		}

		public virtual Task CreateAsync(CancellationToken cancellationToken)
		{
			return CreateAsync((int?)null, (string)null, cancellationToken);
		}

		public virtual Task CreateAsync(int? throughput, IndexingMode indexingMode, CancellationToken cancellationToken)
		{
			return CreateAsync(throughput, ToSerializedIndexingPolicy(indexingMode), cancellationToken);
		}

		public virtual Task CreateAsync(int? throughput, string serializedIndexingPolicy, CancellationToken cancellationToken)
		{
			return CreateAsync(null, null, serializedIndexingPolicy, throughput, cancellationToken);
		}

		public virtual Task CreateAsync(TableRequestOptions requestOptions, OperationContext operationContext)
		{
			return CreateAsync(requestOptions, operationContext, CancellationToken.None);
		}

		public virtual Task CreateAsync(TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			return CreateAsync(requestOptions, operationContext, null, null, cancellationToken);
		}

		public virtual Task CreateAsync(TableRequestOptions requestOptions, OperationContext operationContext, string serializedIndexingPolicy, int? throughput, CancellationToken cancellationToken)
		{
			ServiceClient.AssertPremiumFeaturesOnlyToCosmosTables(throughput, serializedIndexingPolicy);
			requestOptions = TableRequestOptions.ApplyDefaults(requestOptions, ServiceClient);
			operationContext = (operationContext ?? new OperationContext());
			DynamicTableEntity dynamicTableEntity = new DynamicTableEntity();
			dynamicTableEntity.SetCosmosTableName(Name);
			dynamicTableEntity.SetCosmosTableThroughput(throughput);
			dynamicTableEntity.SetCosmosTableIndexingPolicy(serializedIndexingPolicy);
			return new TableOperation(dynamicTableEntity, TableOperationType.Insert, echoContent: false)
			{
				IsTableEntity = true
			}.ExecuteAsync(table: ServiceClient.GetTableReference("Tables"), client: ServiceClient, requestOptions: requestOptions, operationContext: operationContext, cancellationToken: cancellationToken);
		}

		public virtual bool CreateIfNotExists(IndexingMode indexingMode, int? throughput = default(int?))
		{
			return CreateIfNotExists(null, null, ToSerializedIndexingPolicy(indexingMode), throughput);
		}

		public virtual bool CreateIfNotExists(TableRequestOptions requestOptions = null, OperationContext operationContext = null, string serializedIndexingPolicy = null, int? throughput = default(int?))
		{
			try
			{
				Create(requestOptions, operationContext, serializedIndexingPolicy, throughput);
				return true;
			}
			catch (StorageException ex)
			{
				if (ex.RequestInformation.HttpStatusCode != 409 || (ex.RequestInformation.ExtendedErrorInformation != null && !(ex.RequestInformation.ExtendedErrorInformation.ErrorCode == TableErrorCodeStrings.TableAlreadyExists)))
				{
					throw;
				}
				return false;
			}
		}

		public virtual Task<bool> CreateIfNotExistsAsync()
		{
			return CreateIfNotExistsAsync(CancellationToken.None);
		}

		public virtual Task<bool> CreateIfNotExistsAsync(CancellationToken cancellationToken)
		{
			return CreateIfNotExistsAsync(null, null, cancellationToken);
		}

		public virtual Task<bool> CreateIfNotExistsAsync(TableRequestOptions requestOptions, OperationContext operationContext)
		{
			return CreateIfNotExistsAsync(requestOptions, operationContext, null, null, CancellationToken.None);
		}

		public virtual Task<bool> CreateIfNotExistsAsync(TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			return CreateIfNotExistsAsync(requestOptions, operationContext, null, null, cancellationToken);
		}

		public virtual Task<bool> CreateIfNotExistsAsync(IndexingMode indexingMode, int? throughput, CancellationToken cancellationToken)
		{
			return CreateIfNotExistsAsync(null, null, ToSerializedIndexingPolicy(indexingMode), throughput, cancellationToken);
		}

		public virtual async Task<bool> CreateIfNotExistsAsync(TableRequestOptions requestOptions, OperationContext operationContext, string serializedIndexingPolicy, int? throughput, CancellationToken cancellationToken)
		{
			TableRequestOptions requestOptions2 = TableRequestOptions.ApplyDefaults(requestOptions, ServiceClient);
			operationContext = (operationContext ?? new OperationContext());
			try
			{
				await CreateAsync(requestOptions2, operationContext, serializedIndexingPolicy, throughput, cancellationToken);
				return true;
			}
			catch (StorageException ex)
			{
				if (ex.RequestInformation.HttpStatusCode == 409 && (ex.RequestInformation.ExtendedErrorInformation == null || ex.RequestInformation.ExtendedErrorInformation.ErrorCode == TableErrorCodeStrings.TableAlreadyExists))
				{
					return false;
				}
				throw;
			}
			catch (Exception)
			{
				throw;
			}
		}

		public virtual void Delete(TableRequestOptions requestOptions = null, OperationContext operationContext = null)
		{
			requestOptions = TableRequestOptions.ApplyDefaults(requestOptions, ServiceClient);
			operationContext = (operationContext ?? new OperationContext());
			new TableOperation(new DynamicTableEntity
			{
				Properties = 
				{
					{
						"TableName",
						new EntityProperty(Name)
					}
				}
			}, TableOperationType.Delete)
			{
				IsTableEntity = true
			}.Execute(table: ServiceClient.GetTableReference("Tables"), client: ServiceClient, requestOptions: requestOptions, operationContext: operationContext);
		}

		public virtual Task DeleteAsync()
		{
			return DeleteAsync(CancellationToken.None);
		}

		public virtual Task DeleteAsync(CancellationToken cancellationToken)
		{
			return DeleteAsync(null, null, cancellationToken);
		}

		public virtual Task DeleteAsync(TableRequestOptions requestOptions, OperationContext operationContext)
		{
			return DeleteAsync(requestOptions, operationContext, CancellationToken.None);
		}

		public virtual Task DeleteAsync(TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			requestOptions = TableRequestOptions.ApplyDefaults(requestOptions, ServiceClient);
			operationContext = (operationContext ?? new OperationContext());
			return new TableOperation(new DynamicTableEntity
			{
				Properties = 
				{
					{
						"TableName",
						new EntityProperty(Name)
					}
				}
			}, TableOperationType.Delete)
			{
				IsTableEntity = true
			}.ExecuteAsync(table: ServiceClient.GetTableReference("Tables"), client: ServiceClient, requestOptions: requestOptions, operationContext: operationContext, cancellationToken: cancellationToken);
		}

		public virtual bool DeleteIfExists(TableRequestOptions requestOptions = null, OperationContext operationContext = null)
		{
			TableRequestOptions requestOptions2 = TableRequestOptions.ApplyDefaults(requestOptions, ServiceClient);
			operationContext = (operationContext ?? new OperationContext());
			try
			{
				if (!Exists(requestOptions2, operationContext))
				{
					return false;
				}
			}
			catch (StorageException ex)
			{
				if (ex.RequestInformation.HttpStatusCode != 403)
				{
					throw;
				}
			}
			try
			{
				Delete(requestOptions2, operationContext);
				return true;
			}
			catch (StorageException ex2)
			{
				if (ex2.RequestInformation.HttpStatusCode != 404)
				{
					throw;
				}
				if (ex2.RequestInformation.ExtendedErrorInformation != null && !(ex2.RequestInformation.ExtendedErrorInformation.ErrorCode == StorageErrorCodeStrings.ResourceNotFound))
				{
					throw;
				}
				return false;
			}
		}

		public virtual Task<bool> DeleteIfExistsAsync()
		{
			return DeleteIfExistsAsync(CancellationToken.None);
		}

		public virtual Task<bool> DeleteIfExistsAsync(CancellationToken cancellationToken)
		{
			return DeleteIfExistsAsync(null, null, cancellationToken);
		}

		public virtual Task<bool> DeleteIfExistsAsync(TableRequestOptions requestOptions, OperationContext operationContext)
		{
			return DeleteIfExistsAsync(requestOptions, operationContext, CancellationToken.None);
		}

		public virtual async Task<bool> DeleteIfExistsAsync(TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			requestOptions = TableRequestOptions.ApplyDefaults(requestOptions, ServiceClient);
			operationContext = (operationContext ?? new OperationContext());
			try
			{
				if (!(await ExistsAsync(requestOptions, operationContext, cancellationToken)))
				{
					return false;
				}
			}
			catch (StorageException ex)
			{
				if (ex.RequestInformation != null && ex.RequestInformation.HttpStatusCode != 403)
				{
					throw;
				}
			}
			catch (Exception)
			{
				throw;
			}
			cancellationToken.ThrowIfCancellationRequested();
			try
			{
				await DeleteAsync(requestOptions, operationContext, cancellationToken);
				return true;
			}
			catch (StorageException ex3)
			{
				if (ex3.RequestInformation.HttpStatusCode == 404 && (ex3.RequestInformation.ExtendedErrorInformation == null || ex3.RequestInformation.ExtendedErrorInformation.ErrorCode == StorageErrorCodeStrings.ResourceNotFound))
				{
					return false;
				}
				throw;
			}
			catch (Exception)
			{
				throw;
			}
		}

		public bool Exists(TableRequestOptions requestOptions = null, OperationContext operationContext = null)
		{
			requestOptions = TableRequestOptions.ApplyDefaults(requestOptions, ServiceClient);
			operationContext = (operationContext ?? new OperationContext());
			return new TableOperation(new DynamicTableEntity
			{
				Properties = 
				{
					{
						"TableName",
						new EntityProperty(Name)
					}
				}
			}, TableOperationType.Retrieve)
			{
				IsTableEntity = true
			}.Execute(table: ServiceClient.GetTableReference("Tables"), client: ServiceClient, requestOptions: requestOptions, operationContext: operationContext).HttpStatusCode == 200;
		}

		public virtual Task<bool> ExistsAsync()
		{
			return ExistsAsync(CancellationToken.None);
		}

		public virtual Task<bool> ExistsAsync(CancellationToken cancellationToken)
		{
			return ExistsAsync(null, null, cancellationToken);
		}

		public virtual Task<bool> ExistsAsync(TableRequestOptions requestOptions, OperationContext operationContext)
		{
			return ExistsAsync(requestOptions, operationContext, CancellationToken.None);
		}

		public virtual async Task<bool> ExistsAsync(TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			requestOptions = TableRequestOptions.ApplyDefaults(requestOptions, ServiceClient);
			operationContext = (operationContext ?? new OperationContext());
			return (await new TableOperation(new DynamicTableEntity
			{
				Properties = 
				{
					{
						"TableName",
						new EntityProperty(Name)
					}
				}
			}, TableOperationType.Retrieve)
			{
				IsTableEntity = true
			}.ExecuteAsync(table: ServiceClient.GetTableReference("Tables"), client: ServiceClient, requestOptions: requestOptions, operationContext: operationContext, cancellationToken: cancellationToken)).HttpStatusCode == 200;
		}

		private static string ToSerializedIndexingPolicy(IndexingMode? mode)
		{
			IndexingPolicy indexingPolicy = new IndexingPolicy();
			if (mode.HasValue)
			{
				indexingPolicy.IndexingMode = (Microsoft.Azure.Documents.IndexingMode)mode.Value;
			}
			return JsonConvert.SerializeObject(indexingPolicy);
		}

		private void ParseQueryAndVerify(StorageUri address, StorageCredentials credentials)
		{
			StorageUri = NavigationHelper.ParseTableQueryAndVerify(address, out StorageCredentials parsedCredentials);
			if (parsedCredentials != null && credentials != null)
			{
				throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "Cannot provide credentials as part of the address and as constructor parameter. Either pass in the address or use a different constructor."));
			}
			ServiceClient = new CloudTableClient(NavigationHelper.GetServiceClientBaseAddress(StorageUri, null), credentials ?? parsedCredentials);
			Name = NavigationHelper.GetTableNameFromUri(Uri, ServiceClient.UsePathStyleUris);
		}

		public virtual TablePermissions GetPermissions(TableRequestOptions requestOptions = null, OperationContext operationContext = null)
		{
			requestOptions = TableRequestOptions.ApplyDefaults(requestOptions, ServiceClient);
			operationContext = (operationContext ?? new OperationContext());
			return ServiceClient.Executor.GetTablePermissions(ServiceClient, this, requestOptions, operationContext);
		}

		public virtual Task<TablePermissions> GetPermissionsAsync()
		{
			return GetPermissionsAsync(CancellationToken.None);
		}

		public virtual Task<TablePermissions> GetPermissionsAsync(CancellationToken cancellationToken)
		{
			return GetPermissionsAsync(null, null, cancellationToken);
		}

		public virtual Task<TablePermissions> GetPermissionsAsync(TableRequestOptions requestOptions, OperationContext operationContext)
		{
			return GetPermissionsAsync(requestOptions, operationContext, CancellationToken.None);
		}

		public virtual Task<TablePermissions> GetPermissionsAsync(TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			requestOptions = TableRequestOptions.ApplyDefaults(requestOptions, ServiceClient);
			operationContext = (operationContext ?? new OperationContext());
			return ServiceClient.Executor.GetTablePermissionsAsync(ServiceClient, this, requestOptions, operationContext, cancellationToken);
		}

		public virtual void SetPermissions(TablePermissions permissions, TableRequestOptions requestOptions = null, OperationContext operationContext = null)
		{
			requestOptions = TableRequestOptions.ApplyDefaults(requestOptions, ServiceClient);
			operationContext = (operationContext ?? new OperationContext());
			ServiceClient.Executor.SetTablePermissions(permissions, ServiceClient, this, requestOptions, operationContext);
		}

		public virtual Task SetPermissionsAsync(TablePermissions permissions)
		{
			return SetPermissionsAsync(permissions, CancellationToken.None);
		}

		public virtual Task SetPermissionsAsync(TablePermissions permissions, CancellationToken cancellationToken)
		{
			return SetPermissionsAsync(permissions, null, null, cancellationToken);
		}

		public virtual Task SetPermissionsAsync(TablePermissions permissions, TableRequestOptions requestOptions, OperationContext operationContext)
		{
			return SetPermissionsAsync(permissions, requestOptions, operationContext, CancellationToken.None);
		}

		public virtual Task SetPermissionsAsync(TablePermissions permissions, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			requestOptions = TableRequestOptions.ApplyDefaults(requestOptions, ServiceClient);
			operationContext = (operationContext ?? new OperationContext());
			return ServiceClient.Executor.SetTablePermissionsAsync(permissions, ServiceClient, this, requestOptions, operationContext, cancellationToken);
		}

		public string GetSharedAccessSignature(SharedAccessTablePolicy policy)
		{
			return GetSharedAccessSignature(policy, null, null, null, null, null);
		}

		public string GetSharedAccessSignature(SharedAccessTablePolicy policy, string accessPolicyIdentifier)
		{
			return GetSharedAccessSignature(policy, accessPolicyIdentifier, null, null, null, null);
		}

		public string GetSharedAccessSignature(SharedAccessTablePolicy policy, string accessPolicyIdentifier, string startPartitionKey, string startRowKey, string endPartitionKey, string endRowKey)
		{
			return GetSharedAccessSignature(policy, accessPolicyIdentifier, startPartitionKey, startRowKey, endPartitionKey, endRowKey, null, null);
		}

		public string GetSharedAccessSignature(SharedAccessTablePolicy policy, string accessPolicyIdentifier, string startPartitionKey, string startRowKey, string endPartitionKey, string endRowKey, SharedAccessProtocol? protocols, IPAddressOrRange ipAddressOrRange)
		{
			if (!ServiceClient.Credentials.IsSharedKey)
			{
				throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Cannot create Shared Access Signature unless Account Key credentials are used."));
			}
			string canonicalName = GetCanonicalName();
			StorageCredentials credentials = ServiceClient.Credentials;
			string hash = SharedAccessSignatureHelper.GetHash(policy, accessPolicyIdentifier, startPartitionKey, startRowKey, endPartitionKey, endRowKey, canonicalName, "2018-03-28", protocols, ipAddressOrRange, credentials.Key);
			return SharedAccessSignatureHelper.GetSignature(policy, Name, accessPolicyIdentifier, startPartitionKey, startRowKey, endPartitionKey, endRowKey, hash, credentials.KeyName, "2018-03-28", protocols, ipAddressOrRange).ToString();
		}

		private string GetCanonicalName()
		{
			return string.Format(CultureInfo.InvariantCulture, "/{0}/{1}/{2}", "table", ServiceClient.Credentials.AccountName, Name.ToLower());
		}
	}
}
