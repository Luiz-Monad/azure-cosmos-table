using System;
using System.Globalization;
using System.Linq;

namespace Microsoft.Azure.Cosmos.Table
{
	public sealed class EntityProperty : IEquatable<EntityProperty>
	{
		private object propertyAsObject;

		public object PropertyAsObject
		{
			get
			{
				return propertyAsObject;
			}
			internal set
			{
				IsNull = (value == null);
				propertyAsObject = value;
			}
		}

		public EdmType PropertyType
		{
			get;
			private set;
		}

		public byte[] BinaryValue
		{
			get
			{
				if (!IsNull)
				{
					EnforceType(EdmType.Binary);
				}
				return (byte[])PropertyAsObject;
			}
			set
			{
				if (value != null)
				{
					EnforceType(EdmType.Binary);
				}
				PropertyAsObject = value;
			}
		}

		public bool? BooleanValue
		{
			get
			{
				if (!IsNull)
				{
					EnforceType(EdmType.Boolean);
				}
				return (bool?)PropertyAsObject;
			}
			set
			{
				if (value.HasValue)
				{
					EnforceType(EdmType.Boolean);
				}
				PropertyAsObject = value;
			}
		}

		public DateTime? DateTime
		{
			get
			{
				if (!IsNull)
				{
					EnforceType(EdmType.DateTime);
				}
				return (DateTime?)PropertyAsObject;
			}
			set
			{
				if (value.HasValue)
				{
					EnforceType(EdmType.DateTime);
				}
				PropertyAsObject = value;
			}
		}

		public DateTimeOffset? DateTimeOffsetValue
		{
			get
			{
				if (!IsNull)
				{
					EnforceType(EdmType.DateTime);
				}
				if (PropertyAsObject == null)
				{
					return null;
				}
				return new DateTimeOffset((DateTime)PropertyAsObject);
			}
			set
			{
				if (value.HasValue)
				{
					EnforceType(EdmType.DateTime);
					PropertyAsObject = value.Value.UtcDateTime;
				}
				else
				{
					PropertyAsObject = null;
				}
			}
		}

		public double? DoubleValue
		{
			get
			{
				if (!IsNull)
				{
					EnforceType(EdmType.Double);
				}
				return (double?)PropertyAsObject;
			}
			set
			{
				if (value.HasValue)
				{
					EnforceType(EdmType.Double);
				}
				PropertyAsObject = value;
			}
		}

		public Guid? GuidValue
		{
			get
			{
				if (!IsNull)
				{
					EnforceType(EdmType.Guid);
				}
				return (Guid?)PropertyAsObject;
			}
			set
			{
				if (value.HasValue)
				{
					EnforceType(EdmType.Guid);
				}
				PropertyAsObject = value;
			}
		}

		public int? Int32Value
		{
			get
			{
				if (!IsNull)
				{
					EnforceType(EdmType.Int32);
				}
				return (int?)PropertyAsObject;
			}
			set
			{
				if (value.HasValue)
				{
					EnforceType(EdmType.Int32);
				}
				PropertyAsObject = value;
			}
		}

		public long? Int64Value
		{
			get
			{
				if (!IsNull)
				{
					EnforceType(EdmType.Int64);
				}
				return (long?)PropertyAsObject;
			}
			set
			{
				if (value.HasValue)
				{
					EnforceType(EdmType.Int64);
				}
				PropertyAsObject = value;
			}
		}

		public string StringValue
		{
			get
			{
				if (!IsNull)
				{
					EnforceType(EdmType.String);
				}
				return (string)PropertyAsObject;
			}
			set
			{
				if (value != null)
				{
					EnforceType(EdmType.String);
				}
				PropertyAsObject = value;
			}
		}

		internal bool IsNull
		{
			get;
			set;
		}

		internal bool IsEncrypted
		{
			get;
			set;
		}

		public static EntityProperty GeneratePropertyForDateTimeOffset(DateTimeOffset? input)
		{
			return new EntityProperty(input);
		}

		public static EntityProperty GeneratePropertyForByteArray(byte[] input)
		{
			return new EntityProperty(input);
		}

		public static EntityProperty GeneratePropertyForBool(bool? input)
		{
			return new EntityProperty(input);
		}

		public static EntityProperty GeneratePropertyForDouble(double? input)
		{
			return new EntityProperty(input);
		}

		public static EntityProperty GeneratePropertyForGuid(Guid? input)
		{
			return new EntityProperty(input);
		}

		public static EntityProperty GeneratePropertyForInt(int? input)
		{
			return new EntityProperty(input);
		}

		public static EntityProperty GeneratePropertyForLong(long? input)
		{
			return new EntityProperty(input);
		}

		public static EntityProperty GeneratePropertyForString(string input)
		{
			return new EntityProperty(input);
		}

		public EntityProperty(byte[] input)
			: this(EdmType.Binary)
		{
			PropertyAsObject = input;
		}

		public EntityProperty(bool? input)
			: this(EdmType.Boolean)
		{
			IsNull = !input.HasValue;
			PropertyAsObject = input;
		}

