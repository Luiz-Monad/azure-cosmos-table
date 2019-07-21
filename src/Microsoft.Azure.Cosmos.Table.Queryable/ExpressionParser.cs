using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;

namespace Microsoft.Azure.Cosmos.Table.Queryable
{
	internal class ExpressionParser : DataServiceALinqExpressionVisitor
	{
		private int? takeCount;

		internal TableRequestOptions RequestOptions
		{
			get;
			set;
		}

		internal OperationContext OperationContext
		{
			get;
			set;
		}

		internal ConstantExpression Resolver
		{
			get;
			set;
		}

		internal ProjectionQueryOptionExpression Projection
		{
			get;
			set;
		}

		public int? TakeCount
		{
			get
			{
				return takeCount;
			}
			set
			{
				if (value.HasValue && value.Value <= 0)
				{
					throw new ArgumentException("Take count must be positive and greater than 0.");
				}
				takeCount = value;
			}
		}

		public string FilterString
		{
			get;
			set;
		}

		public IList<string> SelectColumns
		{
			get;
			set;
		}

		internal ExpressionParser()
		{
			SelectColumns = new List<string>();
		}

		internal void Translate(Expression e)
		{
			Visit(e);
		}

		internal override Expression VisitMethodCall(MethodCallExpression m)
		{
			throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "The method '{0}' is not supported.", m.Method.Name));
		}

		internal override Expression VisitUnary(UnaryExpression u)
		{
			throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "The unary operator '{0}' is not supported.", u.NodeType.ToString()));
		}

		internal override Expression VisitBinary(BinaryExpression b)
		{
			throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "The binary operator '{0}' is not supported.", b.NodeType.ToString()));
		}

		internal override Expression VisitConstant(ConstantExpression c)
		{
			throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "The constant for '{0}' is not supported.", c.Value));
		}

		internal override Expression VisitTypeIs(TypeBinaryExpression b)
		{
			throw new NotSupportedException("An operation between an expression and a type is not supported.");
		}

		internal override Expression VisitConditional(ConditionalExpression c)
		{
			throw new NotSupportedException("The conditional expression is not supported.");
		}

		internal override Expression VisitParameter(ParameterExpression p)
		{
			throw new NotSupportedException("The parameter expression is not supported.");
		}

		internal override Expression VisitMemberAccess(MemberExpression m)
		{
			throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "The member access of '{0}' is not supported.", m.Member.Name));
		}

		internal override Expression VisitLambda(LambdaExpression lambda)
		{
			throw new NotSupportedException("Lambda Expressions not supported.");
		}

		internal override NewExpression VisitNew(NewExpression nex)
		{
			throw new NotSupportedException("New Expressions not supported.");
		}

		internal override Expression VisitMemberInit(MemberInitExpression init)
		{
			throw new NotSupportedException("Member Init Expressions not supported.");
		}

		internal override Expression VisitListInit(ListInitExpression init)
		{
			throw new NotSupportedException("List Init Expressions not supported.");
		}

		internal override Expression VisitNewArray(NewArrayExpression na)
		{
			throw new NotSupportedException("New Array Expressions not supported.");
		}

		internal override Expression VisitInvocation(InvocationExpression iv)
		{
			throw new NotSupportedException("Invocation Expressions not supported.");
		}

		internal override Expression VisitNavigationPropertySingletonExpression(NavigationPropertySingletonExpression npse)
		{
			throw new NotSupportedException("Navigation not supported.");
		}

		internal override Expression VisitResourceSetExpression(ResourceSetExpression rse)
		{
			VisitQueryOptions(rse);
			return rse;
		}

		internal void VisitQueryOptions(ResourceExpression re)
		{
			if (!re.HasQueryOptions)
			{
				return;
			}
			ResourceSetExpression resourceSetExpression = re as ResourceSetExpression;
			if (resourceSetExpression != null)
			{
				IEnumerator enumerator = resourceSetExpression.SequenceQueryOptions.GetEnumerator();
				while (enumerator.MoveNext())
				{
					Expression expression = (Expression)enumerator.Current;
					switch (expression.NodeType)
					{
					case (ExpressionType)10009:
						VisitQueryOptionExpression((RequestOptionsQueryOptionExpression)expression);
						break;
					case (ExpressionType)10010:
						VisitQueryOptionExpression((OperationContextQueryOptionExpression)expression);
						break;
					case (ExpressionType)10011:
						VisitQueryOptionExpression((EntityResolverQueryOptionExpression)expression);
						break;
					case (ExpressionType)10003:
						VisitQueryOptionExpression((TakeQueryOptionExpression)expression);
						break;
					case (ExpressionType)10006:
						VisitQueryOptionExpression((FilterQueryOptionExpression)expression);
						break;
					}
				}
			}
			if (re.Projection != null && re.Projection.Paths.Count > 0)
			{
				Projection = re.Projection;
				SelectColumns = re.Projection.Paths;
			}
			if (re.CustomQueryOptions.Count > 0)
			{
				VisitCustomQueryOptions(re.CustomQueryOptions);
			}
		}

		internal virtual void VisitQueryOptionExpression(RequestOptionsQueryOptionExpression roqoe)
		{
			RequestOptions = (TableRequestOptions)roqoe.RequestOptions.Value;
		}

		internal virtual void VisitQueryOptionExpression(OperationContextQueryOptionExpression ocqoe)
		{
			OperationContext = (OperationContext)ocqoe.OperationContext.Value;
		}

		internal virtual void VisitQueryOptionExpression(EntityResolverQueryOptionExpression erqoe)
		{
			Resolver = erqoe.Resolver;
		}

		internal virtual void VisitQueryOptionExpression(TakeQueryOptionExpression tqoe)
		{
			TakeCount = (int)tqoe.TakeAmount.Value;
		}

		internal virtual void VisitQueryOptionExpression(FilterQueryOptionExpression fqoe)
		{
			FilterString = ExpressionToString(fqoe.Predicate);
		}

		internal void VisitCustomQueryOptions(Dictionary<ConstantExpression, ConstantExpression> options)
		{
			throw new NotSupportedException();
		}

		private static string ExpressionToString(Expression expression)
		{
			return ExpressionWriter.ExpressionToString(expression);
		}
	}
}
