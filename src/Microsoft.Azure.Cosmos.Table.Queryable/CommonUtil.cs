using System;
using System.Dynamic;
using System.Linq;

namespace Microsoft.Azure.Cosmos.Table.Queryable
{
	internal static class CommonUtil
	{
		private static readonly Type[] UnsupportedTypes = new Type[9]
		{
			typeof(IDynamicMetaObjectProvider),
			typeof(Tuple<>),
			typeof(Tuple<, >),
			typeof(Tuple<, , >),
			typeof(Tuple<, , , >),
			typeof(Tuple<, , , , >),
			typeof(Tuple<, , , , , >),
			typeof(Tuple<, , , , , , >),
			typeof(Tuple<, , , , , , , >)
		};

		internal static bool IsUnsupportedType(Type type)
		{
			if (type.IsGenericType)
			{
				type = type.GetGenericTypeDefinition();
			}
			if (UnsupportedTypes.Any((Type t) => t.IsAssignableFrom(type)))
			{
				return true;
			}
			return false;
		}

		internal static bool IsClientType(Type t)
		{
			return t.GetInterface(typeof(ITableEntity).FullName, ignoreCase: false) != null;
		}
	}
}
