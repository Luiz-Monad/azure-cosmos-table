using System;
using System.Net.Http;

namespace Microsoft.Azure.Cosmos.Table
{
	public sealed class RequestEventArgs : EventArgs
	{
		public RequestResult RequestInformation
		{
			get;
			internal set;
		}

		public HttpRequestMessage Request
		{
			get;
			internal set;
		}

		public HttpResponseMessage Response
		{
			get;
			internal set;
		}

		public RequestEventArgs(RequestResult res)
		{
			RequestInformation = res;
		}
	}
}
