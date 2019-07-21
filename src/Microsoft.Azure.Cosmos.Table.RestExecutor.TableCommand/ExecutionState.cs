using Microsoft.Azure.Cosmos.Table.RestExecutor.Common;
using Microsoft.Azure.Cosmos.Table.RestExecutor.Utils;
using System;
using System.Globalization;
using System.IO;
using System.Net.Http;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.TableCommand
{
	internal class ExecutionState<T> : IDisposable
	{
		private Stream reqStream;

		private volatile Exception exceptionRef;

		private object timeoutLockerObj = new object();

		private bool reqTimedOut;

		private HttpResponseMessage resp;

		private object cancellationLockerObject = new object();

		internal OperationContext OperationContext
		{
			get;
			private set;
		}

		internal DateTime? OperationExpiryTime => Cmd.OperationExpiryTime;

		internal IRetryPolicy RetryPolicy
		{
			get;
			private set;
		}

		internal RESTCommand<T> Cmd
		{
			get;
			private set;
		}

		internal StorageLocation CurrentLocation
		{
			get;
			set;
		}

		internal RESTCommand<T> RestCMD => Cmd;

		internal ExecutorOperation CurrentOperation
		{
			get;
			set;
		}

		internal TimeSpan RemainingTimeout
		{
			get
			{
				if (!OperationExpiryTime.HasValue || OperationExpiryTime.Value.Equals(DateTime.MaxValue))
				{
					return TableRestConstants.DefaultClientSideTimeout;
				}
				TimeSpan timeSpan = OperationExpiryTime.Value - DateTime.Now;
				if (timeSpan <= TimeSpan.Zero)
				{
					throw StorageExceptionTranslator.GenerateTimeoutException(Cmd.CurrentResult, null);
				}
				return timeSpan;
			}
		}

		internal int RetryCount
		{
			get;
			set;
		}

		internal Stream ReqStream
		{
			get
			{
				return reqStream;
			}
			set
			{
				reqStream = value;
			}
		}

		internal Exception ExceptionRef
		{
			get
			{
				return exceptionRef;
			}
			set
			{
				exceptionRef = value;
				if (Cmd != null && Cmd.CurrentResult != null)
				{
					Cmd.CurrentResult.Exception = value;
				}
			}
		}

		internal T Result
		{
			get;
			set;
		}

		internal bool ReqTimedOut
		{
			get
			{
				lock (timeoutLockerObj)
				{
					return reqTimedOut;
				}
			}
			set
			{
				lock (timeoutLockerObj)
				{
					reqTimedOut = value;
				}
			}
		}

		internal StorageRequestMessage Req
		{
			get;
			set;
		}

		internal HttpResponseMessage Resp
		{
			get
			{
				return resp;
			}
			set
			{
				resp = value;
				if (value != null)
				{
					Cmd.CurrentResult.ServiceRequestID = resp.Headers.GetHeaderSingleValueOrDefault("x-ms-request-id");
					Cmd.CurrentResult.ContentMd5 = ((resp.Content.Headers.ContentMD5 != null) ? Convert.ToBase64String(resp.Content.Headers.ContentMD5) : null);
					Cmd.CurrentResult.Etag = ((resp.Headers.ETag != null) ? resp.Headers.ETag.ToString() : null);
					Cmd.CurrentResult.RequestDate = (resp.Headers.Date.HasValue ? resp.Headers.Date.Value.UtcDateTime.ToString("R", CultureInfo.InvariantCulture) : null);
					Cmd.CurrentResult.HttpStatusMessage = resp.ReasonPhrase;
					Cmd.CurrentResult.HttpStatusCode = (int)resp.StatusCode;
				}
			}
		}

		internal object CancellationLockerObject
		{
			get
			{
				return cancellationLockerObject;
			}
			set
			{
				cancellationLockerObject = value;
			}
		}

		internal Action CancelDelegate
		{
			get;
			set;
		}

		public ExecutionState(RESTCommand<T> cmd, IRetryPolicy policy, OperationContext operationContext)
		{
			Cmd = cmd;
			object retryPolicy2;
			if (policy == null)
			{
				IRetryPolicy retryPolicy = new NoRetry();
				retryPolicy2 = retryPolicy;
			}
			else
			{
				retryPolicy2 = policy.CreateInstance();
			}
			RetryPolicy = (IRetryPolicy)retryPolicy2;
			OperationContext = (operationContext ?? new OperationContext());
			InitializeLocation();
			if (OperationContext.StartTime == DateTimeOffset.MinValue)
			{
				OperationContext.StartTime = DateTimeOffset.Now;
			}
		}

		internal void Init()
		{
			Req = null;
			resp = null;
		}

		public void Dispose()
		{
		}

		private void InitializeLocation()
		{
			RESTCommand<T> restCMD = RestCMD;
			if (restCMD != null)
			{
				switch (restCMD.LocationMode)
				{
				case LocationMode.PrimaryOnly:
				case LocationMode.PrimaryThenSecondary:
					CurrentLocation = StorageLocation.Primary;
					break;
				case LocationMode.SecondaryOnly:
				case LocationMode.SecondaryThenPrimary:
					CurrentLocation = StorageLocation.Secondary;
					break;
				default:
					CommonUtility.ArgumentOutOfRange("LocationMode", restCMD.LocationMode);
					break;
				}
				Logger.LogInformational(OperationContext, "Starting operation with location {0} per location mode {1}.", CurrentLocation, restCMD.LocationMode);
			}
			else
			{
				CurrentLocation = StorageLocation.Primary;
			}
		}
	}
}
