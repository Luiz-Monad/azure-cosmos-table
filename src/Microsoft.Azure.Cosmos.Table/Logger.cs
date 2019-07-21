using Microsoft.Azure.Documents.Internals;
using System.Globalization;

namespace Microsoft.Azure.Cosmos.Table
{
	internal static class Logger
	{
		internal static void LogError(OperationContext operationContext, string format, params object[] args)
		{
			DefaultTrace.TraceError(FormatLine(operationContext, format, args));
		}

		internal static void LogWarning(OperationContext operationContext, string format, params object[] args)
		{
			DefaultTrace.TraceWarning(FormatLine(operationContext, format, args));
		}

		internal static void LogInformational(OperationContext operationContext, string format, params object[] args)
		{
			DefaultTrace.TraceInformation(FormatLine(operationContext, format, args));
		}

		internal static void LogVerbose(OperationContext operationContext, string format, params object[] args)
		{
			DefaultTrace.TraceVerbose(FormatLine(operationContext, format, args));
		}

		private static string FormatLine(OperationContext operationContext, string format, object[] args)
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}: {1}", (operationContext == null) ? "*" : operationContext.ClientRequestID, (args == null) ? format : string.Format(CultureInfo.InvariantCulture, format, args).Replace('\n', '.'));
		}
	}
}
