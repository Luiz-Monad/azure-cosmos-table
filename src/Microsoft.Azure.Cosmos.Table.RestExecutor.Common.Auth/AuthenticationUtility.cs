using Microsoft.Azure.Cosmos.Table.RestExecutor.Utils;
using System;
using System.Net.Http;
using System.Text;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.Common.Auth
{
	internal static class AuthenticationUtility
	{
		private const int ExpectedResourceStringLength = 100;

		public static void AppendCanonicalizedDateHeader(CanonicalizedString canonicalizedString, HttpRequestMessage request, bool allowMicrosoftDateHeader = false)
		{
			string headerSingleValueOrDefault = request.Headers.GetHeaderSingleValueOrDefault("x-ms-date");
			if (string.IsNullOrEmpty(headerSingleValueOrDefault))
			{
				canonicalizedString.AppendCanonicalizedElement(GetCanonicalizedHeaderValue(request.Headers.Date));
			}
			else if (allowMicrosoftDateHeader)
			{
				canonicalizedString.AppendCanonicalizedElement(headerSingleValueOrDefault);
			}
			else
			{
				canonicalizedString.AppendCanonicalizedElement(null);
			}
		}

		public static string GetCanonicalizedHeaderValue(DateTimeOffset? value)
		{
			if (value.HasValue)
			{
				return HttpWebUtility.ConvertDateTimeToHttpString(value.Value);
			}
			return null;
		}

		public static string GetCanonicalizedResourceString(Uri uri, string accountName)
		{
			StringBuilder stringBuilder = new StringBuilder(100);
			stringBuilder.Append('/');
			stringBuilder.Append(accountName);
			stringBuilder.Append(GetAbsolutePathWithoutSecondarySuffix(uri, accountName));
			if (HttpWebUtility.ParseQueryString(uri.Query).TryGetValue("comp", out string value))
			{
				stringBuilder.Append("?comp=");
				stringBuilder.Append(value);
			}
			return stringBuilder.ToString();
		}

		private static string GetAbsolutePathWithoutSecondarySuffix(Uri uri, string accountName)
		{
			string text = uri.AbsolutePath;
			string value = accountName + "-secondary";
			int num = text.IndexOf(value, StringComparison.OrdinalIgnoreCase);
			if (num == 1)
			{
				num += accountName.Length;
				text = text.Remove(num, "-secondary".Length);
			}
			return text;
		}
	}
}
