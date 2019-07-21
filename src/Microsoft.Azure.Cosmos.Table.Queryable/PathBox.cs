using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Microsoft.Azure.Cosmos.Table.Queryable
{
	internal class PathBox
	{
		private const char EntireEntityMarker = '*';

		private readonly List<StringBuilder> projectionPaths = new List<StringBuilder>();

		private readonly List<StringBuilder> expandPaths = new List<StringBuilder>();

		private readonly Stack<ParameterExpression> parameterExpressions = new Stack<ParameterExpression>();

		private readonly Dictionary<ParameterExpression, string> basePaths = new Dictionary<ParameterExpression, string>(ReferenceEqualityComparer<ParameterExpression>.Instance);

		internal IEnumerable<string> ProjectionPaths => (from s in projectionPaths
		where s.Length > 0
		select s.ToString()).Distinct();

		internal IEnumerable<string> ExpandPaths => (from s in expandPaths
		where s.Length > 0
		select s.ToString()).Distinct();

		internal ParameterExpression ParamExpressionInScope => parameterExpressions.Peek();

		internal PathBox()
		{
			projectionPaths.Add(new StringBuilder());
		}

		internal void PushParamExpression(ParameterExpression pe)
		{
			StringBuilder stringBuilder = projectionPaths.Last();
			basePaths.Add(pe, stringBuilder.ToString());
			projectionPaths.Remove(stringBuilder);
			parameterExpressions.Push(pe);
		}

		internal void PopParamExpression()
		{
			parameterExpressions.Pop();
		}

		internal void StartNewPath()
		{
			StringBuilder stringBuilder = new StringBuilder(basePaths[ParamExpressionInScope]);
			RemoveEntireEntityMarkerIfPresent(stringBuilder);
			expandPaths.Add(new StringBuilder(stringBuilder.ToString()));
			AddEntireEntityMarker(stringBuilder);
			projectionPaths.Add(stringBuilder);
		}

		internal void AppendToPath(PropertyInfo pi)
		{
			Type elementType = TypeSystem.GetElementType(pi.PropertyType);
			StringBuilder stringBuilder;
			if (CommonUtil.IsClientType(elementType))
			{
				stringBuilder = expandPaths.Last();
				if (stringBuilder.Length > 0)
				{
					stringBuilder.Append('/');
				}
				stringBuilder.Append(pi.Name);
			}
			stringBuilder = projectionPaths.Last();
			RemoveEntireEntityMarkerIfPresent(stringBuilder);
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append('/');
			}
			stringBuilder.Append(pi.Name);
			if (CommonUtil.IsClientType(elementType))
			{
				AddEntireEntityMarker(stringBuilder);
			}
		}

		private static void RemoveEntireEntityMarkerIfPresent(StringBuilder sb)
		{
			if (sb.Length > 0 && sb[sb.Length - 1] == '*')
			{
				sb.Remove(sb.Length - 1, 1);
			}
			if (sb.Length > 0 && sb[sb.Length - 1] == '/')
			{
				sb.Remove(sb.Length - 1, 1);
			}
		}

		private static void AddEntireEntityMarker(StringBuilder sb)
		{
			if (sb.Length > 0)
			{
				sb.Append('/');
			}
			sb.Append('*');
		}
	}
}
