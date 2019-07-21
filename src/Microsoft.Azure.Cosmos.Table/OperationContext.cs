using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Cosmos.Table
{
	public sealed class OperationContext
	{
		private IList<RequestResult> requestResults = new List<RequestResult>();

		public IDictionary<string, string> UserHeaders
		{
			get;
			set;
		}

		public string ClientRequestID
		{
			get;
			set;
		}

		public string CustomUserAgent
		{
			get;
			set;
		}

		public DateTimeOffset StartTime
		{
			get;
			set;
		}

		public DateTimeOffset EndTime
		{
			get;
			set;
		}

		public IList<RequestResult> RequestResults => requestResults;

		public RequestResult LastResult
		{
			get
			{
				if (requestResults == null || requestResults.Count == 0)
				{
					return null;
				}
				return requestResults[requestResults.Count - 1];
			}
		}

		public static event EventHandler<RequestEventArgs> GlobalSendingRequest;

		public static event EventHandler<RequestEventArgs> GlobalResponseReceived;

		public static event EventHandler<RequestEventArgs> GlobalRequestCompleted;

		public static event EventHandler<RequestEventArgs> GlobalRetrying;

		public event EventHandler<RequestEventArgs> SendingRequest;

		public event EventHandler<RequestEventArgs> ResponseReceived;

		public event EventHandler<RequestEventArgs> RequestCompleted;

		public event EventHandler<RequestEventArgs> Retrying;

		public OperationContext()
		{
			ClientRequestID = Guid.NewGuid().ToString();
		}

		internal void FireSendingRequest(RequestEventArgs args)
		{
			this.SendingRequest?.Invoke(this, args);
			OperationContext.GlobalSendingRequest?.Invoke(this, args);
		}

		internal void FireResponseReceived(RequestEventArgs args)
		{
			this.ResponseReceived?.Invoke(this, args);
			OperationContext.GlobalResponseReceived?.Invoke(this, args);
		}

		internal void FireRequestCompleted(RequestEventArgs args)
		{
			this.RequestCompleted?.Invoke(this, args);
			OperationContext.GlobalRequestCompleted?.Invoke(this, args);
		}

		internal void FireRetrying(RequestEventArgs args)
		{
			this.Retrying?.Invoke(this, args);
			OperationContext.GlobalRetrying?.Invoke(this, args);
		}
	}
}
