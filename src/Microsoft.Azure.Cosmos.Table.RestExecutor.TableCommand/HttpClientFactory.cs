using Microsoft.Azure.Cosmos.Table.RestExecutor.Common.Auth;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.TableCommand
{
	internal static class HttpClientFactory
	{
		private static Lazy<HttpClient> instance = new Lazy<HttpClient>(() => BuildHttpClient(StorageAuthenticationHttpHandler.Instance));

		public static HttpClient Instance => instance.Value;

		private static HttpClient BuildHttpClient(HttpMessageHandler httpMessageHandler)
		{
			HttpClient httpClient = new HttpClient(httpMessageHandler, disposeHandler: false);
			httpClient.DefaultRequestHeaders.ExpectContinue = false;
			httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Azure-Cosmos-Table", "1.0.2"));
			httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(TableRestConstants.HeaderConstants.UserAgentComment));
			httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-ms-version", "2018-03-28");
			httpClient.Timeout = Timeout.InfiniteTimeSpan;
			return httpClient;
		}

		internal static HttpClient HttpClientFromDelegatingHandler(DelegatingHandler delegatingHandler)
		{
			if (delegatingHandler == null)
			{
				return null;
			}
			DelegatingHandler delegatingHandler2 = delegatingHandler;
			while (delegatingHandler2.InnerHandler != null)
			{
				HttpMessageHandler innerHandler = delegatingHandler2.InnerHandler;
				if (!(innerHandler is DelegatingHandler))
				{
					throw new ArgumentException("Innermost DelegatingHandler must have a null InnerHandler.");
				}
				delegatingHandler2 = (DelegatingHandler)innerHandler;
			}
			delegatingHandler2.InnerHandler = new StorageAuthenticationHttpHandler();
			return BuildHttpClient(delegatingHandler);
		}
	}
}
