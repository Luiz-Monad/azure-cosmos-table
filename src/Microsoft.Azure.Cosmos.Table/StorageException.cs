using System;
using System.Text;

namespace Microsoft.Azure.Cosmos.Table
{
	public class StorageException : Exception
	{
		public RequestResult RequestInformation
		{
			get;
			private set;
		}

		internal bool IsRetryable
		{
			get;
			set;
		}

		public StorageException()
			: this(null, null, null)
		{
		}

		public StorageException(string message)
			: this(null, message, null)
		{
		}

		public StorageException(string message, Exception innerException)
			: this(null, message, innerException)
		{
		}

		public StorageException(RequestResult res, string message, Exception inner)
			: base(message, inner)
		{
			RequestInformation = res;
			IsRetryable = true;
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(base.ToString());
			if (RequestInformation != null)
			{
				stringBuilder.AppendLine("Request Information");
				stringBuilder.AppendLine("RequestID:" + RequestInformation.ServiceRequestID);
				if (RequestInformation.RequestCharge.HasValue)
				{
					stringBuilder.AppendLine("RequestCharge:" + RequestInformation.RequestCharge.Value);
				}
				stringBuilder.AppendLine("RequestDate:" + RequestInformation.RequestDate);
				stringBuilder.AppendLine("StatusMessage:" + RequestInformation.HttpStatusMessage);
				stringBuilder.AppendLine("ErrorCode:" + RequestInformation.ErrorCode);
				if (RequestInformation.ExtendedErrorInformation != null)
				{
					stringBuilder.AppendLine("ErrorMessage:" + RequestInformation.ExtendedErrorInformation.ErrorMessage);
				}
			}
			return stringBuilder.ToString();
		}
	}
}
