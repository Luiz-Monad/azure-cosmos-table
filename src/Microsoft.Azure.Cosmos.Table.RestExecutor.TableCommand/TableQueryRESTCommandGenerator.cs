using Microsoft.Azure.Cosmos.Table.RestExecutor.Common;
using Microsoft.Azure.Cosmos.Table.RestExecutor.Common.Auth;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.TableCommand
{
	internal static class TableQueryRESTCommandGenerator
	{
		internal static RESTCommand<TableQuerySegment<DynamicTableEntity>> GenerateCMDForTableQuery(TableQuery query, TableContinuationToken token, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions)
		{
			UriQueryBuilder builder2 = RESTCommandGeneratorUtils.GenerateQueryBuilder(query, requestOptions.ProjectSystemProperties);
			if (token != null)
			{
				RESTCommandGeneratorUtils.ApplyToUriQueryBuilder(token, builder2);
			}
			StorageUri storageUri = NavigationHelper.AppendPathToUri(client.StorageUri, table.Name);
			RESTCommand<TableQuerySegment<DynamicTableEntity>> rESTCommand = new RESTCommand<TableQuerySegment<DynamicTableEntity>>(client.Credentials, storageUri);
			RESTCommandGeneratorUtils.ApplyTableRequestOptionsToStorageCommand(requestOptions, rESTCommand);
			rESTCommand.HttpClient = client.HttpClient;
			rESTCommand.CommandLocationMode = RESTCommandGeneratorUtils.GetListingLocationMode(token);
			rESTCommand.Builder = builder2;
			rESTCommand.ParseErrorAsync = StorageExtendedErrorInformationRestHelper.ReadExtendedErrorInfoFromStreamAsync;
			rESTCommand.BuildRequest = ((RESTCommand<TableQuerySegment<DynamicTableEntity>> cmd, Uri uri, UriQueryBuilder builder, HttpContent httpContent, int? timeout, OperationContext ctx) => TableRequestMessageFactory.BuildStorageRequestMessageForTableQuery(uri, builder, SharedKeyCanonicalizer.Instance, client.Credentials, ctx, requestOptions));
			rESTCommand.PreProcessResponse = ((RESTCommand<TableQuerySegment<DynamicTableEntity>> cmd, HttpResponseMessage resp, Exception ex, OperationContext ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp?.StatusCode ?? HttpStatusCode.Unused, null, cmd, ex));
			rESTCommand.PostProcessResponseAsync = async delegate(RESTCommand<TableQuerySegment<DynamicTableEntity>> cmd, HttpResponseMessage resp, OperationContext ctx, CancellationToken cancellationToken)
			{
				ResultSegment<DynamicTableEntity> resultSegment = await TableOperationHttpResponseParsers.TableQueryPostProcessGenericAsync<DynamicTableEntity, DynamicTableEntity>(cmd.ResponseStream, EntityUtilities.ResolveDynamicEntity, resp, requestOptions, ctx, cancellationToken);
				if (resultSegment.ContinuationToken != null)
				{
					resultSegment.ContinuationToken.TargetLocation = cmd.CurrentResult.TargetLocation;
				}
				return new TableQuerySegment<DynamicTableEntity>(resultSegment);
			};
			return rESTCommand;
		}

		internal static RESTCommand<TableQuerySegment<TResult>> GenerateCMDForTableQuery<TInput, TResult>(TableQuery<TInput> query, TableContinuationToken token, CloudTableClient client, CloudTable table, EntityResolver<TResult> resolver, TableRequestOptions requestOptions)
		{
			UriQueryBuilder builder2 = RESTCommandGeneratorUtils.GenerateQueryBuilder(query, requestOptions.ProjectSystemProperties);
			if (token != null)
			{
				RESTCommandGeneratorUtils.ApplyToUriQueryBuilder(token, builder2);
			}
			StorageUri storageUri = NavigationHelper.AppendPathToUri(client.StorageUri, table.Name);
			RESTCommand<TableQuerySegment<TResult>> rESTCommand = new RESTCommand<TableQuerySegment<TResult>>(client.Credentials, storageUri);
			RESTCommandGeneratorUtils.ApplyTableRequestOptionsToStorageCommand(requestOptions, rESTCommand);
			rESTCommand.HttpClient = client.HttpClient;
			rESTCommand.CommandLocationMode = RESTCommandGeneratorUtils.GetListingLocationMode(token);
			rESTCommand.Builder = builder2;
			rESTCommand.ParseErrorAsync = StorageExtendedErrorInformationRestHelper.ReadExtendedErrorInfoFromStreamAsync;
			rESTCommand.BuildRequest = ((RESTCommand<TableQuerySegment<TResult>> cmd, Uri uri, UriQueryBuilder builder, HttpContent httpContent, int? timeout, OperationContext ctx) => TableRequestMessageFactory.BuildStorageRequestMessageForTableQuery(uri, builder, SharedKeyCanonicalizer.Instance, client.Credentials, ctx, requestOptions));
			rESTCommand.PreProcessResponse = ((RESTCommand<TableQuerySegment<TResult>> cmd, HttpResponseMessage resp, Exception ex, OperationContext ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp?.StatusCode ?? HttpStatusCode.Unused, null, cmd, ex));
			rESTCommand.PostProcessResponseAsync = async delegate(RESTCommand<TableQuerySegment<TResult>> cmd, HttpResponseMessage resp, OperationContext ctx, CancellationToken cancellationToken)
			{
				ResultSegment<TResult> resultSegment = await TableOperationHttpResponseParsers.TableQueryPostProcessGenericAsync<TResult, TInput>(cmd.ResponseStream, resolver.Invoke, resp, requestOptions, ctx, cancellationToken);
				if (resultSegment.ContinuationToken != null)
				{
					resultSegment.ContinuationToken.TargetLocation = cmd.CurrentResult.TargetLocation;
				}
				return new TableQuerySegment<TResult>(resultSegment);
			};
			return rESTCommand;
		}

		internal static RESTCommand<TableQuerySegment<TResult>> GenerateCMDForTableQuery<TResult>(TableQuery query, TableContinuationToken token, CloudTableClient client, CloudTable table, EntityResolver<TResult> resolver, TableRequestOptions requestOptions)
		{
			UriQueryBuilder builder2 = RESTCommandGeneratorUtils.GenerateQueryBuilder(query, requestOptions.ProjectSystemProperties);
			if (token != null)
			{
				RESTCommandGeneratorUtils.ApplyToUriQueryBuilder(token, builder2);
			}
			StorageUri storageUri = NavigationHelper.AppendPathToUri(client.StorageUri, table.Name);
			RESTCommand<TableQuerySegment<TResult>> rESTCommand = new RESTCommand<TableQuerySegment<TResult>>(client.Credentials, storageUri);
			RESTCommandGeneratorUtils.ApplyTableRequestOptionsToStorageCommand(requestOptions, rESTCommand);
			rESTCommand.HttpClient = client.HttpClient;
			rESTCommand.CommandLocationMode = RESTCommandGeneratorUtils.GetListingLocationMode(token);
			rESTCommand.Builder = builder2;
			rESTCommand.ParseErrorAsync = StorageExtendedErrorInformationRestHelper.ReadExtendedErrorInfoFromStreamAsync;
			rESTCommand.BuildRequest = ((RESTCommand<TableQuerySegment<TResult>> cmd, Uri uri, UriQueryBuilder builder, HttpContent httpContent, int? timeout, OperationContext ctx) => TableRequestMessageFactory.BuildStorageRequestMessageForTableQuery(uri, builder, SharedKeyCanonicalizer.Instance, client.Credentials, ctx, requestOptions));
			rESTCommand.PreProcessResponse = ((RESTCommand<TableQuerySegment<TResult>> cmd, HttpResponseMessage resp, Exception ex, OperationContext ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp?.StatusCode ?? HttpStatusCode.Unused, null, cmd, ex));
			rESTCommand.PostProcessResponseAsync = async delegate(RESTCommand<TableQuerySegment<TResult>> cmd, HttpResponseMessage resp, OperationContext ctx, CancellationToken cancellationToken)
			{
				ResultSegment<TResult> resultSegment = await TableOperationHttpResponseParsers.TableQueryPostProcessGenericAsync<TResult, DynamicTableEntity>(cmd.ResponseStream, resolver.Invoke, resp, requestOptions, ctx, cancellationToken);
				if (resultSegment.ContinuationToken != null)
				{
					resultSegment.ContinuationToken.TargetLocation = cmd.CurrentResult.TargetLocation;
				}
				return new TableQuerySegment<TResult>(resultSegment);
			};
			return rESTCommand;
		}
	}
}
