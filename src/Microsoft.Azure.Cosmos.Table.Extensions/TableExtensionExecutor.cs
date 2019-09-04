using Microsoft.Azure.Cosmos.Tables.SharedFiles;
using Microsoft.Azure.Documents.Client.Internals;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Cosmos.Table.Extensions
{
	internal sealed class TableExtensionExecutor : IExecutor
	{
		internal string TombstoneFieldName
		{
			get;
			set;
		}

		public TableExtensionExecutor()
		{
			TombstoneFieldName = null;
		}

		public TResult ExecuteTableOperation<TResult>(TableOperation operation, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext) where TResult : class
		{
			return TaskHelper.InlineIfPossible(() => ExecuteTableOperationAsync<TResult>(operation, client, table, requestOptions, operationContext, CancellationToken.None), null).GetAwaiter().GetResult();
		}

		public TResult ExecuteTableBatchOperation<TResult>(TableBatchOperation operation, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext) where TResult : class
		{
			return TaskHelper.InlineIfPossible(() => ExecuteTableBatchOperationAsync<TResult>(operation, client, table, requestOptions, operationContext, CancellationToken.None), null).GetAwaiter().GetResult();
		}

		public ServiceProperties GetServicePropertiesOperation(CloudTableClient client, TableRequestOptions requestOptions, OperationContext operationContext)
		{
			throw new NotSupportedException("The operation is not supported by Azure Cosmos Table endpoints. ");
		}

		public void SetServicePropertiesOperation(ServiceProperties properties, CloudTableClient client, TableRequestOptions requestOptions, OperationContext operationContext)
		{
			throw new NotSupportedException("The operation is not supported by Azure Cosmos Table endpoints. ");
		}

		public ServiceStats GetServiceStats(CloudTableClient client, TableRequestOptions requestOptions, OperationContext operationContext)
		{
			throw new NotSupportedException("The operation is not supported by Azure Cosmos Table endpoints. ");
		}

		public TablePermissions GetTablePermissions(CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext)
		{
			throw new NotSupportedException("The operation is not supported by Azure Cosmos Table endpoints. ");
		}

		public void SetTablePermissions(TablePermissions permissions, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext)
		{
			throw new NotSupportedException("The operation is not supported by Azure Cosmos Table endpoints. ");
		}

		public Task<TResult> ExecuteTableOperationAsync<TResult>(TableOperation operation, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken) where TResult : class
		{
			operationContext = (operationContext ?? new OperationContext());
			return TableExtensionRetryPolicy.Execute(() => TableExtensionOperationHelper.ExecuteOperationAsync<TResult>(operation, client, table, requestOptions, operationContext, cancellationToken), cancellationToken, operationContext, requestOptions);
		}

		public Task<TResult> ExecuteTableBatchOperationAsync<TResult>(TableBatchOperation operation, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken) where TResult : class
		{
			operationContext = (operationContext ?? new OperationContext());
			return TableExtensionRetryPolicy.Execute(() => TableExtensionOperationHelper.ExecuteBatchOperationAsync<TResult>(operation, client, table, requestOptions, operationContext, cancellationToken), cancellationToken, operationContext, requestOptions);
		}

		public Task<ServiceProperties> GetServicePropertiesOperationAsync(CloudTableClient client, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			throw new NotSupportedException("The operation is not supported by Azure Cosmos Table endpoints. ");
		}

		public Task SetServicePropertiesOperationAsync(ServiceProperties properties, CloudTableClient client, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			throw new NotSupportedException("The operation is not supported by Azure Cosmos Table endpoints. ");
		}

		public Task<ServiceStats> GetServiceStatsAsync(CloudTableClient client, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			throw new NotSupportedException("The operation is not supported by Azure Cosmos Table endpoints. ");
		}

		public Task<TablePermissions> GetTablePermissionsAsync(CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			throw new NotSupportedException("The operation is not supported by Azure Cosmos Table endpoints. ");
		}

		public Task SetTablePermissionsAsync(TablePermissions permissions, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			throw new NotSupportedException("The operation is not supported by Azure Cosmos Table endpoints. ");
		}

		public TableQuerySegment<TResult> ExecuteQuerySegmented<TResult, TInput>(TableQuery<TInput> query, TableContinuationToken token, CloudTableClient client, CloudTable table, EntityResolver<TResult> resolver, TableRequestOptions requestOptions, OperationContext operationContext)
		{
			return TaskHelper.InlineIfPossible(() => ExecuteQuerySegmentedInternalAsync(query, token, client, table, resolver ?? new EntityResolver<TResult>(EntityUtilities.ResolveEntityByType<TResult>), requestOptions, operationContext, CancellationToken.None, query.Expression != null), null).GetAwaiter().GetResult();
		}

		public TableQuerySegment<TResult> ExecuteQuerySegmented<TResult>(TableQuery query, TableContinuationToken token, CloudTableClient client, CloudTable table, EntityResolver<TResult> resolver, TableRequestOptions requestOptions, OperationContext operationContext)
		{
			return TaskHelper.InlineIfPossible(() => ExecuteQuerySegmentedInternalAsync(query, token, client, table, resolver ?? new EntityResolver<TResult>(EntityUtilities.ResolveEntityByType<TResult>), requestOptions, operationContext, CancellationToken.None, isLinq: false), null).GetAwaiter().GetResult();
		}

		public Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<TResult, TInput>(TableQuery<TInput> query, TableContinuationToken token, CloudTableClient client, CloudTable table, EntityResolver<TResult> resolver, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			return ExecuteQuerySegmentedInternalAsync(query, token, client, table, resolver ?? new EntityResolver<TResult>(EntityUtilities.ResolveEntityByType<TResult>), requestOptions, operationContext, cancellationToken, query.Expression != null);
		}

		public Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedAsync<TResult>(TableQuery query, TableContinuationToken token, CloudTableClient client, CloudTable table, EntityResolver<TResult> resolver, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
		{
			return ExecuteQuerySegmentedInternalAsync(query, token, client, table, resolver ?? new EntityResolver<TResult>(EntityUtilities.ResolveEntityByType<TResult>), requestOptions, operationContext, cancellationToken, isLinq: false);
		}

		private Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedInternalAsync<TResult>(TableQuery query, TableContinuationToken token, CloudTableClient client, CloudTable table, EntityResolver<TResult> resolver, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken, bool isLinq)
		{
			operationContext = (operationContext ?? new OperationContext());
			return TableExtensionRetryPolicy.Execute(async delegate
			{
				try
				{
					return await TableExtensionQueryHelper.QueryDocumentsAsync(query.TakeCount, string.IsNullOrEmpty(query.FilterString) ? query.FilterString : ODataV3Translator.TranslateFilter(query.FilterString, useUtcTicks: false), query.SelectColumns, token, client, table, resolver, requestOptions, operationContext, isLinq, query.OrderByEntities, TombstoneFieldName);
				}
				catch (Exception exception)
				{
					StorageException tableResultFromException = EntityHelpers.GetTableResultFromException(exception);
					operationContext.RequestResults.Add(tableResultFromException.RequestInformation);
					throw tableResultFromException;
				}
			}, cancellationToken, operationContext, requestOptions);
		}

		private Task<TableQuerySegment<TResult>> ExecuteQuerySegmentedInternalAsync<TResult, TInput>(TableQuery<TInput> query, TableContinuationToken token, CloudTableClient client, CloudTable table, EntityResolver<TResult> resolver, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken, bool isLinq)
		{
			operationContext = (operationContext ?? new OperationContext());
			return TableExtensionRetryPolicy.Execute(async delegate
			{
				try
				{
					if (string.Equals(table.Name, "Tables", StringComparison.OrdinalIgnoreCase))
					{
						return await TableExtensionQueryHelper.QueryCollectionsAsync<TResult>(query.TakeCount, string.IsNullOrEmpty(query.FilterString) ? query.FilterString : ODataV3Translator.TranslateFilter(query.FilterString, useUtcTicks: false), token, client, table, requestOptions, operationContext);
					}
					return await TableExtensionQueryHelper.QueryDocumentsAsync(query.TakeCount, string.IsNullOrEmpty(query.FilterString) ? query.FilterString : ODataV3Translator.TranslateFilter(query.FilterString, useUtcTicks: false), query.SelectColumns, token, client, table, resolver, requestOptions, operationContext, isLinq, query.OrderByEntities, TombstoneFieldName);
				}
				catch (Exception exception)
				{
					StorageException tableResultFromException = EntityHelpers.GetTableResultFromException(exception);
					operationContext.RequestResults.Add(tableResultFromException.RequestInformation);
					throw tableResultFromException;
				}
			}, cancellationToken, operationContext, requestOptions);
		}
	}
}
