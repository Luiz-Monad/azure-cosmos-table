using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Microsoft.Azure.Documents.Interop.Common.Schema
{
	internal static class SchemaUtil
	{
		public static object ConvertEdmType(object value)
		{
			if (value == null)
			{
				return "null";
			}
			Type type = value.GetType();
			if (type.IsValueType)
			{
				if (type == typeof(bool))
				{
					if (!(bool)value)
					{
						return "false";
					}
					return "true";
				}
				if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(decimal) || type == typeof(double) || type == typeof(float) || type == typeof(int) || type == typeof(uint) || type == typeof(short) || type == typeof(ushort))
				{
					return Convert.ToString(value, NumberFormatInfo.InvariantInfo);
				}
				if (type == typeof(long))
				{
					return GetQuotedString(((long)value).ToString("D20", CultureInfo.InvariantCulture));
				}
				if (type == typeof(ulong))
				{
					return GetQuotedString(((ulong)value).ToString("D20", CultureInfo.InvariantCulture));
				}
				if (type == typeof(Guid))
				{
					return GetQuotedString(((Guid)value).ToString());
				}
				if (type == typeof(DateTime))
				{
					return GetQuotedString(GetUtcTicksString((DateTime)value));
				}
				if (type == typeof(DateTimeOffset))
				{
					return GetQuotedString(GetUtcTicksString(((DateTimeOffset)value).UtcDateTime));
				}
			}
			if (type == typeof(string))
			{
				return GetQuotedString((string)value);
			}
			if (type == typeof(byte[]))
			{
				return GetQuotedString(BytesToString((byte[])value));
			}
			throw new InvalidCastException();
		}

		public static object ConvertEdmType<T>(T? value) where T : struct
		{
			if (!value.HasValue)
			{
				return "null";
			}
			return ConvertEdmType(value.Value);
		}

		public static bool IsNullable(Type type)
		{
			if (type.IsGenericType)
			{
				return type.GetGenericTypeDefinition() == typeof(Nullable<>);
			}
			return false;
		}

		public static string GetTimestampEpochSecondString(object dateTime)
		{
			if (dateTime == null)
			{
				throw new ArgumentNullException("dateTime", "Timestamp value shouldn't be null");
			}
			Type type = dateTime.GetType();
			if (type.IsValueType && type == typeof(DateTimeOffset))
			{
				return (((DateTimeOffset)dateTime).UtcDateTime.Ticks / 10000000 - 62135596800L).ToString();
			}
			throw new InvalidCastException("Timestamp value should be DateTimeOffset type");
		}

		public static string GetUtcTicksString(DateTime dateTime)
		{
			return dateTime.ToUniversalTime().Ticks.ToString("D20", CultureInfo.InvariantCulture);
		}

		public static DateTime GetDateTimeFromUtcTicks(long utcTicks)
		{
			return new DateTime(utcTicks, DateTimeKind.Utc);
		}

		public static string BytesToString(byte[] bytes)
		{
			if (bytes == null)
			{
				throw new ArgumentNullException("bytes");
			}
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < bytes.Length; i++)
			{
				stringBuilder.Append((char)bytes[i]);
			}
			return stringBuilder.ToString();
		}

		public static byte[] StringToBytes(string bytesString)
		{
			if (bytesString == null)
			{
				throw new ArgumentNullException("bytesString");
			}
			byte[] array = new byte[bytesString.Length];
			for (int i = 0; i < bytesString.Length; i++)
			{
				array[i] = (byte)bytesString[i];
			}
			return array;
		}

		public static bool IsIdentifierCharacter(char c)
		{
			if (c != '_')
			{
				return char.IsLetterOrDigit(c);
			}
			return true;
		}

		public static int GetHexValue(char c)
		{
			if (c >= '0' && c <= '9')
			{
				return c - 48;
			}
			if (c >= 'a' && c <= 'f')
			{
				return c - 97 + 10;
			}
			if (c >= 'A' && c <= 'F')
			{
				return c - 65 + 10;
			}
			return -1;
		}

		public static bool IsHexCharacter(char c)
		{
			if (c >= '0' && c <= '9')
			{
				return true;
			}
			if (c >= 'a' && c <= 'f')
			{
				return true;
			}
			if (c >= 'A' && c <= 'F')
			{
				return true;
			}
			return false;
		}

		public static string GetQuotedString(string value, bool escapeValue = true, char quotationCharacter = '"')
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			string str = escapeValue ? GetEscapedString(value) : value;
			return quotationCharacter.ToString() + str + quotationCharacter.ToString();
		}

		public static string GetEscapedString(string value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if (value.All((char c) => !IsEscapedCharacter(c)))
			{
				return value;
			}
			StringBuilder stringBuilder = new StringBuilder(value.Length);
			foreach (char c2 in value)
			{
				switch (c2)
				{
				case '"':
					stringBuilder.Append("\\\"");
					continue;
				case '\\':
					stringBuilder.Append("\\\\");
					continue;
				case '\b':
					stringBuilder.Append("\\b");
					continue;
				case '\f':
					stringBuilder.Append("\\f");
					continue;
				case '\n':
					stringBuilder.Append("\\n");
					continue;
				case '\r':
					stringBuilder.Append("\\r");
					continue;
				case '\t':
					stringBuilder.Append("\\t");
					continue;
				}
				switch (char.GetUnicodeCategory(c2))
				{
				case UnicodeCategory.UppercaseLetter:
				case UnicodeCategory.LowercaseLetter:
				case UnicodeCategory.TitlecaseLetter:
				case UnicodeCategory.OtherLetter:
				case UnicodeCategory.DecimalDigitNumber:
				case UnicodeCategory.LetterNumber:
				case UnicodeCategory.OtherNumber:
				case UnicodeCategory.SpaceSeparator:
				case UnicodeCategory.ConnectorPunctuation:
				case UnicodeCategory.DashPunctuation:
				case UnicodeCategory.OpenPunctuation:
				case UnicodeCategory.ClosePunctuation:
				case UnicodeCategory.InitialQuotePunctuation:
				case UnicodeCategory.FinalQuotePunctuation:
				case UnicodeCategory.OtherPunctuation:
				case UnicodeCategory.MathSymbol:
				case UnicodeCategory.CurrencySymbol:
				case UnicodeCategory.ModifierSymbol:
				case UnicodeCategory.OtherSymbol:
					stringBuilder.Append(c2);
					break;
				default:
					stringBuilder.AppendFormat("\\u{0:x4}", (int)c2);
					break;
				}
			}
			return stringBuilder.ToString();
		}

		private static bool IsEscapedCharacter(char c)
		{
			switch (c)
			{
			case '\b':
			case '\t':
			case '\n':
			case '\f':
			case '\r':
			case '"':
			case '\\':
				return true;
			default:
				switch (char.GetUnicodeCategory(c))
				{
				case UnicodeCategory.UppercaseLetter:
				case UnicodeCategory.LowercaseLetter:
				case UnicodeCategory.TitlecaseLetter:
				case UnicodeCategory.OtherLetter:
				case UnicodeCategory.DecimalDigitNumber:
				case UnicodeCategory.LetterNumber:
				case UnicodeCategory.OtherNumber:
				case UnicodeCategory.SpaceSeparator:
				case UnicodeCategory.ConnectorPunctuation:
				case UnicodeCategory.DashPunctuation:
				case UnicodeCategory.OpenPunctuation:
				case UnicodeCategory.ClosePunctuation:
				case UnicodeCategory.InitialQuotePunctuation:
				case UnicodeCategory.FinalQuotePunctuation:
				case UnicodeCategory.OtherPunctuation:
				case UnicodeCategory.MathSymbol:
				case UnicodeCategory.CurrencySymbol:
				case UnicodeCategory.ModifierSymbol:
				case UnicodeCategory.OtherSymbol:
					return false;
				default:
					return true;
				}
			}
		}
	}
}
