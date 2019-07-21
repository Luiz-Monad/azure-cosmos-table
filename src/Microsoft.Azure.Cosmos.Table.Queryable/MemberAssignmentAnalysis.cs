using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.Azure.Cosmos.Table.Queryable
{
	internal class MemberAssignmentAnalysis : ALinqExpressionVisitor
	{
		internal static readonly Expression[] EmptyExpressionArray = new Expression[0];

		private readonly Expression entity;

		private Exception incompatibleAssignmentsException;

		private bool multiplePathsFound;

		private List<Expression> pathFromEntity;

		internal Exception IncompatibleAssignmentsException => incompatibleAssignmentsException;

		internal bool MultiplePathsFound => multiplePathsFound;

		private MemberAssignmentAnalysis(Expression entity)
		{
			this.entity = entity;
			pathFromEntity = new List<Expression>();
		}

		internal static MemberAssignmentAnalysis Analyze(Expression entityInScope, Expression assignmentExpression)
		{
			MemberAssignmentAnalysis memberAssignmentAnalysis = new MemberAssignmentAnalysis(entityInScope);
			memberAssignmentAnalysis.Visit(assignmentExpression);
			return memberAssignmentAnalysis;
		}

		internal Exception CheckCompatibleAssignments(Type targetType, ref MemberAssignmentAnalysis previous)
		{
			if (previous == null)
			{
				previous = this;
				return null;
			}
			Expression[] expressionsToTargetEntity = previous.GetExpressionsToTargetEntity();
			Expression[] expressionsToTargetEntity2 = GetExpressionsToTargetEntity();
			return CheckCompatibleAssignments(targetType, expressionsToTargetEntity, expressionsToTargetEntity2);
		}

		internal override Expression Visit(Expression expression)
		{
			if (multiplePathsFound || incompatibleAssignmentsException != null)
			{
				return expression;
			}
			return base.Visit(expression);
		}

		internal override Expression VisitConditional(ConditionalExpression c)
		{
			ResourceBinder.PatternRules.MatchNullCheckResult matchNullCheckResult = ResourceBinder.PatternRules.MatchNullCheck(entity, c);
			if (matchNullCheckResult.Match)
			{
				Visit(matchNullCheckResult.AssignExpression);
				return c;
			}
			return base.VisitConditional(c);
		}

		internal override Expression VisitParameter(ParameterExpression p)
		{
			if (p == entity)
			{
				if (pathFromEntity.Count != 0)
				{
					multiplePathsFound = true;
				}
				else
				{
					pathFromEntity.Add(p);
				}
			}
			return p;
		}

		internal override Expression VisitMemberInit(MemberInitExpression init)
		{
			MemberAssignmentAnalysis previous = null;
			foreach (MemberBinding binding in init.Bindings)
			{
				MemberAssignment memberAssignment = binding as MemberAssignment;
				if (memberAssignment != null)
				{
					MemberAssignmentAnalysis memberAssignmentAnalysis = Analyze(entity, memberAssignment.Expression);
					if (memberAssignmentAnalysis.MultiplePathsFound)
					{
						multiplePathsFound = true;
						return init;
					}
					Exception ex = memberAssignmentAnalysis.CheckCompatibleAssignments(init.Type, ref previous);
					if (ex != null)
					{
						incompatibleAssignmentsException = ex;
						return init;
					}
					if (pathFromEntity.Count == 0)
					{
						pathFromEntity.AddRange(memberAssignmentAnalysis.GetExpressionsToTargetEntity());
					}
				}
			}
			return init;
		}

		internal override Expression VisitMemberAccess(MemberExpression m)
		{
			Expression result = base.VisitMemberAccess(m);
			if (pathFromEntity.Contains(m.Expression))
			{
				pathFromEntity.Add(m);
			}
			return result;
		}

		internal override Expression VisitMethodCall(MethodCallExpression call)
		{
			if (ReflectionUtil.IsSequenceMethod(call.Method, SequenceMethod.Select))
			{
				Visit(call.Arguments[0]);
				return call;
			}
			return base.VisitMethodCall(call);
		}

		internal Expression[] GetExpressionsBeyondTargetEntity()
		{
			if (pathFromEntity.Count <= 1)
			{
				return EmptyExpressionArray;
			}
			return new Expression[1]
			{
				pathFromEntity[pathFromEntity.Count - 1]
			};
		}

		internal Expression[] GetExpressionsToTargetEntity()
		{
			if (pathFromEntity.Count <= 1)
			{
				return EmptyExpressionArray;
			}
			Expression[] array = new Expression[pathFromEntity.Count - 1];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = pathFromEntity[i];
			}
			return array;
		}

		private static Exception CheckCompatibleAssignments(Type targetType, Expression[] previous, Expression[] candidate)
		{
			if (previous.Length != candidate.Length)
			{
				throw CheckCompatibleAssignmentsFail(targetType, previous, candidate);
			}
			for (int i = 0; i < previous.Length; i++)
			{
				Expression expression = previous[i];
				Expression expression2 = candidate[i];
				if (expression.NodeType != expression2.NodeType)
				{
					throw CheckCompatibleAssignmentsFail(targetType, previous, candidate);
				}
				if (expression != expression2)
				{
					if (expression.NodeType != ExpressionType.MemberAccess)
					{
						return CheckCompatibleAssignmentsFail(targetType, previous, candidate);
					}
					if (((MemberExpression)expression).Member.Name != ((MemberExpression)expression2).Member.Name)
					{
						return CheckCompatibleAssignmentsFail(targetType, previous, candidate);
					}
				}
			}
			return null;
		}

		private static Exception CheckCompatibleAssignmentsFail(Type targetType, Expression[] previous, Expression[] candidate)
		{
			return new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "Cannot initialize an instance of entity type '{0}' because '{1}' and '{2}' do not refer to the same source entity.", targetType.FullName, previous.LastOrDefault(), candidate.LastOrDefault()));
		}
	}
}
