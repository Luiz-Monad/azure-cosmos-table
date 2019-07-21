using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Microsoft.Azure.Cosmos.Table.Queryable
{
	[DebuggerDisplay("OperationContextQueryOptionExpression {operationContext}")]
	internal class OperationContextQueryOptionExpression : QueryOptionExpression
	{
		private ConstantExpression operationContext;

		internal ConstantExpression OperationContext => operationContext;

		internal OperationContextQueryOptionExpression(Type type, ConstantExpression operationContext)
			: base((ExpressionType)10010, type)
		{
			this.operationContext = operationContext;
		}
	}
}
