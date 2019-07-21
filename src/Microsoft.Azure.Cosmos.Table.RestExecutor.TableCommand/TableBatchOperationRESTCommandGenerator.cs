using Microsoft.Azure.Cosmos.Table.RestExecutor.Common;
using Microsoft.Azure.Cosmos.Table.RestExecutor.Common.Auth;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.TableCommand
{
	internal static class TableBatchOperationRESTCommandGenerator
	{
		internal static RESTCommand<TableBatchResult> GenerateCMDForTableBatchOperation(TableBatchOperation batch, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions)
		{
			RESTCommand<TableBatchResult> rESTCommand = new RESTCommand<TableBatchResult>(client.Credentials, client.StorageUri);
			RESTCommandGeneratorUtils.ApplyTableRequestOptionsToStorageCommand(requestOptions, rESTCommand);
			rESTCommand.HttpClient = client.HttpClient;
			TableBatchResult results = new TableBatchResult();
			rESTCommand.CommandLocationMode = ((!batch.ContainsWrites) ? CommandLocationMode.PrimaryOrSecondary : CommandLocationMode.PrimaryOnly);
			rESTCommand.CommandLocationMode = CommandLocationMode.PrimaryOnly;
			rESTCommand.ParseErrorAsync = StorageExtendedErrorInformationRestHelper.ReadExtendedErrorInfoFromStreamAsync;
			rESTCommand.BuildRequest = ((RESTCommand<TableBatchResult> cmd, Uri uri, UriQueryBuilder builder, HttpContent httpContent, int? timeout, OperationContext ctx) => TableRequestMessageFactory.BuildStorageRequestMessageForTableBatchOperation(uri, batch, SharedKeyCanonicalizer.Instance, table.Name, client.Credentials, ctx, requestOptions));
			rESTCommand.PreProcessResponse = ((RESTCommand<TableBatchResult> cmd, HttpResponseMessage resp, Exception ex, OperationContext ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Accepted, resp?.StatusCode ?? HttpStatusCode.Unused, results, cmd, ex));
			rESTCommand.PostProcessResponseAsync = ((RESTCommand<TableBatchResult> cmd, HttpResponseMessage resp, OperationContext ctx, CancellationToken token) => TableOperationHttpResponseParsers.TableBatchOperationPostProcessAsync(results, batch, cmd, resp, ctx, requestOptions, client.Credentials.AccountName, token));
			rESTCommand.RecoveryAction = delegate
			{
				results.Clear();
			};
			return rESTCommand;
		}
	}
}
