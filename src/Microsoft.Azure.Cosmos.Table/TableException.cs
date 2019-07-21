using System;
using System.Net;

namespace Microsoft.Azure.Cosmos.Table
{
	internal class TableException : Exception
	{
		public HttpStatusCode HttpStatusCode
		{
			get;
			private set;
		}

		public string HttpStatusMessage
		{
			get;
			private set;
		}

		public string ErrorCode
		{
			get;
			private set;
		}

		public string ErrorMessage
		{
			get;
			private set;
		}

		internal TableException(HttpStatusCode httpStatusCode, string httpStatusMessage, string errorCode, string errorMessage)
		{
			HttpStatusCode = httpStatusCode;
			HttpStatusMessage = httpStatusMessage;
			ErrorCode = errorCode;
			ErrorMessage = errorMessage;
		}
	}
}
