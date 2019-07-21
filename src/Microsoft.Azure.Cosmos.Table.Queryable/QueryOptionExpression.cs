using System;
using System.Linq.Expressions;

namespace Microsoft.Azure.Cosmos.Table.Queryable
{
	internal abstract class QueryOptionExpression : Expression
	{
        #pragma warning disable 612, 618
		internal QueryOptionExpression(ExpressionType nodeType, Type type)
			: base(nodeType, type)
		{
		}
        #pragma warning restore 612, 618

		internal virtual QueryOptionExpression ComposeMultipleSpecification(QueryOptionExpression previous)
		{
			return this;
		}
	}
}
