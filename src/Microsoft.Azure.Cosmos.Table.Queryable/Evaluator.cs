using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.Azure.Cosmos.Table.Queryable
{
	internal static class Evaluator
	{
		internal class SubtreeEvaluator : DataServiceALinqExpressionVisitor
		{
			private HashSet<Expression> candidates;

			internal SubtreeEvaluator(HashSet<Expression> candidates)
			{
				this.candidates = candidates;
			}

			internal Expression Eval(Expression exp)
			{
				return Visit(exp);
			}

			internal override Expression Visit(Expression exp)
			{
				if (exp == null)
				{
					return null;
				}
				if (candidates.Contains(exp))
				{
					return Evaluate(exp);
				}
				return base.Visit(exp);
			}

			private static Expression Evaluate(Expression e)
			{
				if (e.NodeType == ExpressionType.Constant)
				{
					return e;
				}
				object obj = Expression.Lambda(e).Compile().DynamicInvoke(null);
				Type type = e.Type;
				if (obj != null && type.IsArray && type.GetElementType() == obj.GetType().GetElementType())
				{
					type = obj.GetType();
				}
				return Expression.Constant(obj, type);
			}
		}

		internal class Nominator : DataServiceALinqExpressionVisitor
		{
			private Func<Expression, bool> functionCanBeEvaluated;

			private HashSet<Expression> candidates;

			private bool cannotBeEvaluated;

			internal Nominator(Func<Expression, bool> functionCanBeEvaluated)
			{
				this.functionCanBeEvaluated = functionCanBeEvaluated;
			}

			internal HashSet<Expression> Nominate(Expression expression)
			{
				candidates = new HashSet<Expression>(EqualityComparer<Expression>.Default);
				Visit(expression);
				return candidates;
			}

			internal override Expression Visit(Expression expression)
			{
				if (expression != null)
				{
					bool flag = cannotBeEvaluated;
					cannotBeEvaluated = false;
					base.Visit(expression);
					if (!cannotBeEvaluated)
					{
						if (functionCanBeEvaluated(expression))
						{
							candidates.Add(expression);
						}
						else
						{
							cannotBeEvaluated = true;
						}
					}
					cannotBeEvaluated |= flag;
				}
				return expression;
			}
		}

		internal static Expression PartialEval(Expression expression, Func<Expression, bool> canBeEvaluated)
		{
			return new SubtreeEvaluator(new Nominator(canBeEvaluated).Nominate(expression)).Eval(expression);
		}

		internal static Expression PartialEval(Expression expression)
		{
			return PartialEval(expression, CanBeEvaluatedLocally);
		}

		private static bool CanBeEvaluatedLocally(Expression expression)
		{
			if (expression.NodeType != ExpressionType.Parameter && expression.NodeType != ExpressionType.Lambda)
			{
				return expression.NodeType != (ExpressionType)10000;
			}
			return false;
		}
	}
}
