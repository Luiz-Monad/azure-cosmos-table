using System.Net.Http.Headers;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.Utils
{
	internal static class HttpResponseMessageUtils
	{
		public static string GetHeaderSingleValueOrDefault(this HttpHeaders headers, string name)
		{
			if (headers.Contains(name))
			{
				return RestUtility.GetFirstHeaderValue(headers.GetValues(name));
			}
			return string.Empty;
		}
	}
}
