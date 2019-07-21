using System;
using System.Globalization;

namespace Microsoft.Azure.Cosmos.Table
{
	internal static class EntityPropertyExtensions
	{
		public static bool IsNull(this EntityProperty property)
		{
			switch (property.PropertyType)
			{
			case EdmType.Binary:
				return property.BinaryValue == null;
			case EdmType.Boolean:
				return !property.BooleanValue.HasValue;
			case EdmType.DateTime:
				return !property.DateTime.HasValue;
			case EdmType.Double:
				return !property.DoubleValue.HasValue;
			case EdmType.Guid:
				return !property.GuidValue.HasValue;
			case EdmType.Int32:
				return !property.Int32Value.HasValue;
			case EdmType.Int64:
				return !property.Int64Value.HasValue;
			case EdmType.String:
				return property.StringValue == null;
			default:
				throw new Exception(string.Format(CultureInfo.InvariantCulture, "Unexpected Edm type value {0}", property.PropertyType));
			}
		}
	}
}
