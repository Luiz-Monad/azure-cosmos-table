using Microsoft.Azure.Cosmos.Table.RestExecutor.Common.Auth;
using System;
using System.Net.Http;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.Common
{
	internal sealed class StorageRequestMessage : HttpRequestMessage
	{
		public ICanonicalizer Canonicalizer
		{
			get;
			private set;
		}

		public StorageCredentials Credentials
		{
			get;
			private set;
		}

		public string AccountName
		{
			get;
			private set;
		}

		public StorageRequestMessage(HttpMethod method, Uri requestUri, ICanonicalizer canonicalizer, StorageCredentials credentials, string accountName)
			: base(method, requestUri)
		{
			Canonicalizer = canonicalizer;
			Credentials = credentials;
			AccountName = accountName;
		}
	}
}
