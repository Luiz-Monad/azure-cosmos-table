using System;
using System.Net.Http;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.Common.Auth
{
	internal sealed class SharedKeyCanonicalizer : ICanonicalizer
	{
		private const string SharedKeyAuthorizationScheme = "SharedKey";

		private static SharedKeyCanonicalizer instance = new SharedKeyCanonicalizer();

		public static SharedKeyCanonicalizer Instance => instance;

		public string AuthorizationScheme => "SharedKey";

		private SharedKeyCanonicalizer()
		{
		}

		public string CanonicalizeHttpRequest(HttpRequestMessage request, string accountName)
		{
			CommonUtility.AssertNotNull("request", request);
			CanonicalizedString canonicalizedString = new CanonicalizedString(request.Method.Method);
			if (request.Content != null && request.Content.Headers.ContentMD5 != null)
			{
				canonicalizedString.AppendCanonicalizedElement(Convert.ToBase64String(request.Content.Headers.ContentMD5));
			}
			else
			{
				canonicalizedString.AppendCanonicalizedElement(null);
			}
			if (request.Content != null && request.Content.Headers.ContentType != null)
			{
				canonicalizedString.AppendCanonicalizedElement(request.Content.Headers.ContentType.ToString());
			}
			else
			{
				canonicalizedString.AppendCanonicalizedElement(null);
			}
			AuthenticationUtility.AppendCanonicalizedDateHeader(canonicalizedString, request, allowMicrosoftDateHeader: true);
			string canonicalizedResourceString = AuthenticationUtility.GetCanonicalizedResourceString(request.RequestUri, accountName);
			canonicalizedString.AppendCanonicalizedElement(canonicalizedResourceString);
			return canonicalizedString.ToString();
		}
	}
}
