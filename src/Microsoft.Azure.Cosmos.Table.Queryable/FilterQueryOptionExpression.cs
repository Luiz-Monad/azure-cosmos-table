using System;
using System.Linq.Expressions;

namespace Microsoft.Azure.Cosmos.Table.Queryable
{
	internal class FilterQueryOptionExpression : QueryOptionExpression
	{
		private Expression predicate;

		internal Expression Predicate => predicate;

		internal FilterQueryOptionExpression(Type type, Expression predicate)
			: base((ExpressionType)10006, type)
		{
			this.predicate = predicate;
		}
	}
}
