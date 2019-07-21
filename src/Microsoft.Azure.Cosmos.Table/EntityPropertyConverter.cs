using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Cosmos.Table
{
	public static class EntityPropertyConverter
	{
		private class ObjectReferenceEqualityComparer : IEqualityComparer<object>
		{
			public new bool Equals(object x, object y)
			{
				return x == y;
			}

			public int GetHashCode(object obj)
			{
				return RuntimeHelpers.GetHashCode(obj);
			}
		}

		public const string DefaultPropertyNameDelimiter = "_";

		public static Dictionary<string, EntityProperty> Flatten(object root, OperationContext operationContext)
		{
			return Flatten(root, new EntityPropertyConverterOptions
			{
				PropertyNameDelimiter = "_"
			}, operationContext);
		}

		public static Dictionary<string, EntityProperty> Flatten(object root, EntityPropertyConverterOptions entityPropertyConverterOptions, OperationContext operationContext)
		{
			if (root == null)
			{
				return null;
			}
			Dictionary<string, EntityProperty> dictionary = new Dictionary<string, EntityProperty>();
			HashSet<object> antecedents = new HashSet<object>(new ObjectReferenceEqualityComparer());
			if (!Flatten(dictionary, root, string.Empty, antecedents, entityPropertyConverterOptions, operationContext))
			{
				return null;
			}
			return dictionary;
		}

		public static T ConvertBack<T>(IDictionary<string, EntityProperty> flattenedEntityProperties, OperationContext operationContext)
		{
			return ConvertBack<T>(flattenedEntityProperties, new EntityPropertyConverterOptions
			{
				PropertyNameDelimiter = "_"
			}, operationContext);
		}

		public static T ConvertBack<T>(IDictionary<string, EntityProperty> flattenedEntityProperties, EntityPropertyConverterOptions entityPropertyConverterOptions, OperationContext operationContext)
		{
			if (flattenedEntityProperties == null)
			{
				return default(T);
			}
			T seed = (T)Activator.CreateInstance(typeof(T));
			return flattenedEntityProperties.Aggregate(seed, (T current, KeyValuePair<string, EntityProperty> kvp) => (T)SetProperty(current, kvp.Key, kvp.Value.PropertyAsObject, entityPropertyConverterOptions, operationContext));
		}

		private static bool Flatten(Dictionary<string, EntityProperty> propertyDictionary, object current, string objectPath, HashSet<object> antecedents, EntityPropertyConverterOptions entityPropertyConverterOptions, OperationContext operationContext)
		{
			if (current == null)
			{
				return true;
			}
			Type type = current.GetType();
			EntityProperty entityProperty = CreateEntityPropertyWithType(current, type);
			if (entityProperty != null)
			{
				propertyDictionary.Add(objectPath, entityProperty);
				return true;
			}
			PropertyInfo[] properties = type.GetProperties();
			if (!properties.Any())
			{
				throw new SerializationException(string.Format(CultureInfo.InvariantCulture, "Unsupported type : {0} encountered during conversion to EntityProperty. Object Path: {1}", type, objectPath));
			}
			bool flag = false;
			if (!type.IsValueType)
			{
				if (antecedents.Contains(current))
				{
					throw new SerializationException(string.Format(CultureInfo.InvariantCulture, "Recursive reference detected. Object Path: {0} Property Type: {1}.", objectPath, type));
				}
				antecedents.Add(current);
				flag = true;
			}
			string propertyNameDelimiter = (entityPropertyConverterOptions != null) ? entityPropertyConverterOptions.PropertyNameDelimiter : "_";
			bool result = (from propertyInfo in properties
			where !ShouldSkip(propertyInfo, objectPath, operationContext)
			select propertyInfo).All(delegate(PropertyInfo propertyInfo)
			{
				if (propertyInfo.Name.Contains(propertyNameDelimiter))
				{
					throw new SerializationException(string.Format(CultureInfo.InvariantCulture, "Property delimiter: {0} exists in property name: {1}. Object Path: {2}", propertyNameDelimiter, propertyInfo.Name, objectPath));
				}
				return Flatten(propertyDictionary, propertyInfo.GetValue(current, null), string.IsNullOrWhiteSpace(objectPath) ? propertyInfo.Name : (objectPath + propertyNameDelimiter + propertyInfo.Name), antecedents, entityPropertyConverterOptions, operationContext);
			});
			if (flag)
			{
				antecedents.Remove(current);
			}
			return result;
		}

		private static EntityProperty CreateEntityPropertyWithType(object value, Type type)
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
			if (type == typeof(uint))
			{
				return new EntityProperty((int)Convert.ToUInt32(value, CultureInfo.InvariantCulture));
			}
			if (type == typeof(uint?))
			{
				return new EntityProperty((int)Convert.ToUInt32(value, CultureInfo.InvariantCulture));
			}
			if (type == typeof(long))
			{
				return new EntityProperty((long)value);
			}
			if (type == typeof(long?))
			{
				return new EntityProperty((long?)value);
			}
			if (type == typeof(ulong))
			{
				return new EntityProperty((long)Convert.ToUInt64(value, CultureInfo.InvariantCulture));
			}
			if (type == typeof(ulong?))
			{
				return new EntityProperty((long)Convert.ToUInt64(value, CultureInfo.InvariantCulture));
			}
			if (type.IsEnum)
			{
				return new EntityProperty(value.ToString());
			}
			if (type == typeof(TimeSpan))
			{
				return new EntityProperty(value.ToString());
			}
			if (type == typeof(TimeSpan?))
			{
				return new EntityProperty(value?.ToString());
			}
			return null;
		}

		private static object SetProperty(object root, string propertyPath, object propertyValue, EntityPropertyConverterOptions entityPropertyConverterOptions, OperationContext operationContext)
		{
			if (root == null)
			{
				throw new ArgumentNullException("root");
			}
			if (propertyPath == null)
			{
				throw new ArgumentNullException("propertyPath");
			}
			try
			{
				string text = (entityPropertyConverterOptions != null) ? entityPropertyConverterOptions.PropertyNameDelimiter : "_";
				Stack<Tuple<object, object, PropertyInfo>> stack = new Stack<Tuple<object, object, PropertyInfo>>();
				string[] array = propertyPath.Split(new string[1]
				{
					text
				}, StringSplitOptions.RemoveEmptyEntries);
				object obj = root;
				bool flag = false;
				for (int i = 0; i < array.Length - 1; i++)
				{
					PropertyInfo property = obj.GetType().GetProperty(array[i]);
					object obj2 = property.GetValue(obj, null);
					Type propertyType = property.PropertyType;
					if (obj2 == null)
					{
						obj2 = Activator.CreateInstance(propertyType);
						property.SetValue(obj, ChangeType(obj2, property.PropertyType), null);
					}
					if (flag || propertyType.IsValueType)
					{
						flag = true;
						stack.Push(new Tuple<object, object, PropertyInfo>(obj2, obj, property));
					}
					obj = obj2;
				}
				PropertyInfo property2 = obj.GetType().GetProperty(array.Last());
				property2.SetValue(obj, ChangeType(propertyValue, property2.PropertyType), null);
				object propertyValue2 = obj;
				while (stack.Count != 0)
				{
					Tuple<object, object, PropertyInfo> tuple = stack.Pop();
					tuple.Item3.SetValue(tuple.Item2, ChangeType(propertyValue2, tuple.Item3.PropertyType), null);
					propertyValue2 = tuple.Item2;
				}
				return root;
			}
			catch (Exception ex)
			{
				Logger.LogError(operationContext, "Exception thrown while trying to set property value. Property Path: {0} Property Value: {1}. Exception Message: {2}", propertyPath, propertyValue, ex.Message);
				throw;
			}
		}

		private static object ChangeType(object propertyValue, Type propertyType)
		{
			Type type = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
			if (type.IsEnum)
			{
				return Enum.Parse(type, propertyValue.ToString());
			}
			if (type == typeof(DateTimeOffset))
			{
				return new DateTimeOffset((DateTime)propertyValue);
			}
			if (type == typeof(TimeSpan))
			{
				return TimeSpan.Parse(propertyValue.ToString(), CultureInfo.InvariantCulture);
			}
			if (type == typeof(uint))
			{
				return (uint)(int)propertyValue;
			}
			if (type == typeof(ulong))
			{
				return (ulong)(long)propertyValue;
			}
			return Convert.ChangeType(propertyValue, type, CultureInfo.InvariantCulture);
		}

		private static bool ShouldSkip(PropertyInfo propertyInfo, string objectPath, OperationContext operationContext)
		{
			if (!propertyInfo.CanWrite)
			{
				Logger.LogInformational(operationContext, "Omitting property: {0} from serialization/de-serialization because the property does not have a setter. The property needs to have at least a private setter. Object Path: {1}", propertyInfo.Name, objectPath);
				return true;
			}
			if (!propertyInfo.CanRead)
			{
				Logger.LogInformational(operationContext, "Omitting property: {0} from serialization/de-serialization because the property does not have a getter. Object path: {1}", propertyInfo.Name, objectPath);
				return true;
			}
			return Attribute.IsDefined(propertyInfo, typeof(IgnorePropertyAttribute));
		}
	}
}
