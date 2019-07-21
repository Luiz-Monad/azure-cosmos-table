#define TRACE
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;

namespace Microsoft.Azure.Documents.Internals
{
	internal static class DefaultTrace
	{
		public static Guid ProviderId;

		private static TraceSource TraceSourceInternal;

		private static bool IsListenerAdded;

		public static TraceSource TraceSource => TraceSourceInternal;

		static DefaultTrace()
		{
			ProviderId = new Guid("{B30ABF1C-6A50-4F2B-85C4-61823ED6CF24}");
			TraceSourceInternal = new TraceSource("DocDBTrace");
			IsListenerAdded = false;
			System.Diagnostics.Trace.UseGlobalLock = false;
		}

		/// <summary>
		/// Only client need to init this listener
		/// </summary>
		public static void InitEventListener()
		{
			if (!IsListenerAdded)
			{
				IsListenerAdded = true;
			}
		}

		public static void Flush()
		{
			TraceSource.Flush();
		}

		public static void TraceVerbose(string message)
		{
			TraceSource.TraceEvent(TraceEventType.Verbose, 0, message);
		}

		public static void TraceVerbose(string format, params object[] args)
		{
			TraceSource.TraceEvent(TraceEventType.Verbose, 0, format, args);
		}

		public static void TraceInformation(string message)
		{
			TraceSource.TraceInformation(message);
		}

		public static void TraceInformation(string format, params object[] args)
		{
			TraceSource.TraceInformation(format, args);
		}

		public static void TraceWarning(string message)
		{
			TraceSource.TraceEvent(TraceEventType.Warning, 0, message);
		}

		public static void TraceWarning(string format, params object[] args)
		{
			TraceSource.TraceEvent(TraceEventType.Warning, 0, format, args);
		}

		public static void TraceError(string message)
		{
			TraceSource.TraceEvent(TraceEventType.Error, 0, message);
		}

		public static void TraceError(string format, params object[] args)
		{
			TraceSource.TraceEvent(TraceEventType.Error, 0, format, args);
		}

		public static void TraceCritical(string message)
		{
			TraceSource.TraceEvent(TraceEventType.Critical, 0, message);
		}

		public static void TraceCritical(string format, params object[] args)
		{
			TraceSource.TraceEvent(TraceEventType.Critical, 0, format, args);
		}

		public static void TraceException(Exception e)
		{
			AggregateException ex = e as AggregateException;
			if (ex != null)
			{
				foreach (Exception innerException in ex.InnerExceptions)
				{
					TraceExceptionInternal(innerException);
				}
			}
			else
			{
				TraceExceptionInternal(e);
			}
		}

		private static void TraceExceptionInternal(Exception e)
		{
			while (e != null)
			{
				Uri uri = null;
				DocumentClientExceptionInternal ex = e as DocumentClientExceptionInternal;
				if (ex != null)
				{
					uri = ex.RequestUri;
				}
				SocketException ex2 = e as SocketException;
				if (ex2 != null)
				{
					TraceWarning("Exception {0}: RequesteUri: {1}, SocketErrorCode: {2}, {3}, {4}", ((object)e).GetType(), uri, ex2.SocketErrorCode, e.Message, e.StackTrace);
				}
				else
				{
					TraceWarning("Exception {0}: RequestUri: {1}, {2}, {3}", ((object)e).GetType(), uri, e.Message, e.StackTrace);
				}
				e = e.InnerException;
			}
		}

		/// <summary>
		/// Emit a trace for a set of metric values.
		///
		/// This is intended to be used next to MDM metrics
		///
		/// Details:
		/// Produce a semi-typed trace format as a pipe delimited list of metrics values.
		///
		/// 'TraceMetrics' prefix provides a search term for indexing.
		///
		/// 'name' is an identifier to correlate to call site
		///
		/// Example: TraceMetric|LogServicePoolInfo|0|123|1
		///
		/// </summary>
		/// <param name="name">metric name</param>
		/// <param name="values">sequence of values to be emitted in the trace</param>
		internal static void TraceMetrics(string name, params object[] values)
		{
			TraceInformation(string.Join("|", new object[2]
			{
				"TraceMetrics",
				name
			}.Concat(values)));
		}
	}
}
