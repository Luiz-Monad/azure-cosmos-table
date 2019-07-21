using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Microsoft.Azure.Cosmos.Table.Queryable
{
	[DebuggerDisplay("EntityResolverQueryOptionExpression {requestOptions}")]
	internal class EntityResolverQueryOptionExpression : QueryOptionExpression
	{
		private ConstantExpression resolver;

		internal ConstantExpression Resolver => resolver;

		internal EntityResolverQueryOptionExpression(Type type, ConstantExpression resolver)
			: base((ExpressionType)10011, type)
		{
			this.resolver = resolver;
		}
	}
}
