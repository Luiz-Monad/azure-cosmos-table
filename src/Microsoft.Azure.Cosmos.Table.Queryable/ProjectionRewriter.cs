using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Azure.Cosmos.Table.Queryable
{
	internal class ProjectionRewriter : ALinqExpressionVisitor
	{
		private readonly ParameterExpression newLambdaParameter;

		private ParameterExpression oldLambdaParameter;

		private bool sucessfulRebind;

		private ProjectionRewriter(Type proposedParameterType)
		{
			newLambdaParameter = Expression.Parameter(proposedParameterType, "it");
		}

		internal static LambdaExpression TryToRewrite(LambdaExpression le, Type proposedParameterType)
		{
			if (!ResourceBinder.PatternRules.MatchSingleArgumentLambda(le, out le) || !le.Parameters[0].Type.GetProperties().Any((PropertyInfo p) => p.PropertyType == proposedParameterType))
			{
				return le;
			}
			return new ProjectionRewriter(proposedParameterType).Rebind(le);
		}

		internal LambdaExpression Rebind(LambdaExpression lambda)
		{
			sucessfulRebind = true;
			oldLambdaParameter = lambda.Parameters[0];
			Expression body = Visit(lambda.Body);
			if (sucessfulRebind)
			{
				return Expression.Lambda(typeof(Func<, >).MakeGenericType(newLambdaParameter.Type, lambda.Body.Type), body, newLambdaParameter);
			}
			throw new NotSupportedException("Can only project the last entity type in the query being translated.");
		}

		internal override Expression VisitMemberAccess(MemberExpression m)
		{
			if (m.Expression == oldLambdaParameter)
			{
				if (m.Type == newLambdaParameter.Type)
				{
					return newLambdaParameter;
				}
				sucessfulRebind = false;
			}
			return base.VisitMemberAccess(m);
		}
	}
}
