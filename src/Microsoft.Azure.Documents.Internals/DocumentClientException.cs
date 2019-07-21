using Microsoft.Azure.Documents.Collections;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Microsoft.Azure.Documents.Internals
{
	/// <summary>
	/// The base class for client exceptions in the Azure Cosmos DB service.
	/// </summary>
	[Serializable]
	public class DocumentClientExceptionInternal : Exception
	{
		private Error error;

		private uint? substatus;

		private NameValueCollection responseHeaders;

		/// <summary>
		/// Gets the error code associated with the exception in the Azure Cosmos DB service.
		/// </summary>
		public Error Error
		{
			get
			{
				if (error == null)
				{
					error = new Error
					{
						Code = StatusCode.ToString(),
						Message = Message
					};
				}
				return error;
			}
		}

		/// <summary>
		/// Gets the activity ID associated with the request from the Azure Cosmos DB service.
		/// </summary>
		public string ActivityId
		{
			get
			{
				if (responseHeaders != null)
				{
					return responseHeaders["x-ms-activity-id"];
				}
				return null;
			}
		}

		/// <summary>
		/// Gets the recommended time interval after which the client can retry failed requests from the Azure Cosmos DB service
		/// </summary>
		public TimeSpan RetryAfter
		{
			get
			{
				if (responseHeaders != null)
				{
					string text = responseHeaders["x-ms-retry-after-ms"];
					if (!string.IsNullOrEmpty(text))
					{
						long result = 0L;
						if (long.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out result))
						{
							return TimeSpan.FromMilliseconds(result);
						}
					}
				}
				return TimeSpan.Zero;
			}
		}

		/// <summary>
		/// Gets the headers associated with the response from the Azure Cosmos DB service.
		/// </summary>
		public NameValueCollection ResponseHeaders => responseHeaders;

		internal NameValueCollection Headers
		{
			get
			{
				return responseHeaders;
			}
			set
			{
				responseHeaders = value;
			}
		}

		/// <summary>
		/// Gets or sets the request status code in the Azure Cosmos DB service.
		/// </summary>
		public HttpStatusCode? StatusCode
		{
			get;
			internal set;
		}

		/// <summary>
		/// Gets the textual description of request completion status.
		/// </summary>
		internal string StatusDescription
		{
			get;
			set;
		}

		/// <summary>
		/// Cost of the request in the Azure Cosmos DB service.
		/// </summary>
		public double RequestCharge
		{
			get
			{
				if (responseHeaders != null)
				{
					return GetHeaderValueDouble(responseHeaders, "x-ms-request-charge", 0.0);
				}
				return 0.0;
			}
		}

		private const string ExceptionMessageAddRequestUri = "{0}, Request URI: {1}, RequestStats: {2}, SDK: {3}";

		/// <summary>
		/// Gets the console.log output from server side scripts statements when script logging is enabled.
		/// </summary>
		public string ScriptLog => null; //Helpers.GetScriptLogHeader(Headers);

		/// <summary>
		/// Gets a message that describes the current exception from the Azure Cosmos DB service.
		/// </summary>
		public override string Message
		{
			get
			{
				string text = (RequestStatistics == null) ? string.Empty : RequestStatistics.ToString();
				if (RequestUri != null)
				{
					return string.Format(CultureInfo.CurrentUICulture, ExceptionMessageAddRequestUri, 
						base.Message, RequestUri.PathAndQuery, text, GenerateBaseUserAgentString());
				}
				if (string.IsNullOrEmpty(text))
				{
					return string.Format(CultureInfo.CurrentCulture, "{0}, {1}", base.Message, GenerateBaseUserAgentString());
				}
				return string.Format(CultureInfo.CurrentUICulture, "{0}, {1}, {2}", base.Message, text, GenerateBaseUserAgentString());
			}
		}

		internal virtual string PublicMessage
		{
			get
			{
				string text = (RequestStatistics == null) ? string.Empty : RequestStatistics.ToString();
				if (RequestUri != null)
				{
					return string.Format(CultureInfo.CurrentUICulture, ExceptionMessageAddRequestUri, 
						base.Message, RequestUri.PathAndQuery, text, GenerateBaseUserAgentString());
				}
				if (string.IsNullOrEmpty(text))
				{
					return string.Format(CultureInfo.CurrentCulture, "{0}, {1}", base.Message, GenerateBaseUserAgentString());
				}
				return string.Format(CultureInfo.CurrentUICulture, "{0}, {1}, {2}", base.Message, text, GenerateBaseUserAgentString());
			}
		}

		internal string RawErrorMessage => base.Message;

		internal object RequestStatistics
		{
			get;
			set;
		}

		internal long LSN
		{
			get;
			set;
		}

		internal string PartitionKeyRangeId
		{
			get;
			set;
		}

		internal string ResourceAddress
		{
			get;
			set;
		}

		/// <summary>
		/// Gets the request uri from the current exception from the Azure Cosmos DB service.
		/// </summary>
		internal Uri RequestUri
		{
			get;
			private set;
		}

		internal DocumentClientExceptionInternal(Error errorResource, HttpResponseHeaders responseHeaders, HttpStatusCode? statusCode)
			: base(MessageWithActivityId(errorResource.Message, responseHeaders))
		{
			error = errorResource;
			this.responseHeaders = new NameValueCollection();
			StatusCode = statusCode;
			if (responseHeaders != null)
			{
				foreach (var responseHeader in responseHeaders)
				{
					this.responseHeaders.Add(responseHeader.Key, string.Join(",", responseHeader.Value));
				}
			}
			Guid activityId = Trace.CorrelationManager.ActivityId;
			if (this.responseHeaders.Get("x-ms-activity-id") == null)
			{
				this.responseHeaders.Set("x-ms-activity-id", Trace.CorrelationManager.ActivityId.ToString());
			}
			LSN = -1L;
			PartitionKeyRangeId = null;
			if (StatusCode != HttpStatusCode.Gone)
			{
				DefaultTrace.TraceError("DocumentClientException with status code: {0}, message: {1}, and response headers: {2}", 
					StatusCode ?? ((HttpStatusCode)0), errorResource.Message, SerializeHTTPResponseHeaders(responseHeaders));
			}
		}

		internal DocumentClientExceptionInternal(string message, Exception innerException, 
			HttpStatusCode? statusCode, Uri requestUri = null, string statusDescription = null)
			: this(MessageWithActivityId(message), innerException, null, statusCode, requestUri)
		{
		}

		internal DocumentClientExceptionInternal(string message, Exception innerException, 
			HttpResponseHeaders responseHeaders, HttpStatusCode? statusCode, Uri requestUri = null, uint? substatusCode = null)
			: base(MessageWithActivityId(message, responseHeaders), innerException)
		{
			this.responseHeaders = new NameValueCollection();
			StatusCode = statusCode;
			substatus = substatusCode;
			if (responseHeaders != null)
			{
				foreach (KeyValuePair<string, IEnumerable<string>> responseHeader in responseHeaders)
				{
					this.responseHeaders.Add(responseHeader.Key, string.Join(",", responseHeader.Value));
				}
			}
			Guid activityId = Trace.CorrelationManager.ActivityId;
			this.responseHeaders.Set("x-ms-activity-id", Trace.CorrelationManager.ActivityId.ToString());
			RequestUri = requestUri;
			LSN = -1L;
			PartitionKeyRangeId = null;
			if (StatusCode != HttpStatusCode.Gone)
			{
				DefaultTrace.TraceError("DocumentClientException with status code {0}, message: {1}, inner exception: {2}, and response headers: {3}", 
					StatusCode ?? ((HttpStatusCode)0), message, 
					(innerException != null) ? innerException.ToString() : "null", 
					SerializeHTTPResponseHeaders(responseHeaders));
			}
		}

		internal DocumentClientExceptionInternal(string message, Exception innerException, NameValueCollection responseHeaders, HttpStatusCode? statusCode, 
				uint? substatusCode = null, Uri requestUri = null)
			: this(message, innerException, responseHeaders, statusCode, requestUri)
		{
			substatus = substatusCode;
		}

		internal DocumentClientExceptionInternal(string message, Exception innerException, NameValueCollection responseHeaders, HttpStatusCode? statusCode, 
				Uri requestUri = null)
			: base(MessageWithActivityId(message, responseHeaders), innerException)
		{
			this.responseHeaders = new NameValueCollection();
			StatusCode = statusCode;
			if (responseHeaders != null)
			{
				this.responseHeaders.Add(responseHeaders);
			}
			Guid activityId = Trace.CorrelationManager.ActivityId;
			this.responseHeaders.Set("x-ms-activity-id", Trace.CorrelationManager.ActivityId.ToString());
			RequestUri = requestUri;
			LSN = -1L;
			PartitionKeyRangeId = null;
			if (StatusCode != HttpStatusCode.Gone)
			{
				DefaultTrace.TraceError("DocumentClientException with status code {0}, message: {1}, inner exception: {2}, and response headers: {3}", StatusCode ?? ((HttpStatusCode)0), message, (innerException != null) ? innerException.ToString() : "null", SerializeHTTPResponseHeaders(responseHeaders));
			}
		}

		internal DocumentClientExceptionInternal(string message, HttpStatusCode statusCode, uint? subStatusCode)
			: this(message, null, statusCode)
		{
			substatus = subStatusCode;
			responseHeaders["x-ms-substatus"] = ((int)substatus.Value).ToString(CultureInfo.InvariantCulture);
		}

		private static string MessageWithActivityId(string message, NameValueCollection responseHeaders)
		{
			string[] array = null;
			if (responseHeaders != null)
			{
				array = responseHeaders.GetValues("x-ms-activity-id");
			}
			if (array != null)
			{
				return MessageWithActivityId(message, array.FirstOrDefault());
			}
			return MessageWithActivityId(message);
		}

		private static string MessageWithActivityId(string message, HttpResponseHeaders responseHeaders)
		{
			IEnumerable<string> values = null;
			if (responseHeaders != null && responseHeaders.TryGetValues("x-ms-activity-id", out values) && values != null)
			{
				return MessageWithActivityId(message, values.FirstOrDefault());
			}
			return MessageWithActivityId(message);
		}

		private static string MessageWithActivityId(string message, string activityIdFromHeaders = null)
		{
			string text = null;
			if (!string.IsNullOrEmpty(activityIdFromHeaders))
			{
				text = activityIdFromHeaders;
			}
			else
			{
				if (!(Trace.CorrelationManager.ActivityId != Guid.Empty))
				{
					return message;
				}
				text = Trace.CorrelationManager.ActivityId.ToString();
			}
			if (message.Contains(text))
			{
				return message;
			}
			return string.Format(CultureInfo.InvariantCulture, "{0}" + Environment.NewLine + "ActivityId: {1}", message, text);
		}

		private static string SerializeHTTPResponseHeaders(HttpResponseHeaders responseHeaders)
		{
			if (responseHeaders == null)
			{
				return "null";
			}
			StringBuilder stringBuilder = new StringBuilder("{");
			stringBuilder.Append(Environment.NewLine);
			foreach (KeyValuePair<string, IEnumerable<string>> responseHeader in responseHeaders)
			{
				foreach (string item in responseHeader.Value)
				{
					stringBuilder.Append(string.Format(CultureInfo.InvariantCulture, "\"{0}\": \"{1}\",{2}", responseHeader.Key, item, Environment.NewLine));
				}
			}
			stringBuilder.Append("}");
			return stringBuilder.ToString();
		}

		internal uint GetSubStatus()
		{
			if (!substatus.HasValue)
			{
				substatus = null;
				string text = responseHeaders.Get("x-ms-substatus");
				if (!string.IsNullOrEmpty(text))
				{
					uint result = 0u;
					if (uint.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
					{
						substatus = result;
					}
				}
			}
			if (!substatus.HasValue)
			{
				return 0u;
			}
			return substatus.Value;
		}

		private static string SerializeHTTPResponseHeaders(NameValueCollection responseHeaders)
		{
			if (responseHeaders == null)
			{
				return "null";
			}
			IEnumerable<Tuple<string, string>> enumerable = responseHeaders.AllKeys
				.SelectMany(responseHeaders.GetValues, (string k, string v) => new Tuple<string, string>(k, v));
			StringBuilder stringBuilder = new StringBuilder("{");
			stringBuilder.Append(Environment.NewLine);
			foreach (Tuple<string, string> item in enumerable)
			{
				stringBuilder.Append(string.Format(CultureInfo.InvariantCulture, "\"{0}\": \"{1}\",{2}", item.Item1, item.Item2, Environment.NewLine));
			}
			stringBuilder.Append("}");
			return stringBuilder.ToString();
		}
			
		private static double GetHeaderValueDouble(NameValueCollection headerValues, string headerName, double defaultValue = -1.0)
		{
			double result = defaultValue;
			string text = headerValues[headerName];
			if (!string.IsNullOrEmpty(text) && !double.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out result))
			{
				result = defaultValue;
			}
			return result;
		}

		private static string GenerateBaseUserAgentString()
		{
			string oSVersion = System.Environment.OSVersion.VersionString;
			return string.Format(CultureInfo.InvariantCulture, "{0}/{1} {2}/{3}", 
				System.Environment.OSVersion.Platform.ToString(), 
				string.IsNullOrEmpty(oSVersion) ? "Unknown" : oSVersion.Trim(), "documentdb-netcore-sdk", "2.4.0");
		}


	}
}
