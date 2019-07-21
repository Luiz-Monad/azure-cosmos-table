namespace Microsoft.Azure.Cosmos.Tables.SharedFiles
{
	internal class TableErrorResult
	{
		public string ExtendedErrorCode
		{
			get;
			set;
		}

		public string ExtendedErroMessage
		{
			get;
			set;
		}

		public int HttpStatusCode
		{
			get;
			set;
		}

		public string HttpStatusMessage
		{
			get;
			set;
		}

		public string ServiceRequestID
		{
			get;
			set;
		}

		public double RequestCharge
		{
			get;
			set;
		}
	}
}
