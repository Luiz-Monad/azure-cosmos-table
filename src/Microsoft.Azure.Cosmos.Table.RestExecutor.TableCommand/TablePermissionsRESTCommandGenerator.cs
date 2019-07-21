using Microsoft.Azure.Cosmos.Table.RestExecutor.Common;
using Microsoft.Azure.Cosmos.Table.RestExecutor.Common.Auth;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.TableCommand
{
	internal static class TablePermissionsRESTCommandGenerator
	{
		internal static RESTCommand<TablePermissions> GetAclImpl(CloudTableClient client, CloudTable table, TableRequestOptions requestOptions)
		{
			RESTCommand<TablePermissions> rESTCommand = new RESTCommand<TablePermissions>(client.Credentials, client.StorageUri);
			RESTCommandGeneratorUtils.ApplyTableRequestOptionsToStorageCommand(requestOptions, rESTCommand);
			rESTCommand.HttpClient = client.HttpClient;
			rESTCommand.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
			rESTCommand.BuildRequest = ((RESTCommand<TablePermissions> cmd, Uri uri, UriQueryBuilder uriBuilder, HttpContent httpContent, int? timeout, OperationContext ctx) => TableRequestMessageFactory.BuildStorageRequestMessageForTablePermissions(NavigationHelper.AppendPathToSingleUri(uri, table.Name), uriBuilder, timeout, HttpMethod.Get, null, SharedKeyCanonicalizer.Instance, client.Credentials, ctx, requestOptions));
			rESTCommand.ParseErrorAsync = StorageExtendedErrorInformationRestHelper.ReadExtendedErrorInfoFromStreamAsync;
			rESTCommand.PreProcessResponse = ((RESTCommand<TablePermissions> cmd, HttpResponseMessage resp, Exception ex, OperationContext ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null, cmd, ex));
			rESTCommand.PostProcessResponseAsync = ((RESTCommand<TablePermissions> cmd, HttpResponseMessage resp, OperationContext ctx, CancellationToken token) => TableOperationHttpResponseParsers.ParseGetAclAsync(cmd, resp, ctx));
			return rESTCommand;
		}

		internal static RESTCommand<NullType> SetAclImpl(TablePermissions permissions, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions)
		{
			RESTCommand<NullType> rESTCommand = new RESTCommand<NullType>(client.Credentials, client.StorageUri);
			RESTCommandGeneratorUtils.ApplyTableRequestOptionsToStorageCommand(requestOptions, rESTCommand);
			rESTCommand.HttpClient = client.HttpClient;
			rESTCommand.BuildRequest = ((RESTCommand<NullType> cmd, Uri uri, UriQueryBuilder uriBuilder, HttpContent httpContent, int? timeout, OperationContext ctx) => TableRequestMessageFactory.BuildStorageRequestMessageForTablePermissions(NavigationHelper.AppendPathToSingleUri(uri, table.Name), uriBuilder, timeout, HttpMethod.Put, permissions, SharedKeyCanonicalizer.Instance, client.Credentials, ctx, requestOptions));
			rESTCommand.ParseErrorAsync = StorageExtendedErrorInformationRestHelper.ReadExtendedErrorInfoFromStreamAsync;
			rESTCommand.PreProcessResponse = ((RESTCommand<NullType> cmd, HttpResponseMessage resp, Exception ex, OperationContext ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.NoContent, resp, NullType.Value, cmd, ex));
			return rESTCommand;
		}
	}
}
