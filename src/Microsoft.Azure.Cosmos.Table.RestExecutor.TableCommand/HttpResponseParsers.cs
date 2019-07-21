using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.TableCommand
{
	internal static class HttpResponseParsers
	{
		internal static T ProcessExpectedStatusCodeNoException<T>(HttpStatusCode expectedStatusCode, HttpResponseMessage resp, T retVal, RESTCommand<T> cmd, Exception ex)
		{
			return ProcessExpectedStatusCodeNoException(expectedStatusCode, resp?.StatusCode ?? HttpStatusCode.Unused, retVal, cmd, ex);
		}

		internal static T ProcessExpectedStatusCodeNoException<T>(HttpStatusCode[] expectedStatusCodes, HttpResponseMessage resp, T retVal, RESTCommand<T> cmd, Exception ex)
		{
			return ProcessExpectedStatusCodeNoException(expectedStatusCodes, resp?.StatusCode ?? HttpStatusCode.Unused, retVal, cmd, ex);
		}

		internal static T ProcessExpectedStatusCodeNoException<T>(HttpStatusCode expectedStatusCode, HttpStatusCode actualStatusCode, T retVal, RESTCommand<T> cmd, Exception ex)
		{
			if (ex != null)
			{
				throw ex;
			}
			if (actualStatusCode != expectedStatusCode)
			{
				throw new StorageException(cmd.CurrentResult, string.Format(CultureInfo.InvariantCulture, "Unexpected response code, Expected:{0}, Received:{1}", expectedStatusCode, actualStatusCode), null);
			}
			return retVal;
		}

		internal static T ProcessExpectedStatusCodeNoException<T>(HttpStatusCode[] expectedStatusCodes, HttpStatusCode actualStatusCode, T retVal, RESTCommand<T> cmd, Exception ex)
		{
			if (ex != null)
			{
				throw ex;
			}
			if (!expectedStatusCodes.Contains(actualStatusCode))
			{
				string arg = string.Join(",", expectedStatusCodes);
				throw new StorageException(cmd.CurrentResult, string.Format(CultureInfo.InvariantCulture, "Unexpected response code, Expected:{0}, Received:{1}", arg, actualStatusCode.ToString()), null);
			}
			return retVal;
		}
	}
}