		public EntityProperty(DateTimeOffset? input)
			: this(EdmType.DateTime)
		{
			if (input.HasValue)
			{
				PropertyAsObject = input.Value.UtcDateTime;
				return;
			}
			IsNull = true;
			PropertyAsObject = null;
		}

		public EntityProperty(DateTime? input)
			: this(EdmType.DateTime)
		{
			IsNull = !input.HasValue;
			PropertyAsObject = input;
		}

		public EntityProperty(double? input)
			: this(EdmType.Double)
		{
			IsNull = !input.HasValue;
			PropertyAsObject = input;
		}

		public EntityProperty(Guid? input)
			: this(EdmType.Guid)
		{
			IsNull = !input.HasValue;
			PropertyAsObject = input;
		}

		public EntityProperty(int? input)
			: this(EdmType.Int32)
		{
			IsNull = !input.HasValue;
			PropertyAsObject = input;
		}

		public EntityProperty(long? input)
			: this(EdmType.Int64)
		{
			IsNull = !input.HasValue;
			PropertyAsObject = input;
		}

		public EntityProperty(string input)
			: this(EdmType.String)
		{
			PropertyAsObject = input;
		}

		private EntityProperty(EdmType propertyType)
		{
			PropertyType = propertyType;
		}

		public override bool Equals(object obj)
		{
			EntityProperty other = obj as EntityProperty;
			return Equals(other);
		}

		public bool Equals(EntityProperty other)
		{
			if (other == null)
			{
				return false;
			}
			if (PropertyType != other.PropertyType)
			{
				return false;
			}
			if (IsNull && other.IsNull)
			{
				return true;
			}
			switch (PropertyType)
			{
			case EdmType.Binary:
				if (BinaryValue.Length == other.BinaryValue.Length)
				{
					return BinaryValue.SequenceEqual(other.BinaryValue);
				}
				return false;
			case EdmType.Boolean:
				return BooleanValue == other.BooleanValue;
			case EdmType.DateTime:
			{
				DateTime? dateTime = DateTime;
				DateTime? dateTime2 = other.DateTime;
				if (dateTime.HasValue != dateTime2.HasValue)
				{
					return false;
				}
				if (!dateTime.HasValue)
				{
					return true;
				}
				return dateTime.GetValueOrDefault() == dateTime2.GetValueOrDefault();
			}
			case EdmType.Double:
				return DoubleValue == other.DoubleValue;
			case EdmType.Guid:
			{
				Guid? guidValue = GuidValue;
				Guid? guidValue2 = other.GuidValue;
				if (guidValue.HasValue != guidValue2.HasValue)
				{
					return false;
				}
				if (!guidValue.HasValue)
				{
					return true;
				}
				return guidValue.GetValueOrDefault() == guidValue2.GetValueOrDefault();
			}
			case EdmType.Int32:
				return Int32Value == other.Int32Value;
			case EdmType.Int64:
				return Int64Value == other.Int64Value;
			case EdmType.String:
				return string.Equals(StringValue, other.StringValue, StringComparison.Ordinal);
			default:
				return PropertyAsObject.Equals(other.PropertyAsObject);
			}
		}

		public override int GetHashCode()
		{
			int num;
			if (PropertyAsObject == null)
			{
				num = 0;
			}
			else if (PropertyType == EdmType.Binary)
			{
				num = 0;
				byte[] array = (byte[])PropertyAsObject;
				if (array.Length != 0)
				{
					for (int i = 0; i < Math.Min(array.Length - 4, 1024); i += 4)
					{
						num ^= BitConverter.ToInt32(array, i);
					}
				}
			}
			else
			{
				num = PropertyAsObject.GetHashCode();
			}
			return num ^ PropertyType.GetHashCode() ^ IsNull.GetHashCode();
		}

		public static EntityProperty CreateEntityPropertyFromObject(object entityValue)
		{
			return CreateEntityPropertyFromObject(entityValue, allowUnknownTypes: true);
		}

		internal static EntityProperty CreateEntityPropertyFromObject(object value, bool allowUnknownTypes)
		{
			if (value is string)
			{
				return new EntityProperty((string)value);
			}
			if (value is byte[])
			{
				return new EntityProperty((byte[])value);
			}
			if (value is bool)
			{
				return new EntityProperty((bool)value);
			}
			if (value is bool?)
			{
				return new EntityProperty((bool?)value);
			}
			if (value is DateTime)
			{
				return new EntityProperty((DateTime)value);
			}
			if (value is DateTime?)
			{
				return new EntityProperty((DateTime?)value);
			}
			if (value is DateTimeOffset)
			{
				return new EntityProperty((DateTimeOffset)value);
			}
			if (value is DateTimeOffset?)
			{
				return new EntityProperty((DateTimeOffset?)value);
			}
			if (value is double)
			{
				return new EntityProperty((double)value);
			}
			if (value is double?)
			{
				return new EntityProperty((double?)value);
			}
			if (value is Guid?)
			{
				return new EntityProperty((Guid?)value);
			}
			if (value is Guid)
			{
				return new EntityProperty((Guid)value);
			}
			if (value is int)
			{
				return new EntityProperty((int)value);
			}
			if (value is int?)
			{
				return new EntityProperty((int?)value);
			}
			if (value is long)
			{
				return new EntityProperty((long)value);
			}
			if (value is long?)
			{
				return new EntityProperty((long?)value);
			}
			if (value == null)
			{
				return new EntityProperty((string)null);
			}
			if (allowUnknownTypes)
			{
				return new EntityProperty(value.ToString());
			}
			return null;
		}

