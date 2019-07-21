using Microsoft.Azure.Cosmos.Table.RestExecutor.Common;
using Microsoft.Azure.Cosmos.Table.RestExecutor.Common.Auth;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.TableCommand
{
	internal static class TableServiceStatsRESTCommandGenerator
	{
		internal static RESTCommand<ServiceStats> GenerateCMDForGetServiceStats(CloudTableClient client, TableRequestOptions requestOptions)
		{
			RESTCommand<ServiceStats> rESTCommand = new RESTCommand<ServiceStats>(client.Credentials, client.StorageUri);
			RESTCommandGeneratorUtils.ApplyTableRequestOptionsToStorageCommand(requestOptions, rESTCommand);
			rESTCommand.HttpClient = client.HttpClient;
			LocationMode? locationMode = requestOptions.LocationMode;
			if (locationMode.GetValueOrDefault() == LocationMode.PrimaryOnly && (locationMode.HasValue ? true : false))
			{
				throw new InvalidOperationException("GetServiceStats cannot be run with a 'PrimaryOnly' location mode.");
			}
			rESTCommand.CommandLocationMode = CommandLocationMode.PrimaryOrSecondary;
			rESTCommand.BuildRequest = ((RESTCommand<ServiceStats> cmd, Uri uri, UriQueryBuilder uriBuilder, HttpContent httpContent, int? timeout, OperationContext ctx) => TableRequestMessageFactory.BuildStorageRequestMessageForGetServiceStats(uri, uriBuilder, timeout, SharedKeyCanonicalizer.Instance, client.Credentials, ctx, requestOptions));
			rESTCommand.ParseErrorAsync = StorageExtendedErrorInformationRestHelper.ReadExtendedErrorInfoFromStreamAsync;
			rESTCommand.PreProcessResponse = ((RESTCommand<ServiceStats> cmd, HttpResponseMessage resp, Exception ex, OperationContext ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp, null, cmd, ex));
			rESTCommand.PostProcessResponseAsync = ((RESTCommand<ServiceStats> cmd, HttpResponseMessage resp, OperationContext ctx, CancellationToken token) => TableOperationHttpResponseParsers.ReadServiceStatsAsync(cmd.ResponseStream));
			return rESTCommand;
		}
	}
}
