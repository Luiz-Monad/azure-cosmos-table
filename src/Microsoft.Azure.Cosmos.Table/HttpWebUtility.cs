using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Microsoft.Azure.Cosmos.Table
{
	internal static class HttpWebUtility
	{
		public static IDictionary<string, string> ParseQueryString(string query)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			if (string.IsNullOrEmpty(query))
			{
				return dictionary;
			}
			if (query.StartsWith("?", StringComparison.Ordinal))
			{
				if (query.Length == 1)
				{
					return dictionary;
				}
				query = query.Substring(1);
			}
			string text = query;
			char[] separator = new char[1]
			{
				'&'
			};
			string[] array = text.Split(separator);
			foreach (string text2 in array)
			{
				int num = text2.IndexOf("=", StringComparison.Ordinal);
				string key;
				string text3;
				if (num < 0)
				{
					key = string.Empty;
					text3 = Uri.UnescapeDataString(text2);
				}
				else
				{
					key = Uri.UnescapeDataString(text2.Substring(0, num));
					text3 = Uri.UnescapeDataString(text2.Substring(num + 1));
				}
				dictionary[key] = ((!dictionary.TryGetValue(key, out string value)) ? text3 : (value + "," + text3));
			}
			return dictionary;
		}

		public static string ConvertDateTimeToHttpString(DateTimeOffset dateTime)
		{
			return dateTime.UtcDateTime.ToString("R", CultureInfo.InvariantCulture);
		}

		public static string CombineHttpHeaderValues(IEnumerable<string> headerValues)
		{
			if (headerValues == null)
			{
				return null;
			}
			if (headerValues.Count() != 0)
			{
				return string.Join(",", headerValues);
			}
			return null;
		}
	}
}
