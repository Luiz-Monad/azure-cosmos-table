using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.Common.Auth
{
	internal sealed class StorageAuthenticationHttpHandler : HttpClientHandler
	{
		private static Lazy<StorageAuthenticationHttpHandler> instance = new Lazy<StorageAuthenticationHttpHandler>(() => new StorageAuthenticationHttpHandler());

		public static StorageAuthenticationHttpHandler Instance => instance.Value;

		internal StorageAuthenticationHttpHandler()
		{
		}

		private Task<HttpResponseMessage> GetNoOpAuthenticationTask(StorageRequestMessage request, CancellationToken cancellationToken)
		{
			return base.SendAsync((HttpRequestMessage)request, cancellationToken);
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			StorageRequestMessage storageRequestMessage = request as StorageRequestMessage;
			return SelectAuthenticationTaskFactory(storageRequestMessage)(storageRequestMessage, cancellationToken);
		}

		private Func<StorageRequestMessage, CancellationToken, Task<HttpResponseMessage>> SelectAuthenticationTaskFactory(StorageRequestMessage request)
		{
			if (request.Credentials?.IsSharedKey ?? false)
			{
				return GetSharedKeyAuthenticationTask;
			}
			return GetNoOpAuthenticationTask;
		}

		private Task<HttpResponseMessage> GetSharedKeyAuthenticationTask(StorageRequestMessage request, CancellationToken cancellationToken)
		{
			ICanonicalizer canonicalizer = request.Canonicalizer;
			StorageCredentials credentials = request.Credentials;
			string accountName = request.AccountName;
			if (!request.Headers.Contains("x-ms-date"))
			{
				string value = HttpWebUtility.ConvertDateTimeToHttpString(DateTimeOffset.UtcNow);
				request.Headers.Add("x-ms-date", value);
			}
			if (credentials.IsSharedKey)
			{
				string key = credentials.Key;
				string message = canonicalizer.CanonicalizeHttpRequest(request, accountName);
				string arg = CryptoUtility.ComputeHmac256(key, message);
				request.Headers.Authorization = new AuthenticationHeaderValue(canonicalizer.AuthorizationScheme, string.Format(CultureInfo.InvariantCulture, "{0}:{1}", credentials.AccountName, arg));
			}
			return base.SendAsync((HttpRequestMessage)request, cancellationToken);
		}
	}
}
