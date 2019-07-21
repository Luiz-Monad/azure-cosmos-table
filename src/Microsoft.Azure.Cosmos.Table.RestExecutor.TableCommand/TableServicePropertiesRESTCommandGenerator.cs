using Microsoft.Azure.Cosmos.Table.RestExecutor.Common;
using Microsoft.Azure.Cosmos.Table.RestExecutor.Common.Auth;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.TableCommand
{
	internal static class TableServicePropertiesRESTCommandGenerator
	{
		internal static RESTCommand<ServiceProperties> GetServicePropertiesImpl(CloudTableClient client, TableRequestOptions requestOptions)
		{
			RESTCommand<ServiceProperties> rESTCommand = new RESTCommand<ServiceProperties>(client.Credentials, client.StorageUri);
			RESTCommandGeneratorUtils.ApplyTableRequestOptionsToStorageCommand(requestOptions, rESTCommand);
			rESTCommand.HttpClient = client.HttpClient;
			rESTCommand.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
			rESTCommand.BuildRequest = ((RESTCommand<ServiceProperties> cmd, Uri uri, UriQueryBuilder uriBuilder, HttpContent httpContent, int? timeout, OperationContext ctx) => TableRequestMessageFactory.BuildStorageRequestMessageForTableServiceProperties(uri, uriBuilder, timeout, HttpMethod.Get, null, SharedKeyCanonicalizer.Instance, client.Credentials, ctx, requestOptions));
			rESTCommand.ParseErrorAsync = StorageExtendedErrorInformationRestHelper.ReadExtendedErrorInfoFromStreamAsync;
			rESTCommand.PreProcessResponse = ((RESTCommand<ServiceProperties> cmd, HttpResponseMessage resp, Exception ex, OperationContext ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null, cmd, ex));
			rESTCommand.PostProcessResponseAsync = ((RESTCommand<ServiceProperties> cmd, HttpResponseMessage resp, OperationContext ctx, CancellationToken token) => TableOperationHttpResponseParsers.ReadServicePropertiesAsync(cmd.ResponseStream));
			return rESTCommand;
		}

		internal static RESTCommand<NullType> SetServicePropertiesImpl(ServiceProperties properties, CloudTableClient client, TableRequestOptions requestOptions)
		{
			RESTCommand<NullType> rESTCommand = new RESTCommand<NullType>(client.Credentials, client.StorageUri);
			RESTCommandGeneratorUtils.ApplyTableRequestOptionsToStorageCommand(requestOptions, rESTCommand);
			rESTCommand.HttpClient = client.HttpClient;
			rESTCommand.BuildRequest = ((RESTCommand<NullType> cmd, Uri uri, UriQueryBuilder uriBuilder, HttpContent httpContent, int? timeout, OperationContext ctx) => TableRequestMessageFactory.BuildStorageRequestMessageForTableServiceProperties(uri, uriBuilder, timeout, HttpMethod.Put, properties, SharedKeyCanonicalizer.Instance, client.Credentials, ctx, requestOptions));
			rESTCommand.ParseErrorAsync = StorageExtendedErrorInformationRestHelper.ReadExtendedErrorInfoFromStreamAsync;
			rESTCommand.PreProcessResponse = ((RESTCommand<NullType> cmd, HttpResponseMessage resp, Exception ex, OperationContext ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Accepted, resp, NullType.Value, cmd, ex));
			return rESTCommand;
		}
	}
}
