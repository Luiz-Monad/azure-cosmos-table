using Microsoft.Azure.Documents.Interop.Common.Schema;
using System;
using System.Globalization;
using System.Text;

namespace Microsoft.Azure.Cosmos.Tables.SharedFiles
{
	internal static class ODataV3Translator
	{
		private const string GuidPrefix = "guid'";

		private const string DateTimeOffSetPrefix = "datetime'";

		private const string BinaryPrefix = "X'";

		private static readonly char[] filterSplit = new char[1]
		{
			' '
		};

		private static readonly char[] terminatingChar = new char[1]
		{
			'\''
		};

		private const string terminatingCharString = "'";

		public static string TranslateFilter(string odataFilter, bool useUtcTicks = true)
		{
			StringBuilder stringBuilder = new StringBuilder();
			string[] array = odataFilter.Split(filterSplit, StringSplitOptions.None);
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i];
				string restOfToken = null;
				string text2 = null;
				if (text.StartsWith("guid'", StringComparison.Ordinal))
				{
					text2 = PrepareFilterContent(text, out restOfToken);
					text = GetGuid(text2);
				}
				else if (text.StartsWith("X'", StringComparison.Ordinal))
				{
					text2 = PrepareFilterContent(text, out restOfToken);
					text = GetBinaryData(text2);
				}
				else if (text.StartsWith("datetime'", StringComparison.Ordinal))
				{
					text2 = PrepareFilterContent(text, out restOfToken);
					text = ((!useUtcTicks) ? removeDateTimePrefix(text2) : GetDateTimeOffset(text2));
				}
				stringBuilder.Append(text);
				if (!string.IsNullOrEmpty(restOfToken))
				{
					stringBuilder.Append(restOfToken);
				}
				stringBuilder.Append(" ");
			}
			return stringBuilder.ToString();
		}

		private static string PrepareFilterContent(string filterToken, out string restOfToken)
		{
			restOfToken = null;
			string text = filterToken;
			if (!text.EndsWith("'", StringComparison.Ordinal))
			{
				int num = text.LastIndexOf("'", StringComparison.Ordinal) + 1;
				if (text.IndexOf("'", StringComparison.Ordinal) + 1 == num)
				{
					throw new InvalidFilterException("Filter token had one single quote instead of two", filterToken);
				}
				text = filterToken.Substring(0, num);
				restOfToken = filterToken.Substring(num);
			}
			return text;
		}

		private static string GetGuid(string filterToken)
		{
			return filterToken.Replace("guid'", "'");
		}

		private static string GetBinaryData(string filterToken)
		{
			int num = filterToken.Length - "X'".Length - 1;
			if (num / 2 > 32)
			{
				throw new InvalidFilterException("Binary filter token length too high", filterToken);
			}
			string text = filterToken.Substring("X'".Length, num);
			if (!uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint result))
			{
				throw new InvalidFilterException("Binary filter token could not be parsed to uint", filterToken);
			}
			byte[] array = BitConverter.GetBytes(result);
			Array.Resize(ref array, text.Length / 2);
			Array.Reverse((Array)array);
			return SchemaUtil.GetQuotedString(SchemaUtil.BytesToString(array), escapeValue: false, '\'');
		}

		private static string GetDateTimeOffset(string filterToken)
		{
			if (!DateTime.TryParse(removeDateTimePrefix(filterToken), out DateTime result))
			{
				throw new InvalidFilterException("DateTime filter token value invalid", filterToken);
			}
			return SchemaUtil.GetUtcTicksString(result);
		}

		private static string removeDateTimePrefix(string filterToken)
		{
			int length = filterToken.Length - "datetime'".Length - 1;
			return filterToken.Substring("datetime'".Length, length);
		}
	}
}
