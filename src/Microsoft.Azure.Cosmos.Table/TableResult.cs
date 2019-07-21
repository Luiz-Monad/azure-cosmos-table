namespace Microsoft.Azure.Cosmos.Table
{
	public sealed class TableResult
	{
		public object Result
		{
			get;
			set;
		}

		public int HttpStatusCode
		{
			get;
			set;
		}

		public string Etag
		{
			get;
			set;
		}

		public string SessionToken
		{
			get;
			internal set;
		}

		public double? RequestCharge
		{
			get;
			internal set;
		}
	}
}