		internal static EntityProperty CreateEntityPropertyFromObject(object value, Type type)
		{
			if (type == typeof(string))
			{
				return new EntityProperty((string)value);
			}
			if (type == typeof(byte[]))
			{
				return new EntityProperty((byte[])value);
			}
			if (type == typeof(bool))
			{
				return new EntityProperty((bool)value);
			}
			if (type == typeof(bool?))
			{
				return new EntityProperty((bool?)value);
			}
			if (type == typeof(DateTime))
			{
				return new EntityProperty((DateTime)value);
			}
			if (type == typeof(DateTime?))
			{
				return new EntityProperty((DateTime?)value);
			}
			if (type == typeof(DateTimeOffset))
			{
				return new EntityProperty((DateTimeOffset)value);
			}
			if (type == typeof(DateTimeOffset?))
			{
				return new EntityProperty((DateTimeOffset?)value);
			}
			if (type == typeof(double))
			{
				return new EntityProperty((double)value);
			}
			if (type == typeof(double?))
			{
				return new EntityProperty((double?)value);
			}
			if (type == typeof(Guid?))
			{
				return new EntityProperty((Guid?)value);
			}
			if (type == typeof(Guid))
			{
				return new EntityProperty((Guid)value);
			}
			if (type == typeof(int))
			{
				return new EntityProperty((int)value);
			}
			if (type == typeof(int?))
			{
				return new EntityProperty((int?)value);
			}
			if (type == typeof(long))
			{
				return new EntityProperty((long)value);
			}
			if (type == typeof(long?))
			{
				return new EntityProperty((long?)value);
			}
			return null;
		}

		internal static EntityProperty CreateEntityPropertyFromObject(object value, EdmType type)
		{
			switch (type)
			{
			case EdmType.String:
				return new EntityProperty((string)value);
			case EdmType.Binary:
				return new EntityProperty(Convert.FromBase64String((string)value));
			case EdmType.Boolean:
				return new EntityProperty(bool.Parse((string)value));
			case EdmType.DateTime:
				return new EntityProperty(DateTimeOffset.Parse((string)value, CultureInfo.InvariantCulture));
			case EdmType.Double:
				return new EntityProperty(double.Parse((string)value, CultureInfo.InvariantCulture));
			case EdmType.Guid:
				return new EntityProperty(Guid.Parse((string)value));
			case EdmType.Int32:
				return new EntityProperty(int.Parse((string)value, CultureInfo.InvariantCulture));
			case EdmType.Int64:
				return new EntityProperty(long.Parse((string)value, CultureInfo.InvariantCulture));
			default:
				return null;
			}
		}

		public override string ToString()
		{
			return GetStringValue(this);
		}

		internal static string GetStringValue(EntityProperty entityProperty)
		{
			object objectValue = GetObjectValue(entityProperty);
			if (objectValue == null)
			{
				return null;
			}
			if (entityProperty.PropertyType == EdmType.Double)
			{
				return entityProperty.DoubleValue.Value.ToString(CultureInfo.InvariantCulture);
			}
			if (entityProperty.PropertyType == EdmType.DateTime)
			{
				return entityProperty.DateTime.Value.ToString("o", CultureInfo.InvariantCulture);
			}
			if (entityProperty.PropertyType == EdmType.Binary)
			{
				return Convert.ToBase64String(entityProperty.BinaryValue);
			}
			return objectValue.ToString();
		}

		internal static object GetObjectValue(EntityProperty entityProperty)
		{
			if (entityProperty.PropertyType == EdmType.String)
			{
				return entityProperty.StringValue;
			}
			if (entityProperty.PropertyType == EdmType.Binary)
			{
				return entityProperty.BinaryValue;
			}
			if (entityProperty.PropertyType == EdmType.Boolean)
			{
				return entityProperty.BooleanValue;
			}
			if (entityProperty.PropertyType == EdmType.DateTime)
			{
				return entityProperty.DateTime;
			}
			if (entityProperty.PropertyType == EdmType.Double)
			{
				return entityProperty.DoubleValue;
			}
			if (entityProperty.PropertyType == EdmType.Guid)
			{
				return entityProperty.GuidValue;
			}
			if (entityProperty.PropertyType == EdmType.Int32)
			{
				return entityProperty.Int32Value;
			}
			if (entityProperty.PropertyType == EdmType.Int64)
			{
				return entityProperty.Int64Value;
			}
			return null;
		}

		private void EnforceType(EdmType requestedType)
		{
			if (PropertyType != requestedType)
			{
				throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Cannot return {0} type for a {1} typed property.", requestedType, PropertyType));
			}
		}
	}
}
