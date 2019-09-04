using Microsoft.Azure.Cosmos.Table.RestExecutor.Common.Auth;
using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.TableCommand
{
	internal static class HttpClientFactory
	{
		private static Lazy<HttpClient> instance = new Lazy<HttpClient>(() => BuildHttpClient(StorageAuthenticationHttpHandler.Instance, null));

		public static HttpClient Instance => instance.Value;

		private static HttpClient BuildHttpClient(HttpMessageHandler httpMessageHandler, TimeSpan? httpClientTimeout)
		{
			HttpClient httpClient = new HttpClient(httpMessageHandler, disposeHandler: false);
			httpClient.DefaultRequestHeaders.ExpectContinue = false;
			httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Azure-Cosmos-Table", "1.0.4"));
			httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(TableRestConstants.HeaderConstants.UserAgentComment));
			httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-ms-version", "2018-03-28");
			httpClient.Timeout = (httpClientTimeout ?? TableRestConstants.DefaultHttpClientTimeout);
			return httpClient;
		}

		internal static HttpClient HttpClientFromConfiguration(RestExecutorConfiguration configuration)
		{
			if (configuration == null)
			{
				return null;
			}
			if (configuration.DelegatingHandler == null)
			{
				return BuildHttpClient(StorageAuthenticationHttpHandler.Instance, configuration.HttpClientTimeout);
			}
			DelegatingHandler delegatingHandler = configuration.DelegatingHandler;
			DelegatingHandler delegatingHandler2 = delegatingHandler;
			while (delegatingHandler2.InnerHandler != null)
			{
				DelegatingHandler obj = delegatingHandler2.InnerHandler as DelegatingHandler;
				if (obj == null)
				{
					throw new ArgumentException("Innermost DelegatingHandler must have a null InnerHandler.");
				}
				delegatingHandler2 = obj;
			}
			delegatingHandler2.InnerHandler = new StorageAuthenticationHttpHandler();
			return BuildHttpClient(delegatingHandler, configuration.HttpClientTimeout);
		}
	}
}
