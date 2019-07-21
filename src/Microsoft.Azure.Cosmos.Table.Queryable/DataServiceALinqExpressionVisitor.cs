using System.Linq.Expressions;

namespace Microsoft.Azure.Cosmos.Table.Queryable
{
	internal abstract class DataServiceALinqExpressionVisitor : ALinqExpressionVisitor
	{
		internal override Expression Visit(Expression exp)
		{
			if (exp == null)
			{
				return null;
			}
			switch (exp.NodeType)
			{
			case (ExpressionType)10000:
			case (ExpressionType)10001:
				return VisitResourceSetExpression((ResourceSetExpression)exp);
			case (ExpressionType)10002:
				return VisitNavigationPropertySingletonExpression((NavigationPropertySingletonExpression)exp);
			case (ExpressionType)10007:
				return VisitInputReferenceExpression((InputReferenceExpression)exp);
			default:
				return base.Visit(exp);
			}
		}

		internal virtual Expression VisitResourceSetExpression(ResourceSetExpression rse)
		{
			Expression expression = Visit(rse.Source);
			if (expression != rse.Source)
			{
				rse = new ResourceSetExpression(rse.Type, expression, rse.MemberExpression, rse.ResourceType, rse.ExpandPaths, rse.CountOption, rse.CustomQueryOptions, rse.Projection);
			}
			return rse;
		}

		internal virtual Expression VisitNavigationPropertySingletonExpression(NavigationPropertySingletonExpression npse)
		{
			Expression expression = Visit(npse.Source);
			if (expression != npse.Source)
			{
				npse = new NavigationPropertySingletonExpression(npse.Type, expression, npse.MemberExpression, npse.MemberExpression.Type, npse.ExpandPaths, npse.CountOption, npse.CustomQueryOptions, npse.Projection);
			}
			return npse;
		}

		internal virtual Expression VisitInputReferenceExpression(InputReferenceExpression ire)
		{
			return ((ResourceExpression)Visit(ire.Target)).CreateReference();
		}
	}
}
