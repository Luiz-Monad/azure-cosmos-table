using System;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Microsoft.Azure.Cosmos.Table.Queryable
{
	internal static class WebConvert
	{
		private const string HexValues = "0123456789ABCDEF";

		private const string XmlHexEncodePrefix = "0x";

		internal static string ConvertByteArrayToKeyString(byte[] byteArray)
		{
			StringBuilder stringBuilder = new StringBuilder(3 + byteArray.Length * 2);
			stringBuilder.Append("X");
			stringBuilder.Append("'");
			for (int i = 0; i < byteArray.Length; i++)
			{
				stringBuilder.Append("0123456789ABCDEF"[byteArray[i] >> 4]);
				stringBuilder.Append("0123456789ABCDEF"[byteArray[i] & 0xF]);
			}
			stringBuilder.Append("'");
			return stringBuilder.ToString();
		}

		internal static bool IsKeyTypeQuoted(Type type)
		{
			if (!(type == typeof(XElement)))
			{
				return type == typeof(string);
			}
			return true;
		}

		internal static bool TryKeyPrimitiveToString(object value, out string result)
		{
			if (value.GetType() == typeof(byte[]))
			{
				result = ConvertByteArrayToKeyString((byte[])value);
			}
			else
			{
				if (!TryXmlPrimitiveToString(value, out result))
				{
					return false;
				}
				if (value.GetType() == typeof(DateTime))
				{
					result = "datetime'" + result + "'";
				}
				else if (value.GetType() == typeof(decimal))
				{
					result += "M";
				}
				else if (value.GetType() == typeof(Guid))
				{
					result = "guid'" + result + "'";
				}
				else if (value.GetType() == typeof(long))
				{
					result += "L";
				}
				else if (value.GetType() == typeof(float))
				{
					result += "f";
				}
				else if (value.GetType() == typeof(double))
				{
					result = AppendDecimalMarkerToDouble(result);
				}
				else if (IsKeyTypeQuoted(value.GetType()))
				{
					result = "'" + result.Replace("'", "''") + "'";
				}
			}
			return true;
		}

		internal static bool TryXmlPrimitiveToString(object value, out string result)
		{
			result = null;
			Type type = value.GetType();
			type = (Nullable.GetUnderlyingType(type) ?? type);
			if (typeof(string) == type)
			{
				result = (string)value;
			}
			else if (typeof(bool) == type)
			{
				result = XmlConvert.ToString((bool)value);
			}
			else if (typeof(byte) == type)
			{
				result = XmlConvert.ToString((byte)value);
			}
			else if (typeof(DateTime) == type)
			{
				result = XmlConvert.ToString((DateTime)value, XmlDateTimeSerializationMode.RoundtripKind);
			}
			else if (typeof(decimal) == type)
			{
				result = XmlConvert.ToString((decimal)value);
			}
			else if (typeof(double) == type)
			{
				result = XmlConvert.ToString((double)value);
			}
			else if (typeof(Guid) == type)
			{
				result = value.ToString();
			}
			else if (typeof(short) == type)
			{
				result = XmlConvert.ToString((short)value);
			}
			else if (typeof(int) == type)
			{
				result = XmlConvert.ToString((int)value);
			}
			else if (typeof(long) == type)
			{
				result = XmlConvert.ToString((long)value);
			}
			else if (typeof(sbyte) == type)
			{
				result = XmlConvert.ToString((sbyte)value);
			}
			else if (typeof(float) == type)
			{
				result = XmlConvert.ToString((float)value);
			}
			else if (typeof(byte[]) == type)
			{
				byte[] inArray = (byte[])value;
				result = Convert.ToBase64String(inArray);
			}
			else
			{
				if (ClientConvert.IsBinaryValue(value))
				{
					return ClientConvert.TryKeyBinaryToString(value, out result);
				}
				if (!(typeof(XElement) == type))
				{
					result = null;
					return false;
				}
				result = ((XElement)value).ToString(SaveOptions.None);
			}
			return true;
		}

		private static string AppendDecimalMarkerToDouble(string input)
		{
			for (int i = 0; i < input.Length; i++)
			{
				if (!char.IsDigit(input[i]))
				{
					return input;
				}
			}
			return input + ".0";
		}
	}
}
