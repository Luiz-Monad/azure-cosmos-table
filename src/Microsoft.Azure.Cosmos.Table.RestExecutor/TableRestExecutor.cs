using Microsoft.Azure.Cosmos.Table.RestExecutor.TableCommand;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor
{
	internal sealed class TableRestExecutor : IExecutor
	{
		public TResult ExecuteTableOperation<TResult>(TableOperation operation, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext) where TResult : class
		{
			return Executor.ExecuteSync(TableOperationRESTCommandGenerator.GenerateCMDForTableOperation(operation, client, table, requestOptions) as RESTCommand<TResult>, requestOptions.RetryPolicy, operationContext);
		}

		public TResult ExecuteTableBatchOperation<TResult>(TableBatchOperation operation, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext) where TResult : class
		{
			return Executor.ExecuteSync(TableBatchOperationRESTCommandGenerator.GenerateCMDForTableBatchOperation(operation, client, table, requestOptions) as RESTCommand<TResult>, requestOptions.RetryPolicy, operationContext);
		}

		public ServiceProperties GetServicePropertiesOperation(CloudTableClient client, TableRequestOptions requestOptions, OperationContext operationContext)
		{
			return Executor.ExecuteSync(TableServicePropertiesRESTCommandGenerator.GetServicePropertiesImpl(client, requestOptions), requestOptions.RetryPolicy, operationContext);
		}

		public void SetServicePropertiesOperation(ServiceProperties properties, CloudTableClient client, TableRequestOptions requestOptions, OperationContext operationContext)
		{
			Executor.ExecuteSync(TableServicePropertiesRESTCommandGenerator.SetServicePropertiesImpl(properties, client, requestOptions), requestOptions.RetryPolicy, operationContext);
		}

		public ServiceStats GetServiceStats(CloudTableClient client, TableRequestOptions requestOptions, OperationContext operationContext)
		{
			return Executor.ExecuteSync(TableServiceStatsRESTCommandGenerator.GenerateCMDForGetServiceStats(client, requestOptions), requestOptions.RetryPolicy, operationContext);
		}

		public TablePermissions GetTablePermissions(CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext)
		{
			return Executor.ExecuteSync(TablePermissionsRESTCommandGenerator.GetAclImpl(client, table, requestOptions), requestOptions.RetryPolicy, operationContext);
		}

		public void SetTablePermissions(TablePermissions permissions, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext)
		{
			Executor.ExecuteSync(TablePermissionsRESTCommandGenerator.SetAclImpl(permissions, client, table, requestOptions), requestOptions.RetryPolicy, operationContext);
		}

		public Task<TResult> ExecuteTableOperationAsync<TResult>(TableOperation operation, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken) where TResult : class
		{
			return Executor.ExecuteAsync(TableOperationRESTCommandGenerator.GenerateCMDForTableOperation(operation, client, table, requestOptions) as RESTCommand<TResult>, requestOptions.RetryPolicy, operationContext, cancellationToken);
		}

		public Task<TResult> ExecuteTableBatchOperationAsync<TResult>(TableBatchOperation operation, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken) where TResult : class
		{
			return Executor.ExecuteAsync(TableBatchOperationRESTCommandGenerator.GenerateCMDForTableBatchOperation(operation, client, table, requestOptions) as RESTCommand<TResult>, requestOptions.RetryPolicy, operationContext, cancellationToken);
		}

		public Task<ServiceProperties> GetServicePropertiesOperationAsync(CloudTableClient client, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			return Executor.ExecuteAsync(TableServicePropertiesRESTCommandGenerator.GetServicePropertiesImpl(client, requestOptions), requestOptions.RetryPolicy, operationContext, cancellationToken);
		}

		public Task SetServicePropertiesOperationAsync(ServiceProperties properties, CloudTableClient client, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			return Executor.ExecuteAsync(TableServicePropertiesRESTCommandGenerator.SetServicePropertiesImpl(properties, client, requestOptions), requestOptions.RetryPolicy, operationContext, cancellationToken);
		}

		public Task<ServiceStats> GetServiceStatsAsync(CloudTableClient client, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			return Executor.ExecuteAsync(TableServiceStatsRESTCommandGenerator.GenerateCMDForGetServiceStats(client, requestOptions), requestOptions.RetryPolicy, operationContext, cancellationToken);
		}

		public Task<TablePermissions> GetTablePermissionsAsync(CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			return Executor.ExecuteAsync(TablePermissionsRESTCommandGenerator.GetAclImpl(client, table, requestOptions), requestOptions.RetryPolicy, operationContext, cancellationToken);
		}

		public Task SetTablePermissionsAsync(TablePermissions permissions, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			return Executor.ExecuteAsync(TablePermissionsRESTCommandGenerator.SetAclImpl(permissions, client, table, requestOptions), requestOptions.RetryPolicy, operationContext, cancellationToken);
		}

		public TableQuerySegment<TResult> ExecuteQuerySegmented<TResult, TInput>(TableQuery<TInput> query, TableContinuationToken token, CloudTableClient client, CloudTable table, EntityResolver<TResult> resolver, TableRequestOptions requestOptions, OperationContext operationContext)
		{
			return Executor.ExecuteSync(TableQueryRESTCommandGenerator.GenerateCMDForTableQuery(query, token, client, table, resolver ?? new EntityResolver<TResult>(EntityUtilities.ResolveEntityByType<TResult>), requestOptions), requestOptions.RetryPolicy, operationContext);
		}

		public TableQuerySegment<TResult> ExecuteQuerySegmented<TResult>(TableQuery query, TableContinuationToken token, CloudTableClient client, CloudTable table, EntityResolver<TResult> resolver, TableRequestOptions requestOptions, OperationContext operationContext)
		{
			return Executor.ExecuteSync((typeof(TResult) == typeof(DynamicTableEntity)) ? (TableQueryRESTCommandGenerator.GenerateCMDForTableQuery(query, token, client, table, requestOptions) as RESTCommand<TableQuerySegment<TResult>>) : TableQueryRESTCommandGenerator.GenerateCMDForTableQuery(query, token, client, table, resolver ?? new EntityResolver<TResult>(EntityUtilities.ResolveEntityByType<TResult>), requestOptions), requestOptions.RetryPolicy, operationContext);
		}

		public Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<TResult, TInput>(TableQuery<TInput> query, TableContinuationToken token, CloudTableClient client, CloudTable table, EntityResolver<TResult> resolver, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			return Executor.ExecuteAsync(TableQueryRESTCommandGenerator.GenerateCMDForTableQuery(query, token, client, table, resolver ?? new EntityResolver<TResult>(EntityUtilities.ResolveEntityByType<TResult>), requestOptions), requestOptions.RetryPolicy, operationContext, cancellationToken);
		}

		public Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<TResult>(TableQuery query, TableContinuationToken token, CloudTableClient client, CloudTable table, EntityResolver<TResult> resolver, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			return Executor.ExecuteAsync((typeof(TResult) == typeof(DynamicTableEntity)) ? (TableQueryRESTCommandGenerator.GenerateCMDForTableQuery(query, token, client, table, requestOptions) as RESTCommand<TableQuerySegment<TResult>>) : TableQueryRESTCommandGenerator.GenerateCMDForTableQuery(query, token, client, table, resolver ?? new EntityResolver<TResult>(EntityUtilities.ResolveEntityByType<TResult>), requestOptions), requestOptions.RetryPolicy, operationContext, cancellationToken);
		}
	}
}
