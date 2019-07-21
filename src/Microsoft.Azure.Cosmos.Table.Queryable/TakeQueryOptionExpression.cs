using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Microsoft.Azure.Cosmos.Table.Queryable
{
	[DebuggerDisplay("TakeQueryOptionExpression {TakeAmount}")]
	internal class TakeQueryOptionExpression : QueryOptionExpression
	{
		private ConstantExpression takeAmount;

		internal ConstantExpression TakeAmount => takeAmount;

		internal TakeQueryOptionExpression(Type type, ConstantExpression takeAmount)
			: base((ExpressionType)10003, type)
		{
			this.takeAmount = takeAmount;
		}

		internal override QueryOptionExpression ComposeMultipleSpecification(QueryOptionExpression previous)
		{
			int num = (int)takeAmount.Value;
			int num2 = (int)((TakeQueryOptionExpression)previous).takeAmount.Value;
			if (num >= num2)
			{
				return previous;
			}
			return this;
		}
	}
}
