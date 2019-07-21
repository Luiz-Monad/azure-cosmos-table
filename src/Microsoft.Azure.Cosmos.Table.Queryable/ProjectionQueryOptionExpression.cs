using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.Azure.Cosmos.Table.Queryable
{
	internal class ProjectionQueryOptionExpression : QueryOptionExpression
	{
		private readonly LambdaExpression lambda;

		private readonly List<string> paths;

		internal static readonly LambdaExpression DefaultLambda = Expression.Lambda((Expression)Expression.Constant(0), (ParameterExpression[])null);

		internal LambdaExpression Selector => lambda;

		internal List<string> Paths => paths;

		internal ProjectionQueryOptionExpression(Type type, LambdaExpression lambda, List<string> paths)
			: base((ExpressionType)10008, type)
		{
			this.lambda = lambda;
			this.paths = paths;
		}
	}
}
