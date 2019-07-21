using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Azure.Cosmos.Table.Queryable
{
	internal static class TypeSystem
	{
		private static readonly Dictionary<MethodInfo, string> StaticExpressionMethodMap;

		private static readonly Dictionary<string, string> StaticExpressionVBMethodMap;

		private static readonly Dictionary<PropertyInfo, MethodInfo> StaticPropertiesAsMethodsMap;

		private const string VisualBasicAssemblyFullName = "Microsoft.VisualBasic, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";

		static TypeSystem()
		{
			StaticExpressionMethodMap = new Dictionary<MethodInfo, string>(EqualityComparer<MethodInfo>.Default);
			StaticExpressionVBMethodMap = new Dictionary<string, string>(EqualityComparer<string>.Default);
			StaticPropertiesAsMethodsMap = new Dictionary<PropertyInfo, MethodInfo>(EqualityComparer<PropertyInfo>.Default);
		}

		internal static bool TryGetQueryOptionMethod(MethodInfo mi, out string methodName)
		{
			if (!StaticExpressionMethodMap.TryGetValue(mi, out methodName))
			{
				if (mi.DeclaringType.Assembly.FullName == "Microsoft.VisualBasic, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")
				{
					return StaticExpressionVBMethodMap.TryGetValue(mi.DeclaringType.FullName + "." + mi.Name, out methodName);
				}
				return false;
			}
			return true;
		}

		internal static bool TryGetPropertyAsMethod(PropertyInfo pi, out MethodInfo mi)
		{
			return StaticPropertiesAsMethodsMap.TryGetValue(pi, out mi);
		}

		internal static Type GetElementType(Type seqType)
		{
			Type type = FindIEnumerable(seqType);
			if (type == null)
			{
				return seqType;
			}
			return type.GetGenericArguments()[0];
		}

		internal static bool IsPrivate(PropertyInfo pi)
		{
			MethodInfo methodInfo = pi.GetGetMethod() ?? pi.GetSetMethod();
			if (methodInfo != null)
			{
				return methodInfo.IsPrivate;
			}
			return true;
		}

		internal static Type FindIEnumerable(Type seqType)
		{
			if (seqType == null || seqType == typeof(string))
			{
				return null;
			}
			if (seqType.IsArray)
			{
				return typeof(IEnumerable<>).MakeGenericType(seqType.GetElementType());
			}
			if (seqType.IsGenericType)
			{
				Type[] genericArguments = seqType.GetGenericArguments();
				foreach (Type type in genericArguments)
				{
					Type type2 = typeof(IEnumerable<>).MakeGenericType(type);
					if (type2.IsAssignableFrom(seqType))
					{
						return type2;
					}
				}
			}
			Type[] interfaces = seqType.GetInterfaces();
			if (interfaces != null && interfaces.Length != 0)
			{
				Type[] genericArguments = interfaces;
				for (int i = 0; i < genericArguments.Length; i++)
				{
					Type type3 = FindIEnumerable(genericArguments[i]);
					if (type3 != null)
					{
						return type3;
					}
				}
			}
			if (seqType.BaseType != null && seqType.BaseType != typeof(object))
			{
				return FindIEnumerable(seqType.BaseType);
			}
			return null;
		}
	}
}
