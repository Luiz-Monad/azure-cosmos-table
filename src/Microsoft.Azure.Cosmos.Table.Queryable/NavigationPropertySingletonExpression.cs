using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.Azure.Cosmos.Table.Queryable
{
	internal class NavigationPropertySingletonExpression : ResourceExpression
	{
		private readonly Expression memberExpression;

		private readonly Type resourceType;

		internal MemberExpression MemberExpression => (MemberExpression)memberExpression;

		internal override Type ResourceType => resourceType;

		internal override bool IsSingleton => true;

		internal override bool HasQueryOptions
		{
			get
			{
				if (ExpandPaths.Count <= 0 && CountOption != CountOption.InlineAll && CustomQueryOptions.Count <= 0)
				{
					return base.Projection != null;
				}
				return true;
			}
		}

		internal NavigationPropertySingletonExpression(Type type, Expression source, Expression memberExpression, Type resourceType, List<string> expandPaths, CountOption countOption, Dictionary<ConstantExpression, ConstantExpression> customQueryOptions, ProjectionQueryOptionExpression projection)
			: base(source, (ExpressionType)10002, type, expandPaths, countOption, customQueryOptions, projection)
		{
			this.memberExpression = memberExpression;
			this.resourceType = resourceType;
		}

		internal override ResourceExpression CreateCloneWithNewType(Type type)
		{
			return new NavigationPropertySingletonExpression(type, base.Source, MemberExpression, TypeSystem.GetElementType(type), ExpandPaths.ToList(), CountOption, CustomQueryOptions.ToDictionary((KeyValuePair<ConstantExpression, ConstantExpression> kvp) => kvp.Key, (KeyValuePair<ConstantExpression, ConstantExpression> kvp) => kvp.Value), base.Projection);
		}
	}
}
