using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Microsoft.Azure.Cosmos.Table.Queryable
{
	[DebuggerDisplay("RequestOptionsQueryOptionExpression {requestOptions}")]
	internal class RequestOptionsQueryOptionExpression : QueryOptionExpression
	{
		private ConstantExpression requestOptions;

		internal ConstantExpression RequestOptions => requestOptions;

		internal RequestOptionsQueryOptionExpression(Type type, ConstantExpression requestOptions)
			: base((ExpressionType)10009, type)
		{
			this.requestOptions = requestOptions;
		}
	}
}
