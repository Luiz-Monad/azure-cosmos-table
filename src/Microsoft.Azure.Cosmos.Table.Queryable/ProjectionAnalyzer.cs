using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Azure.Cosmos.Table.Queryable
{
	internal static class ProjectionAnalyzer
	{
		private class EntityProjectionAnalyzer : ALinqExpressionVisitor
		{
			private readonly PathBox box;

			private readonly Type type;

			private EntityProjectionAnalyzer(PathBox pb, Type type)
			{
				box = pb;
				this.type = type;
			}

			internal static void Analyze(MemberInitExpression mie, PathBox pb)
			{
				EntityProjectionAnalyzer entityProjectionAnalyzer = new EntityProjectionAnalyzer(pb, mie.Type);
				MemberAssignmentAnalysis previous = null;
				foreach (MemberBinding binding in mie.Bindings)
				{
					MemberAssignment memberAssignment = binding as MemberAssignment;
					entityProjectionAnalyzer.Visit(memberAssignment.Expression);
					if (memberAssignment != null)
					{
						MemberAssignmentAnalysis memberAssignmentAnalysis = MemberAssignmentAnalysis.Analyze(pb.ParamExpressionInScope, memberAssignment.Expression);
						if (memberAssignmentAnalysis.IncompatibleAssignmentsException != null)
						{
							throw memberAssignmentAnalysis.IncompatibleAssignmentsException;
						}
						Type memberType = GetMemberType(memberAssignment.Member);
						Expression[] expressionsBeyondTargetEntity = memberAssignmentAnalysis.GetExpressionsBeyondTargetEntity();
						if (expressionsBeyondTargetEntity.Length == 0)
						{
							throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "Initializing instances of the entity type {0} with the expression {1} is not supported.", memberType, memberAssignment.Expression));
						}
						MemberExpression memberExpression = expressionsBeyondTargetEntity[expressionsBeyondTargetEntity.Length - 1] as MemberExpression;
						if (memberExpression != null && memberExpression.Member.Name != memberAssignment.Member.Name)
						{
							throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "Cannot assign the value from the {0} property to the {1} property.  When projecting results into a entity type, the property names of the source type and the target type must match for the properties being projected.", memberExpression.Member.Name, memberAssignment.Member.Name));
						}
						memberAssignmentAnalysis.CheckCompatibleAssignments(mie.Type, ref previous);
						bool flag = CommonUtil.IsClientType(memberType);
						if (CommonUtil.IsClientType(memberExpression.Type) && !flag)
						{
							throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "Constructing or initializing instances of the type {0} with the expression {1} is not supported.", memberType, memberAssignment.Expression));
						}
					}
				}
			}

			internal override Expression VisitUnary(UnaryExpression u)
			{
				if (ResourceBinder.PatternRules.MatchConvertToAssignable(u))
				{
					return base.VisitUnary(u);
				}
				if (u.NodeType == ExpressionType.Convert || u.NodeType == ExpressionType.ConvertChecked)
				{
					Type obj = Nullable.GetUnderlyingType(u.Operand.Type) ?? u.Operand.Type;
					Type type = Nullable.GetUnderlyingType(u.Type) ?? u.Type;
					if (ClientConvert.IsKnownType(obj) && ClientConvert.IsKnownType(type))
					{
						return base.Visit(u.Operand);
					}
				}
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "Initializing instances of the entity type {0} with the expression {1} is not supported.", this.type, u.ToString()));
			}

			internal override Expression VisitBinary(BinaryExpression b)
			{
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "Initializing instances of the entity type {0} with the expression {1} is not supported.", type, b.ToString()));
			}

			internal override Expression VisitTypeIs(TypeBinaryExpression b)
			{
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "Initializing instances of the entity type {0} with the expression {1} is not supported.", type, b.ToString()));
			}

			internal override Expression VisitConditional(ConditionalExpression c)
			{
				ResourceBinder.PatternRules.MatchNullCheckResult matchNullCheckResult = ResourceBinder.PatternRules.MatchNullCheck(box.ParamExpressionInScope, c);
				if (matchNullCheckResult.Match)
				{
					Visit(matchNullCheckResult.AssignExpression);
					return c;
				}
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "Initializing instances of the entity type {0} with the expression {1} is not supported.", type, c.ToString()));
			}

			internal override Expression VisitConstant(ConstantExpression c)
			{
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "Initializing instances of the entity type {0} with the expression {1} is not supported.", type, c.ToString()));
			}

			internal override Expression VisitMemberAccess(MemberExpression m)
			{
				if (!CommonUtil.IsClientType(m.Expression.Type))
				{
					throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "Initializing instances of the entity type {0} with the expression {1} is not supported.", type, m.ToString()));
				}
				PropertyInfo propInfo = null;
				if (ResourceBinder.PatternRules.MatchNonPrivateReadableProperty(m, out propInfo))
				{
					Expression result = base.VisitMemberAccess(m);
					box.AppendToPath(propInfo);
					return result;
				}
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "Initializing instances of the entity type {0} with the expression {1} is not supported.", type, m.ToString()));
			}

			internal override Expression VisitMethodCall(MethodCallExpression m)
			{
				if (IsMethodCallAllowedEntitySequence(m))
				{
					CheckChainedSequence(m, type);
					return base.VisitMethodCall(m);
				}
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "Initializing instances of the entity type {0} with the expression {1} is not supported.", type, m.ToString()));
			}

			internal override Expression VisitInvocation(InvocationExpression iv)
			{
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "Initializing instances of the entity type {0} with the expression {1} is not supported.", type, iv.ToString()));
			}

			internal override Expression VisitLambda(LambdaExpression lambda)
			{
				ProjectionAnalyzer.Analyze(lambda, box);
				return lambda;
			}

			internal override Expression VisitListInit(ListInitExpression init)
			{
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "Initializing instances of the entity type {0} with the expression {1} is not supported.", type, init.ToString()));
			}

			internal override Expression VisitNewArray(NewArrayExpression na)
			{
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "Initializing instances of the entity type {0} with the expression {1} is not supported.", type, na.ToString()));
			}

			internal override Expression VisitMemberInit(MemberInitExpression init)
			{
				ProjectionAnalyzer.Analyze(init, box);
				return init;
			}

			internal override NewExpression VisitNew(NewExpression nex)
			{
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "Initializing instances of the entity type {0} with the expression {1} is not supported.", type, nex.ToString()));
			}

			internal override Expression VisitParameter(ParameterExpression p)
			{
				if (p != box.ParamExpressionInScope)
				{
					throw new NotSupportedException("Can only project the last entity type in the query being translated.");
				}
				box.StartNewPath();
				return p;
			}

			private static Type GetMemberType(MemberInfo member)
			{
				PropertyInfo propertyInfo = member as PropertyInfo;
				if (propertyInfo != null)
				{
					return propertyInfo.PropertyType;
				}
				return (member as FieldInfo).FieldType;
			}
		}

		private class NonEntityProjectionAnalyzer : DataServiceALinqExpressionVisitor
		{
			private PathBox box;

			private Type type;

			private NonEntityProjectionAnalyzer(PathBox pb, Type type)
			{
				box = pb;
				this.type = type;
			}

			internal static void Analyze(Expression e, PathBox pb)
			{
				NonEntityProjectionAnalyzer nonEntityProjectionAnalyzer = new NonEntityProjectionAnalyzer(pb, e.Type);
				MemberInitExpression memberInitExpression = e as MemberInitExpression;
				if (memberInitExpression != null)
				{
					foreach (MemberBinding binding in memberInitExpression.Bindings)
					{
						MemberAssignment memberAssignment = binding as MemberAssignment;
						if (memberAssignment != null)
						{
							nonEntityProjectionAnalyzer.Visit(memberAssignment.Expression);
						}
					}
				}
				else
				{
					nonEntityProjectionAnalyzer.Visit(e);
				}
			}

			internal override Expression VisitUnary(UnaryExpression u)
			{
				if (!ResourceBinder.PatternRules.MatchConvertToAssignable(u) && CommonUtil.IsClientType(u.Operand.Type))
				{
					throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "Constructing or initializing instances of the type {0} with the expression {1} is not supported.", type, u.ToString()));
				}
				return base.VisitUnary(u);
			}

			internal override Expression VisitBinary(BinaryExpression b)
			{
				if (CommonUtil.IsClientType(b.Left.Type) || CommonUtil.IsClientType(b.Right.Type))
				{
					throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "Constructing or initializing instances of the type {0} with the expression {1} is not supported.", type, b.ToString()));
				}
				return base.VisitBinary(b);
			}

			internal override Expression VisitTypeIs(TypeBinaryExpression b)
			{
				if (CommonUtil.IsClientType(b.Expression.Type))
				{
					throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "Constructing or initializing instances of the type {0} with the expression {1} is not supported.", type, b.ToString()));
				}
				return base.VisitTypeIs(b);
			}

			internal override Expression VisitConditional(ConditionalExpression c)
			{
				ResourceBinder.PatternRules.MatchNullCheckResult matchNullCheckResult = ResourceBinder.PatternRules.MatchNullCheck(box.ParamExpressionInScope, c);
				if (matchNullCheckResult.Match)
				{
					Visit(matchNullCheckResult.AssignExpression);
					return c;
				}
				if (CommonUtil.IsClientType(c.Test.Type) || CommonUtil.IsClientType(c.IfTrue.Type) || CommonUtil.IsClientType(c.IfFalse.Type))
				{
					throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "Constructing or initializing instances of the type {0} with the expression {1} is not supported.", type, c.ToString()));
				}
				return base.VisitConditional(c);
			}

			internal override Expression VisitMemberAccess(MemberExpression m)
			{
				if (ClientConvert.IsKnownNullableType(m.Expression.Type))
				{
					return base.VisitMemberAccess(m);
				}
				if (!CommonUtil.IsClientType(m.Expression.Type))
				{
					throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "Constructing or initializing instances of the type {0} with the expression {1} is not supported.", type, m.ToString()));
				}
				PropertyInfo propInfo = null;
				if (ResourceBinder.PatternRules.MatchNonPrivateReadableProperty(m, out propInfo))
				{
					Expression result = base.VisitMemberAccess(m);
					box.AppendToPath(propInfo);
					return result;
				}
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "Constructing or initializing instances of the type {0} with the expression {1} is not supported.", type, m.ToString()));
			}

			internal override Expression VisitMethodCall(MethodCallExpression m)
			{
				if (IsMethodCallAllowedEntitySequence(m))
				{
					CheckChainedSequence(m, type);
					return base.VisitMethodCall(m);
				}
				if ((m.Object != null && CommonUtil.IsClientType(m.Object.Type)) || m.Arguments.Any((Expression a) => CommonUtil.IsClientType(a.Type)))
				{
					throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "Constructing or initializing instances of the type {0} with the expression {1} is not supported.", type, m.ToString()));
				}
				return base.VisitMethodCall(m);
			}

			internal override Expression VisitInvocation(InvocationExpression iv)
			{
				if (CommonUtil.IsClientType(iv.Expression.Type) || iv.Arguments.Any((Expression a) => CommonUtil.IsClientType(a.Type)))
				{
					throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "Constructing or initializing instances of the type {0} with the expression {1} is not supported.", type, iv.ToString()));
				}
				return base.VisitInvocation(iv);
			}

			internal override Expression VisitLambda(LambdaExpression lambda)
			{
				ProjectionAnalyzer.Analyze(lambda, box);
				return lambda;
			}

			internal override Expression VisitMemberInit(MemberInitExpression init)
			{
				ProjectionAnalyzer.Analyze(init, box);
				return init;
			}

			internal override NewExpression VisitNew(NewExpression nex)
			{
				if (CommonUtil.IsClientType(nex.Type))
				{
					throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "Constructing or initializing instances of the type {0} with the expression {1} is not supported.", type, nex.ToString()));
				}
				return base.VisitNew(nex);
			}

			internal override Expression VisitParameter(ParameterExpression p)
			{
				if (p != box.ParamExpressionInScope)
				{
					throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "Constructing or initializing instances of the type {0} with the expression {1} is not supported.", type, p.ToString()));
				}
				box.StartNewPath();
				return p;
			}

			internal override Expression VisitConstant(ConstantExpression c)
			{
				if (CommonUtil.IsClientType(c.Type))
				{
					throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "Constructing or initializing instances of the type {0} with the expression {1} is not supported.", type, c.ToString()));
				}
				return base.VisitConstant(c);
			}
		}

		internal static bool Analyze(LambdaExpression le, ResourceExpression re, bool matchMembers)
		{
			if (le.Body.NodeType == ExpressionType.Constant)
			{
				if (CommonUtil.IsClientType(le.Body.Type))
				{
					throw new NotSupportedException("Referencing of local entity type instances not supported when projecting results.");
				}
				re.Projection = new ProjectionQueryOptionExpression(le.Body.Type, le, new List<string>());
				return true;
			}
			if (le.Body.NodeType == ExpressionType.Call)
			{
				MethodCallExpression methodCallExpression = le.Body as MethodCallExpression;
				if (methodCallExpression.Method == ReflectionUtil.ProjectMethodInfo.MakeGenericMethod(le.Body.Type))
				{
					ConstantExpression constantExpression = methodCallExpression.Arguments[1] as ConstantExpression;
					re.Projection = new ProjectionQueryOptionExpression(le.Body.Type, ProjectionQueryOptionExpression.DefaultLambda, new List<string>((string[])constantExpression.Value));
					return true;
				}
			}
			if (le.Body.NodeType == ExpressionType.MemberInit || le.Body.NodeType == ExpressionType.New)
			{
				AnalyzeResourceExpression(le, re);
				return true;
			}
			if (matchMembers && SkipConverts(le.Body).NodeType == ExpressionType.MemberAccess)
			{
				AnalyzeResourceExpression(le, re);
				return true;
			}
			return false;
		}

		internal static void Analyze(LambdaExpression e, PathBox pb)
		{
			bool num = CommonUtil.IsClientType(e.Body.Type);
			pb.PushParamExpression(e.Parameters.Last());
			if (!num)
			{
				NonEntityProjectionAnalyzer.Analyze(e.Body, pb);
			}
			else
			{
				switch (e.Body.NodeType)
				{
				case ExpressionType.MemberInit:
					EntityProjectionAnalyzer.Analyze((MemberInitExpression)e.Body, pb);
					break;
				case ExpressionType.New:
					throw new NotSupportedException("Construction of entity type instances must use object initializer with default constructor.");
				case ExpressionType.Constant:
					throw new NotSupportedException("Referencing of local entity type instances not supported when projecting results.");
				default:
					NonEntityProjectionAnalyzer.Analyze(e.Body, pb);
					break;
				}
			}
			pb.PopParamExpression();
		}

		internal static bool IsMethodCallAllowedEntitySequence(MethodCallExpression call)
		{
			if (!ReflectionUtil.IsSequenceMethod(call.Method, SequenceMethod.ToList))
			{
				return ReflectionUtil.IsSequenceMethod(call.Method, SequenceMethod.Select);
			}
			return true;
		}

		internal static void CheckChainedSequence(MethodCallExpression call, Type type)
		{
			if (ReflectionUtil.IsSequenceMethod(call.Method, SequenceMethod.Select))
			{
				MethodCallExpression methodCallExpression = ResourceBinder.StripTo<MethodCallExpression>(call.Arguments[0]);
				if (methodCallExpression != null && ReflectionUtil.IsSequenceMethod(methodCallExpression.Method, SequenceMethod.Select))
				{
					throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "Constructing or initializing instances of the type {0} with the expression {1} is not supported.", type, call.ToString()));
				}
			}
		}

		private static void Analyze(MemberInitExpression mie, PathBox pb)
		{
			if (CommonUtil.IsClientType(mie.Type))
			{
				EntityProjectionAnalyzer.Analyze(mie, pb);
			}
			else
			{
				NonEntityProjectionAnalyzer.Analyze(mie, pb);
			}
		}

		private static void AnalyzeResourceExpression(LambdaExpression lambda, ResourceExpression resource)
		{
			PathBox pathBox = new PathBox();
			Analyze(lambda, pathBox);
			resource.Projection = new ProjectionQueryOptionExpression(lambda.Body.Type, lambda, pathBox.ProjectionPaths.ToList());
			resource.ExpandPaths = pathBox.ExpandPaths.Union(resource.ExpandPaths, StringComparer.Ordinal).ToList();
		}

		private static Expression SkipConverts(Expression expression)
		{
			Expression expression2 = expression;
			while (expression2.NodeType == ExpressionType.Convert || expression2.NodeType == ExpressionType.ConvertChecked)
			{
				expression2 = ((UnaryExpression)expression2).Operand;
			}
			return expression2;
		}
	}
}
