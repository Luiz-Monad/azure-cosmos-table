using Microsoft.Azure.Cosmos.Tables.SharedFiles;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Azure.Cosmos.Tables.ResourceModel
{
	internal static class EtagHelper
	{
		internal const string ETagPrefix = "\"datetime'";

		private static List<int> byteOrder = new List<int>
		{
			13,
			12,
			11,
			10,
			9,
			8,
			15,
			14,
			1,
			0,
			3,
			2,
			7,
			6,
			5,
			4
		};

		public static DateTimeOffset GetTimeOffSetFromBackEndETagFormat(string etag)
		{
			if (string.IsNullOrEmpty(etag))
			{
				throw new InvalidOperationException();
			}
			etag = etag.Trim(new char[1]
			{
				'"'
			});
			byte[] array = new Guid(etag).ToByteArray();
			byte[] array2 = new byte[8];
			for (int i = 0; i < 8; i++)
			{
				Buffer.SetByte(array2, i, array[byteOrder[i]]);
			}
			return DateTimeOffset.FromFileTime(BitConverter.ToInt64(array2, 0)).ToUniversalTime();
		}

		public static string ConvertFromBackEndETagFormat(string etag)
		{
			if (string.IsNullOrEmpty(etag))
			{
				return etag;
			}
			string arg = Uri.EscapeDataString(GetTimeOffSetFromBackEndETagFormat(etag).UtcDateTime.ToString("o", CultureInfo.InvariantCulture));
			return string.Format(CultureInfo.InvariantCulture, "W/\"datetime'{0}'\"", arg);
		}

		public static string ConvertToBackEndETagFormat(string tableEtag)
		{
			if (string.IsNullOrEmpty(tableEtag) || tableEtag == "*")
			{
				return tableEtag;
			}
			if (tableEtag.StartsWith("W/", StringComparison.Ordinal))
			{
				string text = tableEtag.Substring(2);
				if (text.Length < "\"datetime'".Length + 2)
				{
					throw new InvalidEtagException("Invalid Etag format.", tableEtag);
				}
				if (!text.StartsWith("\"datetime'", StringComparison.Ordinal))
				{
					throw new InvalidEtagException("Invalid Etag format.", tableEtag);
				}
				text = text.Substring("\"datetime'".Length, text.Length - 2 - "\"datetime'".Length);
				if (!DateTimeOffset.TryParse(Uri.UnescapeDataString(text), CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset result))
				{
					throw new InvalidEtagException("Invalid Etag format.", tableEtag);
				}
				byte[] bytes = BitConverter.GetBytes(result.ToFileTime());
				byte[] array = new byte[16];
				byte[] bytes2 = BitConverter.GetBytes(0L);
				for (int i = 0; i < 8; i++)
				{
					byte value = (i < 8) ? bytes[i] : bytes2[0];
					Buffer.SetByte(array, byteOrder[i], value);
				}
				Guid guid = new Guid(array);
				return string.Format(CultureInfo.InvariantCulture, "\"{0}\"", guid.ToString());
			}
			throw new InvalidEtagException("Invalid Etag format.", tableEtag);
		}
	}
}
