using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Azure.Cosmos.Table
{
	public class TableEntity : ITableEntity
	{
		private static ConcurrentDictionary<Type, Dictionary<string, EdmType>> propertyResolverCache = new ConcurrentDictionary<Type, Dictionary<string, EdmType>>();

		private static bool disablePropertyResolverCache = false;

		public string PartitionKey
		{
			get;
			set;
		}

		public string RowKey
		{
			get;
			set;
		}

		public DateTimeOffset Timestamp
		{
			get;
			set;
		}

		public string ETag
		{
			get;
			set;
		}

		internal static ConcurrentDictionary<Type, Dictionary<string, EdmType>> PropertyResolverCache
		{
			get
			{
				return propertyResolverCache;
			}
			set
			{
				propertyResolverCache = value;
			}
		}

		public static bool DisablePropertyResolverCache
		{
			get
			{
				return disablePropertyResolverCache;
			}
			set
			{
				if (value)
				{
					propertyResolverCache.Clear();
				}
				disablePropertyResolverCache = value;
			}
		}

		public TableEntity()
		{
		}

		public TableEntity(string partitionKey, string rowKey)
		{
			PartitionKey = partitionKey;
			RowKey = rowKey;
		}

		public virtual void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
		{
			ReflectionRead(this, properties, operationContext);
		}

		public static void ReadUserObject(object entity, IDictionary<string, EntityProperty> properties, OperationContext operationContext)
		{
			CommonUtility.AssertNotNull("entity", entity);
			ReflectionRead(entity, properties, operationContext);
		}

		public static TResult ConvertBack<TResult>(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
		{
			return EntityPropertyConverter.ConvertBack<TResult>(properties, operationContext);
		}

		public static TResult ConvertBack<TResult>(IDictionary<string, EntityProperty> properties, EntityPropertyConverterOptions entityPropertyConverterOptions, OperationContext operationContext)
		{
			return EntityPropertyConverter.ConvertBack<TResult>(properties, entityPropertyConverterOptions, operationContext);
		}

		private static void ReflectionRead(object entity, IDictionary<string, EntityProperty> properties, OperationContext operationContext)
		{
			foreach (PropertyInfo item in (IEnumerable<PropertyInfo>)entity.GetType().GetProperties())
			{
				if (!ShouldSkipProperty(item, operationContext))
				{
					if (!properties.ContainsKey(item.Name))
					{
						Logger.LogInformational(operationContext, "Omitting property '{0}' from de-serialization because there is no corresponding entry in the dictionary provided.", item.Name);
					}
					else
					{
						EntityProperty entityProperty = properties[item.Name];
						if (entityProperty.IsNull)
						{
							item.SetValue(entity, null, null);
						}
						else
						{
							switch (entityProperty.PropertyType)
							{
							case EdmType.String:
								if (!(item.PropertyType != typeof(string)))
								{
									item.SetValue(entity, entityProperty.StringValue, null);
								}
								break;
							case EdmType.Binary:
								if (!(item.PropertyType != typeof(byte[])))
								{
									item.SetValue(entity, entityProperty.BinaryValue, null);
								}
								break;
							case EdmType.Boolean:
								if (!(item.PropertyType != typeof(bool)) || !(item.PropertyType != typeof(bool?)))
								{
									item.SetValue(entity, entityProperty.BooleanValue, null);
								}
								break;
							case EdmType.DateTime:
								if (item.PropertyType == typeof(DateTime))
								{
									item.SetValue(entity, entityProperty.DateTimeOffsetValue.Value.UtcDateTime, null);
								}
								else if (item.PropertyType == typeof(DateTime?))
								{
									item.SetValue(entity, entityProperty.DateTimeOffsetValue.HasValue ? new DateTime?(entityProperty.DateTimeOffsetValue.Value.UtcDateTime) : null, null);
								}
								else if (item.PropertyType == typeof(DateTimeOffset))
								{
									item.SetValue(entity, entityProperty.DateTimeOffsetValue.Value, null);
								}
								else if (item.PropertyType == typeof(DateTimeOffset?))
								{
									item.SetValue(entity, entityProperty.DateTimeOffsetValue, null);
								}
								break;
							case EdmType.Double:
								if (!(item.PropertyType != typeof(double)) || !(item.PropertyType != typeof(double?)))
								{
									item.SetValue(entity, entityProperty.DoubleValue, null);
								}
								break;
							case EdmType.Guid:
								if (!(item.PropertyType != typeof(Guid)) || !(item.PropertyType != typeof(Guid?)))
								{
									item.SetValue(entity, entityProperty.GuidValue, null);
								}
								break;
							case EdmType.Int32:
								if (!(item.PropertyType != typeof(int)) || !(item.PropertyType != typeof(int?)))
								{
									item.SetValue(entity, entityProperty.Int32Value, null);
								}
								break;
							case EdmType.Int64:
								if (!(item.PropertyType != typeof(long)) || !(item.PropertyType != typeof(long?)))
								{
									item.SetValue(entity, entityProperty.Int64Value, null);
								}
								break;
							}
						}
					}
				}
			}
		}

		public virtual IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
		{
			return ReflectionWrite(this, operationContext);
		}

		public static IDictionary<string, EntityProperty> WriteUserObject(object entity, OperationContext operationContext)
		{
			CommonUtility.AssertNotNull("entity", entity);
			return ReflectionWrite(entity, operationContext);
		}

		public static IDictionary<string, EntityProperty> Flatten(object entity, OperationContext operationContext)
		{
			CommonUtility.AssertNotNull("entity", entity);
			return EntityPropertyConverter.Flatten(entity, operationContext);
		}

		public static IDictionary<string, EntityProperty> Flatten(object entity, EntityPropertyConverterOptions entityPropertyConverterOptions, OperationContext operationContext)
		{
			CommonUtility.AssertNotNull("entity", entity);
			return EntityPropertyConverter.Flatten(entity, entityPropertyConverterOptions, operationContext);
		}

		private static IDictionary<string, EntityProperty> ReflectionWrite(object entity, OperationContext operationContext)
		{
			Dictionary<string, EntityProperty> dictionary = new Dictionary<string, EntityProperty>();
			foreach (PropertyInfo item in (IEnumerable<PropertyInfo>)entity.GetType().GetProperties())
			{
				if (!ShouldSkipProperty(item, operationContext))
				{
					EntityProperty entityProperty = EntityProperty.CreateEntityPropertyFromObject(item.GetValue(entity, null), item.PropertyType);
					if (entityProperty != null)
					{
						dictionary.Add(item.Name, entityProperty);
					}
				}
			}
			return dictionary;
		}

		internal static bool ShouldSkipProperty(PropertyInfo property, OperationContext operationContext)
		{
			switch (property.Name)
			{
			case "PartitionKey":
			case "RowKey":
			case "Timestamp":
			case "ETag":
				return true;
			default:
			{
				MethodInfo setMethod = property.SetMethod;
				MethodInfo getMethod = property.GetMethod;
				if (setMethod == null || !setMethod.IsPublic || getMethod == null || !getMethod.IsPublic)
				{
					Logger.LogInformational(operationContext, "Omitting property '{0}' from serialization/de-serialization because the property's getter/setter are not public.", property.Name);
					return true;
				}
				if (setMethod.IsStatic)
				{
					return true;
				}
				if (Attribute.IsDefined(property, typeof(IgnorePropertyAttribute)))
				{
					Logger.LogInformational(operationContext, "Omitting property '{0}' from serialization/de-serialization because IgnoreAttribute has been set on that property.", property.Name);
					return true;
				}
				return false;
			}
			}
		}
	}
}
