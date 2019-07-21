using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq.Expressions;

namespace Microsoft.Azure.Cosmos.Table.Queryable
{
	internal abstract class ALinqExpressionVisitor
	{
		internal virtual Expression Visit(Expression exp)
		{
			if (exp == null)
			{
				return exp;
			}
			switch (exp.NodeType)
			{
			case ExpressionType.ArrayLength:
			case ExpressionType.Convert:
			case ExpressionType.ConvertChecked:
			case ExpressionType.Negate:
			case ExpressionType.UnaryPlus:
			case ExpressionType.NegateChecked:
			case ExpressionType.Not:
			case ExpressionType.Quote:
			case ExpressionType.TypeAs:
				return VisitUnary((UnaryExpression)exp);
			case ExpressionType.Add:
			case ExpressionType.AddChecked:
			case ExpressionType.And:
			case ExpressionType.AndAlso:
			case ExpressionType.ArrayIndex:
			case ExpressionType.Coalesce:
			case ExpressionType.Divide:
			case ExpressionType.Equal:
			case ExpressionType.ExclusiveOr:
			case ExpressionType.GreaterThan:
			case ExpressionType.GreaterThanOrEqual:
			case ExpressionType.LeftShift:
			case ExpressionType.LessThan:
			case ExpressionType.LessThanOrEqual:
			case ExpressionType.Modulo:
			case ExpressionType.Multiply:
			case ExpressionType.MultiplyChecked:
			case ExpressionType.NotEqual:
			case ExpressionType.Or:
			case ExpressionType.OrElse:
			case ExpressionType.Power:
			case ExpressionType.RightShift:
			case ExpressionType.Subtract:
			case ExpressionType.SubtractChecked:
				return VisitBinary((BinaryExpression)exp);
			case ExpressionType.TypeIs:
				return VisitTypeIs((TypeBinaryExpression)exp);
			case ExpressionType.Conditional:
				return VisitConditional((ConditionalExpression)exp);
			case ExpressionType.Constant:
				return VisitConstant((ConstantExpression)exp);
			case ExpressionType.Parameter:
				return VisitParameter((ParameterExpression)exp);
			case ExpressionType.MemberAccess:
				return VisitMemberAccess((MemberExpression)exp);
			case ExpressionType.Call:
				return VisitMethodCall((MethodCallExpression)exp);
			case ExpressionType.Lambda:
				return VisitLambda((LambdaExpression)exp);
			case ExpressionType.New:
				return VisitNew((NewExpression)exp);
			case ExpressionType.NewArrayInit:
			case ExpressionType.NewArrayBounds:
				return VisitNewArray((NewArrayExpression)exp);
			case ExpressionType.Invoke:
				return VisitInvocation((InvocationExpression)exp);
			case ExpressionType.MemberInit:
				return VisitMemberInit((MemberInitExpression)exp);
			case ExpressionType.ListInit:
				return VisitListInit((ListInitExpression)exp);
			default:
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "The expression type {0} is not supported.", exp.NodeType.ToString()));
			}
		}

		internal virtual MemberBinding VisitBinding(MemberBinding binding)
		{
			switch (binding.BindingType)
			{
			case MemberBindingType.Assignment:
				return VisitMemberAssignment((MemberAssignment)binding);
			case MemberBindingType.MemberBinding:
				return VisitMemberMemberBinding((MemberMemberBinding)binding);
			case MemberBindingType.ListBinding:
				return VisitMemberListBinding((MemberListBinding)binding);
			default:
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "The expression type {0} is not supported.", binding.BindingType.ToString()));
			}
		}

		internal virtual ElementInit VisitElementInitializer(ElementInit initializer)
		{
			ReadOnlyCollection<Expression> readOnlyCollection = VisitExpressionList(initializer.Arguments);
			if (readOnlyCollection != initializer.Arguments)
			{
				return Expression.ElementInit(initializer.AddMethod, readOnlyCollection);
			}
			return initializer;
		}

		internal virtual Expression VisitUnary(UnaryExpression u)
		{
			Expression expression = Visit(u.Operand);
			if (expression != u.Operand)
			{
				return Expression.MakeUnary(u.NodeType, expression, u.Type, u.Method);
			}
			return u;
		}

		internal virtual Expression VisitBinary(BinaryExpression b)
		{
			Expression expression = Visit(b.Left);
			Expression expression2 = Visit(b.Right);
			Expression expression3 = Visit(b.Conversion);
			if (expression != b.Left || expression2 != b.Right || expression3 != b.Conversion)
			{
				if (b.NodeType == ExpressionType.Coalesce && b.Conversion != null)
				{
					return Expression.Coalesce(expression, expression2, expression3 as LambdaExpression);
				}
				return Expression.MakeBinary(b.NodeType, expression, expression2, b.IsLiftedToNull, b.Method);
			}
			return b;
		}

		internal virtual Expression VisitTypeIs(TypeBinaryExpression b)
		{
			Expression expression = Visit(b.Expression);
			if (expression != b.Expression)
			{
				return Expression.TypeIs(expression, b.TypeOperand);
			}
			return b;
		}

		internal virtual Expression VisitConstant(ConstantExpression c)
		{
			return c;
		}

		internal virtual Expression VisitConditional(ConditionalExpression c)
		{
			Expression expression = Visit(c.Test);
			Expression expression2 = Visit(c.IfTrue);
			Expression expression3 = Visit(c.IfFalse);
			if (expression != c.Test || expression2 != c.IfTrue || expression3 != c.IfFalse)
			{
				return Expression.Condition(expression, expression2, expression3);
			}
			return c;
		}

		internal virtual Expression VisitParameter(ParameterExpression p)
		{
			return p;
		}

		internal virtual Expression VisitMemberAccess(MemberExpression m)
		{
			Expression expression = Visit(m.Expression);
			if (expression != m.Expression)
			{
				return Expression.MakeMemberAccess(expression, m.Member);
			}
			return m;
		}

		internal virtual Expression VisitMethodCall(MethodCallExpression m)
		{
			Expression expression = Visit(m.Object);
			IEnumerable<Expression> enumerable = VisitExpressionList(m.Arguments);
			if (expression != m.Object || enumerable != m.Arguments)
			{
				return Expression.Call(expression, m.Method, enumerable);
			}
			return m;
		}

		internal virtual ReadOnlyCollection<Expression> VisitExpressionList(ReadOnlyCollection<Expression> original)
		{
			List<Expression> list = null;
			int i = 0;
			for (int count = original.Count; i < count; i++)
			{
				Expression expression = Visit(original[i]);
				if (list != null)
				{
					list.Add(expression);
				}
				else if (expression != original[i])
				{
					list = new List<Expression>(count);
					for (int j = 0; j < i; j++)
					{
						list.Add(original[j]);
					}
					list.Add(expression);
				}
			}
			if (list != null)
			{
				return new ReadOnlyCollection<Expression>(list);
			}
			return original;
		}

		internal virtual MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
		{
			Expression expression = Visit(assignment.Expression);
			if (expression != assignment.Expression)
			{
				return Expression.Bind(assignment.Member, expression);
			}
			return assignment;
		}

		internal virtual MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding binding)
		{
			IEnumerable<MemberBinding> enumerable = VisitBindingList(binding.Bindings);
			if (enumerable != binding.Bindings)
			{
				return Expression.MemberBind(binding.Member, enumerable);
			}
			return binding;
		}

		internal virtual MemberListBinding VisitMemberListBinding(MemberListBinding binding)
		{
			IEnumerable<ElementInit> enumerable = VisitElementInitializerList(binding.Initializers);
			if (enumerable != binding.Initializers)
			{
				return Expression.ListBind(binding.Member, enumerable);
			}
			return binding;
		}

		internal virtual IEnumerable<MemberBinding> VisitBindingList(ReadOnlyCollection<MemberBinding> original)
		{
			List<MemberBinding> list = null;
			int i = 0;
			for (int count = original.Count; i < count; i++)
			{
				MemberBinding memberBinding = VisitBinding(original[i]);
				if (list != null)
				{
					list.Add(memberBinding);
				}
				else if (memberBinding != original[i])
				{
					list = new List<MemberBinding>(count);
					for (int j = 0; j < i; j++)
					{
						list.Add(original[j]);
					}
					list.Add(memberBinding);
				}
			}
			if (list != null)
			{
				return list;
			}
			return original;
		}

		internal virtual IEnumerable<ElementInit> VisitElementInitializerList(ReadOnlyCollection<ElementInit> original)
		{
			List<ElementInit> list = null;
			int i = 0;
			for (int count = original.Count; i < count; i++)
			{
				ElementInit elementInit = VisitElementInitializer(original[i]);
				if (list != null)
				{
					list.Add(elementInit);
				}
				else if (elementInit != original[i])
				{
					list = new List<ElementInit>(count);
					for (int j = 0; j < i; j++)
					{
						list.Add(original[j]);
					}
					list.Add(elementInit);
				}
			}
			if (list != null)
			{
				return list;
			}
			return original;
		}

		internal virtual Expression VisitLambda(LambdaExpression lambda)
		{
			Expression expression = Visit(lambda.Body);
			if (expression != lambda.Body)
			{
				return Expression.Lambda(lambda.Type, expression, lambda.Parameters);
			}
			return lambda;
		}

		internal virtual NewExpression VisitNew(NewExpression nex)
		{
			IEnumerable<Expression> enumerable = VisitExpressionList(nex.Arguments);
			if (enumerable != nex.Arguments)
			{
				if (nex.Members != null)
				{
					return Expression.New(nex.Constructor, enumerable, nex.Members);
				}
				return Expression.New(nex.Constructor, enumerable);
			}
			return nex;
		}

		internal virtual Expression VisitMemberInit(MemberInitExpression init)
		{
			NewExpression newExpression = VisitNew(init.NewExpression);
			IEnumerable<MemberBinding> enumerable = VisitBindingList(init.Bindings);
			if (newExpression != init.NewExpression || enumerable != init.Bindings)
			{
				return Expression.MemberInit(newExpression, enumerable);
			}
			return init;
		}

		internal virtual Expression VisitListInit(ListInitExpression init)
		{
			NewExpression newExpression = VisitNew(init.NewExpression);
			IEnumerable<ElementInit> enumerable = VisitElementInitializerList(init.Initializers);
			if (newExpression != init.NewExpression || enumerable != init.Initializers)
			{
				return Expression.ListInit(newExpression, enumerable);
			}
			return init;
		}

		internal virtual Expression VisitNewArray(NewArrayExpression na)
		{
			IEnumerable<Expression> enumerable = VisitExpressionList(na.Expressions);
			if (enumerable != na.Expressions)
			{
				if (na.NodeType == ExpressionType.NewArrayInit)
				{
					return Expression.NewArrayInit(na.Type.GetElementType(), enumerable);
				}
				return Expression.NewArrayBounds(na.Type.GetElementType(), enumerable);
			}
			return na;
		}

		internal virtual Expression VisitInvocation(InvocationExpression iv)
		{
			IEnumerable<Expression> enumerable = VisitExpressionList(iv.Arguments);
			Expression expression = Visit(iv.Expression);
			if (enumerable != iv.Arguments || expression != iv.Expression)
			{
				return Expression.Invoke(expression, enumerable);
			}
			return iv;
		}
	}
}
