using Microsoft.Azure.Cosmos.Table.RestExecutor.Common;
using Microsoft.Azure.Cosmos.Table.RestExecutor.Common.Auth;
using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.TableCommand
{
	internal static class TableOperationRESTCommandGenerator
	{
		internal static RESTCommand<TableResult> GenerateCMDForTableOperation(TableOperation operation, CloudTableClient client, CloudTable table, TableRequestOptions modifiedOptions)
		{
			if (operation.OperationType == TableOperationType.Insert || operation.OperationType == TableOperationType.InsertOrMerge || operation.OperationType == TableOperationType.InsertOrReplace)
			{
				if (!operation.IsTableEntity && operation.OperationType != 0)
				{
					CommonUtility.AssertNotNull("Upserts require a valid PartitionKey", operation.PartitionKey);
					CommonUtility.AssertNotNull("Upserts require a valid RowKey", operation.RowKey);
				}
				return InsertImpl(operation, client, table, modifiedOptions);
			}
			if (operation.OperationType == TableOperationType.Delete)
			{
				if (!operation.IsTableEntity)
				{
					CommonUtility.AssertNotNullOrEmpty("Delete requires a valid ETag", operation.ETag);
					CommonUtility.AssertNotNull("Delete requires a valid PartitionKey", operation.PartitionKey);
					CommonUtility.AssertNotNull("Delete requires a valid RowKey", operation.RowKey);
				}
				return DeleteImpl(operation, client, table, modifiedOptions);
			}
			if (operation.OperationType == TableOperationType.Merge)
			{
				CommonUtility.AssertNotNullOrEmpty("Merge requires a valid ETag", operation.ETag);
				CommonUtility.AssertNotNull("Merge requires a valid PartitionKey", operation.PartitionKey);
				CommonUtility.AssertNotNull("Merge requires a valid RowKey", operation.RowKey);
				return MergeImpl(operation, client, table, modifiedOptions);
			}
			if (operation.OperationType == TableOperationType.Replace)
			{
				CommonUtility.AssertNotNullOrEmpty("Replace requires a valid ETag", operation.ETag);
				CommonUtility.AssertNotNull("Replace requires a valid PartitionKey", operation.PartitionKey);
				CommonUtility.AssertNotNull("Replace requires a valid RowKey", operation.RowKey);
				return ReplaceImpl(operation, client, table, modifiedOptions);
			}
			if (operation.OperationType == TableOperationType.Retrieve)
			{
				return RetrieveImpl(operation, client, table, modifiedOptions);
			}
			throw new NotSupportedException();
		}

		private static RESTCommand<TableResult> InsertImpl(TableOperation operation, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions)
		{
			RESTCommand<TableResult> rESTCommand = new RESTCommand<TableResult>(client.Credentials, RESTCommandGeneratorUtils.GenerateRequestURI(operation, client.StorageUri, table.Name));
			RESTCommandGeneratorUtils.ApplyTableRequestOptionsToStorageCommand(requestOptions, rESTCommand);
			rESTCommand.HttpClient = client.HttpClient;
			TableResult result = new TableResult
			{
				Result = operation.Entity
			};
			rESTCommand.ParseErrorAsync = StorageExtendedErrorInformationRestHelper.ReadExtendedErrorInfoFromStreamAsync;
			rESTCommand.BuildRequest = ((RESTCommand<TableResult> cmd, Uri uri, UriQueryBuilder builder, HttpContent httpContent, int? timeout, OperationContext ctx) => TableRequestMessageFactory.BuildStorageRequestMessageForTableOperation(uri, operation, SharedKeyCanonicalizer.Instance, client.Credentials, ctx, requestOptions));
			rESTCommand.PreProcessResponse = ((RESTCommand<TableResult> cmd, HttpResponseMessage resp, Exception ex, OperationContext ctx) => TableOperationHttpResponseParsers.TableOperationPreProcess(result, operation, resp, ex));
			rESTCommand.PostProcessResponseAsync = ((RESTCommand<TableResult> cmd, HttpResponseMessage resp, OperationContext ctx, CancellationToken token) => TableOperationHttpResponseParsers.TableOperationPostProcessAsync(result, operation, cmd, resp, ctx, requestOptions, client.Credentials.AccountName, token));
			return rESTCommand;
		}

		private static RESTCommand<TableResult> DeleteImpl(TableOperation operation, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions)
		{
			RESTCommand<TableResult> rESTCommand = new RESTCommand<TableResult>(client.Credentials, RESTCommandGeneratorUtils.GenerateRequestURI(operation, client.StorageUri, table.Name));
			RESTCommandGeneratorUtils.ApplyTableRequestOptionsToStorageCommand(requestOptions, rESTCommand);
			rESTCommand.HttpClient = client.HttpClient;
			TableResult result = new TableResult
			{
				Result = operation.Entity
			};
			rESTCommand.ParseErrorAsync = StorageExtendedErrorInformationRestHelper.ReadExtendedErrorInfoFromStreamAsync;
			rESTCommand.BuildRequest = ((RESTCommand<TableResult> cmd, Uri uri, UriQueryBuilder builder, HttpContent httpContent, int? timeout, OperationContext ctx) => TableRequestMessageFactory.BuildStorageRequestMessageForTableOperation(uri, operation, SharedKeyCanonicalizer.Instance, client.Credentials, ctx, requestOptions));
			rESTCommand.PreProcessResponse = ((RESTCommand<TableResult> cmd, HttpResponseMessage resp, Exception ex, OperationContext ctx) => TableOperationHttpResponseParsers.TableOperationPreProcess(result, operation, resp, ex));
			return rESTCommand;
		}

		private static RESTCommand<TableResult> MergeImpl(TableOperation operation, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions)
		{
			RESTCommand<TableResult> rESTCommand = new RESTCommand<TableResult>(client.Credentials, RESTCommandGeneratorUtils.GenerateRequestURI(operation, client.StorageUri, table.Name));
			RESTCommandGeneratorUtils.ApplyTableRequestOptionsToStorageCommand(requestOptions, rESTCommand);
			rESTCommand.HttpClient = client.HttpClient;
			TableResult result = new TableResult
			{
				Result = operation.Entity
			};
			rESTCommand.ParseErrorAsync = StorageExtendedErrorInformationRestHelper.ReadExtendedErrorInfoFromStreamAsync;
			rESTCommand.BuildRequest = ((RESTCommand<TableResult> cmd, Uri uri, UriQueryBuilder builder, HttpContent httpContent, int? timeout, OperationContext ctx) => TableRequestMessageFactory.BuildStorageRequestMessageForTableOperation(uri, operation, SharedKeyCanonicalizer.Instance, client.Credentials, ctx, requestOptions));
			rESTCommand.PreProcessResponse = ((RESTCommand<TableResult> cmd, HttpResponseMessage resp, Exception ex, OperationContext ctx) => TableOperationHttpResponseParsers.TableOperationPreProcess(result, operation, resp, ex));
			return rESTCommand;
		}

		private static RESTCommand<TableResult> ReplaceImpl(TableOperation operation, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions)
		{
			RESTCommand<TableResult> rESTCommand = new RESTCommand<TableResult>(client.Credentials, RESTCommandGeneratorUtils.GenerateRequestURI(operation, client.StorageUri, table.Name));
			RESTCommandGeneratorUtils.ApplyTableRequestOptionsToStorageCommand(requestOptions, rESTCommand);
			rESTCommand.HttpClient = client.HttpClient;
			TableResult result = new TableResult
			{
				Result = operation.Entity
			};
			rESTCommand.ParseErrorAsync = StorageExtendedErrorInformationRestHelper.ReadExtendedErrorInfoFromStreamAsync;
			rESTCommand.BuildRequest = ((RESTCommand<TableResult> cmd, Uri uri, UriQueryBuilder builder, HttpContent httpContent, int? timeout, OperationContext ctx) => TableRequestMessageFactory.BuildStorageRequestMessageForTableOperation(uri, operation, SharedKeyCanonicalizer.Instance, client.Credentials, ctx, requestOptions));
			rESTCommand.PreProcessResponse = ((RESTCommand<TableResult> cmd, HttpResponseMessage resp, Exception ex, OperationContext ctx) => TableOperationHttpResponseParsers.TableOperationPreProcess(result, operation, resp, ex));
			return rESTCommand;
		}

		private static RESTCommand<TableResult> RetrieveImpl(TableOperation operation, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions)
		{
			RESTCommand<TableResult> rESTCommand = new RESTCommand<TableResult>(client.Credentials, RESTCommandGeneratorUtils.GenerateRequestURI(operation, client.StorageUri, table.Name));
			RESTCommandGeneratorUtils.ApplyTableRequestOptionsToStorageCommand(requestOptions, rESTCommand);
			rESTCommand.HttpClient = client.HttpClient;
			TableResult result = new TableResult();
			if (operation.SelectColumns != null && operation.SelectColumns.Count > 0)
			{
				rESTCommand.Builder = RESTCommandGeneratorUtils.GenerateQueryBuilder(operation, requestOptions.ProjectSystemProperties);
			}
			rESTCommand.ParseErrorAsync = StorageExtendedErrorInformationRestHelper.ReadExtendedErrorInfoFromStreamAsync;
			rESTCommand.BuildRequest = ((RESTCommand<TableResult> cmd, Uri uri, UriQueryBuilder builder, HttpContent httpContent, int? timeout, OperationContext ctx) => TableRequestMessageFactory.BuildStorageRequestMessageForTableOperation((builder != null) ? builder.AddToUri(uri) : uri, operation, SharedKeyCanonicalizer.Instance, client.Credentials, ctx, requestOptions));
			rESTCommand.PreProcessResponse = ((RESTCommand<TableResult> cmd, HttpResponseMessage resp, Exception ex, OperationContext ctx) => TableOperationHttpResponseParsers.TableOperationPreProcess(result, operation, resp, ex));
			rESTCommand.PostProcessResponseAsync = delegate(RESTCommand<TableResult> cmd, HttpResponseMessage resp, OperationContext ctx, CancellationToken token)
			{
				if (resp.StatusCode == HttpStatusCode.NotFound)
				{
					return Task.FromResult(result);
				}
				return TableOperationHttpResponseParsers.TableOperationPostProcessAsync(result, operation, cmd, resp, ctx, requestOptions, client.Credentials.AccountName, token);
			};
			return rESTCommand;
		}

		internal static StorageUri GenerateRequestURI(TableOperation operation, StorageUri uriList, string tableName)
		{
			return new StorageUri(GenerateRequestURI(operation, uriList.PrimaryUri, tableName), GenerateRequestURI(operation, uriList.SecondaryUri, tableName));
		}

		internal static Uri GenerateRequestURI(TableOperation operation, Uri uri, string tableName)
		{
			if (uri == null)
			{
				return null;
			}
			if (operation.OperationType == TableOperationType.Insert)
			{
				return NavigationHelper.AppendPathToSingleUri(uri, tableName + "()");
			}
			return NavigationHelper.AppendPathToSingleUri(uri, string.Format(arg1: (!operation.IsTableEntity) ? string.Format(CultureInfo.InvariantCulture, "{0}='{1}',{2}='{3}'", "PartitionKey", operation.PartitionKey.Replace("'", "''"), "RowKey", operation.RowKey.Replace("'", "''")) : string.Format(CultureInfo.InvariantCulture, "'{0}'", operation.Entity.WriteEntity(null)["TableName"].StringValue), provider: CultureInfo.InvariantCulture, format: "{0}({1})", arg0: tableName));
		}

		private static CommandLocationMode GetListingLocationMode(TableContinuationToken token)
		{
			if (token != null && token.TargetLocation.HasValue)
			{
				switch (token.TargetLocation.Value)
				{
				case StorageLocation.Primary:
					return CommandLocationMode.PrimaryOnly;
				case StorageLocation.Secondary:
					return CommandLocationMode.SecondaryOnly;
				}
				CommonUtility.ArgumentOutOfRange("TargetLocation", token.TargetLocation.Value);
			}
			return CommandLocationMode.PrimaryOrSecondary;
		}
	}
}
