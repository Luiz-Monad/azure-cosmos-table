using System;

namespace Microsoft.Azure.Cosmos.Table
{
	public sealed class RequestResult
	{
		private volatile Exception exception;

		public int HttpStatusCode
		{
			get;
			set;
		}

		public string HttpStatusMessage
		{
			get;
			internal set;
		}

		public string ServiceRequestID
		{
			get;
			internal set;
		}

		public string ContentMd5
		{
			get;
			internal set;
		}

		public string Etag
		{
			get;
			internal set;
		}

		public string RequestDate
		{
			get;
			internal set;
		}

		public StorageLocation TargetLocation
		{
			get;
			internal set;
		}

		public StorageExtendedErrorInformation ExtendedErrorInformation
		{
			get;
			internal set;
		}

		public string ErrorCode
		{
			get;
			internal set;
		}

		public bool IsRequestServerEncrypted
		{
			get;
			internal set;
		}

		public Exception Exception
		{
			get
			{
				return exception;
			}
			set
			{
				exception = value;
			}
		}

		public DateTimeOffset StartTime
		{
			get;
			internal set;
		}

		public DateTimeOffset EndTime
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
