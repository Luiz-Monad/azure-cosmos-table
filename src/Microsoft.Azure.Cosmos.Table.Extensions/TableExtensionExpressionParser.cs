using Microsoft.Azure.Cosmos.Table.Queryable;

namespace Microsoft.Azure.Cosmos.Table.Extensions
{
	internal sealed class TableExtensionExpressionParser : ExpressionParser
	{
		internal override void VisitQueryOptionExpression(FilterQueryOptionExpression fqoe)
		{
			base.FilterString = TableExtensionExpressionWriter.ExpressionToString(fqoe.Predicate);
		}
	}
}
