using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Cosmos.Table
{
	internal interface IExecutor
	{
		TResult ExecuteTableOperation<TResult>(TableOperation operation, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext) where TResult : class;

		TResult ExecuteTableBatchOperation<TResult>(TableBatchOperation operation, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext) where TResult : class;

		ServiceProperties GetServicePropertiesOperation(CloudTableClient client, TableRequestOptions requestOptions, OperationContext operationContext);

		void SetServicePropertiesOperation(ServiceProperties properties, CloudTableClient client, TableRequestOptions requestOptions, OperationContext operationContext);

		ServiceStats GetServiceStats(CloudTableClient client, TableRequestOptions requestOptions, OperationContext operationContext);

		TablePermissions GetTablePermissions(CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext);

		void SetTablePermissions(TablePermissions permissions, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext);

		Task<TResult> ExecuteTableOperationAsync<TResult>(TableOperation operation, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken) where TResult : class;

		Task<TResult> ExecuteTableBatchOperationAsync<TResult>(TableBatchOperation operation, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken) where TResult : class;

		Task<ServiceProperties> GetServicePropertiesOperationAsync(CloudTableClient client, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken);

		Task SetServicePropertiesOperationAsync(ServiceProperties properties, CloudTableClient client, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken);

		Task<ServiceStats> GetServiceStatsAsync(CloudTableClient client, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken);

		Task<TablePermissions> GetTablePermissionsAsync(CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken);

		Task SetTablePermissionsAsync(TablePermissions permissions, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken);

		TableQuerySegment<TResult> ExecuteQuerySegmented<TResult, TInput>(TableQuery<TInput> query, TableContinuationToken token, CloudTableClient client, CloudTable table, EntityResolver<TResult> resolver, TableRequestOptions requestOptions, OperationContext operationContext);

		TableQuerySegment<TResult> ExecuteQuerySegmented<TResult>(TableQuery query, TableContinuationToken token, CloudTableClient client, CloudTable table, EntityResolver<TResult> resolver, TableRequestOptions requestOptions, OperationContext operationContext);

		Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<TResult, TInput>(TableQuery<TInput> query, TableContinuationToken token, CloudTableClient client, CloudTable table, EntityResolver<TResult> resolver, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken);

		Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<TResult>(TableQuery query, TableContinuationToken token, CloudTableClient client, CloudTable table, EntityResolver<TResult> resolver, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken);
	}
}
