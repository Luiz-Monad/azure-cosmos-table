using System.Net.Http;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.Common.Auth
{
	internal interface ICanonicalizer
	{
		string AuthorizationScheme
		{
			get;
		}

		string CanonicalizeHttpRequest(HttpRequestMessage request, string accountName);
	}
}
