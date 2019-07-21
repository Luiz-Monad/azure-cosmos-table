using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

namespace Microsoft.Azure.Cosmos.Table.Queryable
{
	internal static class ClientConvert
	{
		internal enum StorageType
		{
			Boolean,
			Byte,
			ByteArray,
			Char,
			CharArray,
			DateTime,
			DateTimeOffset,
			Decimal,
			Double,
			Guid,
			Int16,
			Int32,
			Int64,
			Single,
			String,
			SByte,
			TimeSpan,
			Type,
			UInt16,
			UInt32,
			UInt64,
			Uri,
			XDocument,
			XElement,
			Binary
		}

		private const string SystemDataLinq = "System.Data.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";

		private static readonly Type[] KnownTypes = CreateKnownPrimitives();

		private static readonly Dictionary<string, Type> NamedTypesMap = CreateKnownNamesMap();

		private static bool needSystemDataLinqBinary = true;

		internal static object ChangeType(string propertyValue, Type propertyType)
		{
			try
			{
				switch (IndexOfStorage(propertyType))
				{
				case 0:
					return XmlConvert.ToBoolean(propertyValue);
				case 1:
					return XmlConvert.ToByte(propertyValue);
				case 2:
					return Convert.FromBase64String(propertyValue);
				case 3:
					return XmlConvert.ToChar(propertyValue);
				case 4:
					return propertyValue.ToCharArray();
				case 5:
					return XmlConvert.ToDateTime(propertyValue, XmlDateTimeSerializationMode.RoundtripKind);
				case 6:
					return XmlConvert.ToDateTimeOffset(propertyValue);
				case 7:
					return XmlConvert.ToDecimal(propertyValue);
				case 8:
					return XmlConvert.ToDouble(propertyValue);
				case 9:
					return new Guid(propertyValue);
				case 10:
					return XmlConvert.ToInt16(propertyValue);
				case 11:
					return XmlConvert.ToInt32(propertyValue);
				case 12:
					return XmlConvert.ToInt64(propertyValue);
				case 13:
					return XmlConvert.ToSingle(propertyValue);
				case 14:
					return propertyValue;
				case 15:
					return XmlConvert.ToSByte(propertyValue);
				case 16:
					return XmlConvert.ToTimeSpan(propertyValue);
				case 17:
					return Type.GetType(propertyValue, throwOnError: true);
				case 18:
					return XmlConvert.ToUInt16(propertyValue);
				case 19:
					return XmlConvert.ToUInt32(propertyValue);
				case 20:
					return XmlConvert.ToUInt64(propertyValue);
				case 21:
					return CreateUri(propertyValue, UriKind.RelativeOrAbsolute);
				case 22:
					return (0 < propertyValue.Length) ? XDocument.Parse(propertyValue) : new XDocument();
				case 23:
					return XElement.Parse(propertyValue);
				case 24:
					return Activator.CreateInstance(KnownTypes[24], Convert.FromBase64String(propertyValue));
				default:
					return propertyValue;
				}
			}
			catch (FormatException innerException)
			{
				propertyValue = ((propertyValue.Length == 0) ? "String.Empty" : "String");
				throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "The current value '{1}' type is not compatible with the expected '{0}' type.", propertyType.ToString(), propertyValue), innerException);
			}
			catch (OverflowException innerException2)
			{
				propertyValue = ((propertyValue.Length == 0) ? "String.Empty" : "String");
				throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "The current value '{1}' type is not compatible with the expected '{0}' type.", propertyType.ToString(), propertyValue), innerException2);
			}
		}

		internal static bool IsBinaryValue(object value)
		{
			return 24 == IndexOfStorage(value.GetType());
		}

		internal static bool TryKeyBinaryToString(object binaryValue, out string result)
		{
			return WebConvert.TryKeyPrimitiveToString((byte[])binaryValue.GetType().InvokeMember("ToArray", BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod, null, binaryValue, null, null, CultureInfo.InvariantCulture, null), out result);
		}

		internal static bool TryKeyPrimitiveToString(object value, out string result)
		{
			if (IsBinaryValue(value))
			{
				return TryKeyBinaryToString(value, out result);
			}
			if (value is DateTimeOffset)
			{
				value = ((DateTimeOffset)value).UtcDateTime;
			}
			else if (value is DateTimeOffset?)
			{
				value = ((DateTimeOffset?)value).Value.UtcDateTime;
			}
			return WebConvert.TryKeyPrimitiveToString(value, out result);
		}

		internal static bool ToNamedType(string typeName, out Type type)
		{
			type = typeof(string);
			if (!string.IsNullOrEmpty(typeName))
			{
				return NamedTypesMap.TryGetValue(typeName, out type);
			}
			return true;
		}

		internal static string ToTypeName(Type type)
		{
			foreach (KeyValuePair<string, Type> item in NamedTypesMap)
			{
				if (item.Value == type)
				{
					return item.Key;
				}
			}
			return type.FullName;
		}

		internal static string ToString(object propertyValue, bool atomDateConstruct)
		{
			switch (IndexOfStorage(propertyValue.GetType()))
			{
			case 0:
				return XmlConvert.ToString((bool)propertyValue);
			case 1:
				return XmlConvert.ToString((byte)propertyValue);
			case 2:
				return Convert.ToBase64String((byte[])propertyValue);
			case 3:
				return XmlConvert.ToString((char)propertyValue);
			case 4:
				return new string((char[])propertyValue);
			case 5:
			{
				DateTime dateTime = (DateTime)propertyValue;
				return XmlConvert.ToString((dateTime.Kind == DateTimeKind.Unspecified && atomDateConstruct) ? new DateTime(dateTime.Ticks, DateTimeKind.Utc) : dateTime, XmlDateTimeSerializationMode.RoundtripKind);
			}
			case 6:
				return XmlConvert.ToString((DateTimeOffset)propertyValue);
			case 7:
				return XmlConvert.ToString((decimal)propertyValue);
			case 8:
				return XmlConvert.ToString((double)propertyValue);
			case 9:
				return ((Guid)propertyValue).ToString();
			case 10:
				return XmlConvert.ToString((short)propertyValue);
			case 11:
				return XmlConvert.ToString((int)propertyValue);
			case 12:
				return XmlConvert.ToString((long)propertyValue);
			case 13:
				return XmlConvert.ToString((float)propertyValue);
			case 14:
				return (string)propertyValue;
			case 15:
				return XmlConvert.ToString((sbyte)propertyValue);
			case 16:
				return XmlConvert.ToString((TimeSpan)propertyValue);
			case 17:
				return ((Type)propertyValue).AssemblyQualifiedName;
			case 18:
				return XmlConvert.ToString((ushort)propertyValue);
			case 19:
				return XmlConvert.ToString((uint)propertyValue);
			case 20:
				return XmlConvert.ToString((ulong)propertyValue);
			case 21:
				return ((Uri)propertyValue).ToString();
			case 22:
				return ((XDocument)propertyValue).ToString();
			case 23:
				return ((XElement)propertyValue).ToString();
			case 24:
				return propertyValue.ToString();
			default:
				return propertyValue.ToString();
			}
		}

		internal static bool IsKnownType(Type type)
		{
			return 0 <= IndexOfStorage(type);
		}

		internal static bool IsKnownNullableType(Type type)
		{
			return IsKnownType(Nullable.GetUnderlyingType(type) ?? type);
		}

		internal static bool IsSupportedPrimitiveTypeForUri(Type type)
		{
			return ContainsReference(NamedTypesMap.Values.ToArray(), type);
		}

		internal static bool ContainsReference<T>(T[] array, T value) where T : class
		{
			return 0 <= IndexOfReference(array, value);
		}

		internal static string GetEdmType(Type propertyType)
		{
			switch (IndexOfStorage(propertyType))
			{
			case 0:
				return "Edm.Boolean";
			case 1:
				return "Edm.Byte";
			case 2:
			case 24:
				return "Edm.Binary";
			case 5:
				return "Edm.DateTime";
			case 7:
				return "Edm.Decimal";
			case 8:
				return "Edm.Double";
			case 9:
				return "Edm.Guid";
			case 10:
				return "Edm.Int16";
			case 11:
				return "Edm.Int32";
			case 12:
				return "Edm.Int64";
			case 13:
				return "Edm.Single";
			case 15:
				return "Edm.SByte";
			case 6:
			case 16:
			case 18:
			case 19:
			case 20:
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "Can't cast to unsupported type '{0}'", propertyType.Name));
			case 3:
			case 4:
			case 14:
			case 17:
			case 21:
			case 22:
			case 23:
				return null;
			default:
				return null;
			}
		}

		private static Type[] CreateKnownPrimitives()
		{
			return new Type[25]
			{
				typeof(bool),
				typeof(byte),
				typeof(byte[]),
				typeof(char),
				typeof(char[]),
				typeof(DateTime),
				typeof(DateTimeOffset),
				typeof(decimal),
				typeof(double),
				typeof(Guid),
				typeof(short),
				typeof(int),
				typeof(long),
				typeof(float),
				typeof(string),
				typeof(sbyte),
				typeof(TimeSpan),
				typeof(Type),
				typeof(ushort),
				typeof(uint),
				typeof(ulong),
				typeof(Uri),
				typeof(XDocument),
				typeof(XElement),
				null
			};
		}

		private static Dictionary<string, Type> CreateKnownNamesMap()
		{
			return new Dictionary<string, Type>(EqualityComparer<string>.Default)
			{
				{
					"Edm.String",
					typeof(string)
				},
				{
					"Edm.Boolean",
					typeof(bool)
				},
				{
					"Edm.Byte",
					typeof(byte)
				},
				{
					"Edm.DateTime",
					typeof(DateTime)
				},
				{
					"Edm.Decimal",
					typeof(decimal)
				},
				{
					"Edm.Double",
					typeof(double)
				},
				{
					"Edm.Guid",
					typeof(Guid)
				},
				{
					"Edm.Int16",
					typeof(short)
				},
				{
					"Edm.Int32",
					typeof(int)
				},
				{
					"Edm.Int64",
					typeof(long)
				},
				{
					"Edm.SByte",
					typeof(sbyte)
				},
				{
					"Edm.Single",
					typeof(float)
				},
				{
					"Edm.Binary",
					typeof(byte[])
				}
			};
		}

		private static int IndexOfStorage(Type type)
		{
			int num = IndexOfReference(KnownTypes, type);
			if (num < 0 && needSystemDataLinqBinary && type.Name == "Binary")
			{
				return LoadSystemDataLinqBinary(type);
			}
			return num;
		}

		internal static int IndexOfReference<T>(T[] array, T value) where T : class
		{
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] == value)
				{
					return i;
				}
			}
			return -1;
		}

		internal static Uri CreateUri(string value, UriKind kind)
		{
			if (value != null)
			{
				return new Uri(value, kind);
			}
			return null;
		}

		private static int LoadSystemDataLinqBinary(Type type)
		{
			if (type.Namespace == "System.Data.Linq" && AssemblyName.ReferenceMatchesDefinition(type.Assembly.GetName(), new AssemblyName("System.Data.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")))
			{
				KnownTypes[24] = type;
				needSystemDataLinqBinary = false;
				return 24;
			}
			return -1;
		}
	}
}
