using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Microsoft.Azure.Cosmos.Table.Queryable
{
	internal class ExpressionWriter : DataServiceALinqExpressionVisitor
	{
		internal readonly StringBuilder builder;

		private readonly Stack<Expression> expressionStack;

		private bool cantTranslateExpression;

		private Expression parent;

		protected ExpressionWriter()
		{
			builder = new StringBuilder();
			expressionStack = new Stack<Expression>();
			expressionStack.Push(null);
		}

		internal static string ExpressionToString(Expression e)
		{
			return new ExpressionWriter().ConvertExpressionToString(e);
		}

		internal string ConvertExpressionToString(Expression e)
		{
			string result = Translate(e);
			if (cantTranslateExpression)
			{
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "The expression {0} is not supported.", e.ToString()));
			}
			return result;
		}

		internal override Expression Visit(Expression exp)
		{
			parent = expressionStack.Peek();
			expressionStack.Push(exp);
			Expression result = base.Visit(exp);
			expressionStack.Pop();
			return result;
		}

		internal override Expression VisitConditional(ConditionalExpression c)
		{
			cantTranslateExpression = true;
			return c;
		}

		internal override Expression VisitLambda(LambdaExpression lambda)
		{
			cantTranslateExpression = true;
			return lambda;
		}

		internal override NewExpression VisitNew(NewExpression nex)
		{
			cantTranslateExpression = true;
			return nex;
		}

		internal override Expression VisitMemberInit(MemberInitExpression init)
		{
			cantTranslateExpression = true;
			return init;
		}

		internal override Expression VisitListInit(ListInitExpression init)
		{
			cantTranslateExpression = true;
			return init;
		}

		internal override Expression VisitNewArray(NewArrayExpression na)
		{
			cantTranslateExpression = true;
			return na;
		}

		internal override Expression VisitInvocation(InvocationExpression iv)
		{
			cantTranslateExpression = true;
			return iv;
		}

		internal override Expression VisitInputReferenceExpression(InputReferenceExpression ire)
		{
			if (parent == null || parent.NodeType != ExpressionType.MemberAccess)
			{
				string arg = (parent != null) ? parent.ToString() : ire.ToString();
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "The expression {0} is not supported.", arg));
			}
			return ire;
		}

		internal override Expression VisitMethodCall(MethodCallExpression m)
		{
			if (TypeSystem.TryGetQueryOptionMethod(m.Method, out string methodName))
			{
				builder.Append(methodName);
				builder.Append('(');
				if (methodName == "substringof")
				{
					Visit(m.Arguments[0]);
					builder.Append(',');
					Visit(m.Object);
				}
				else
				{
					if (m.Object != null)
					{
						Visit(m.Object);
					}
					if (m.Arguments.Count > 0)
					{
						if (m.Object != null)
						{
							builder.Append(',');
						}
						for (int i = 0; i < m.Arguments.Count; i++)
						{
							Visit(m.Arguments[i]);
							if (i < m.Arguments.Count - 1)
							{
								builder.Append(',');
							}
						}
					}
				}
				builder.Append(')');
			}
			else
			{
				cantTranslateExpression = true;
			}
			return m;
		}

		internal override Expression VisitMemberAccess(MemberExpression m)
		{
			if (m.Member is FieldInfo)
			{
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "Referencing public field '{0}' not supported in query option expression.  Use public property instead.", m.Member.Name));
			}
			if (m.Member.DeclaringType == typeof(EntityProperty))
			{
				MethodCallExpression methodCallExpression = m.Expression as MethodCallExpression;
				if (methodCallExpression != null && methodCallExpression.Arguments.Count == 1 && methodCallExpression.Method == ReflectionUtil.DictionaryGetItemMethodInfo)
				{
					MemberExpression memberExpression = methodCallExpression.Object as MemberExpression;
					if (memberExpression != null && memberExpression.Member.DeclaringType == typeof(DynamicTableEntity) && memberExpression.Member.Name == "Properties")
					{
						ConstantExpression constantExpression = methodCallExpression.Arguments[0] as ConstantExpression;
						if (constantExpression == null || !(constantExpression.Value is string))
						{
							throw new NotSupportedException("Accessing property dictionary of DynamicTableEntity requires a string constant for property name.");
						}
						builder.Append(TranslateMemberName((string)constantExpression.Value));
						return constantExpression;
					}
				}
				throw new NotSupportedException("Referencing {0} on EntityProperty only supported with properties dictionary exposed via DynamicTableEntity.");
			}
			Expression expression = Visit(m.Expression);
			if (m.Member.Name == "Value" && m.Member.DeclaringType.IsGenericType && m.Member.DeclaringType.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				return m;
			}
			if (!IsInputReference(expression) && expression.NodeType != ExpressionType.Convert && expression.NodeType != ExpressionType.ConvertChecked)
			{
				builder.Append('/');
			}
			builder.Append(TranslateMemberName(m.Member.Name));
			return m;
		}

		internal override Expression VisitConstant(ConstantExpression c)
		{
			string result = null;
			if (c.Value == null)
			{
				builder.Append("null");
				return c;
			}
			if (!ClientConvert.TryKeyPrimitiveToString(c.Value, out result))
			{
				throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Could not convert constant {0} expression to string.", c.Value));
			}
			builder.Append(result);
			return c;
		}

		internal override Expression VisitUnary(UnaryExpression u)
		{
			switch (u.NodeType)
			{
			case ExpressionType.Not:
				builder.Append("not");
				builder.Append(' ');
				VisitOperand(u.Operand);
				break;
			case ExpressionType.Negate:
			case ExpressionType.NegateChecked:
				builder.Append(' ');
				builder.Append(TranslateOperator(u.NodeType));
				VisitOperand(u.Operand);
				break;
			default:
				cantTranslateExpression = true;
				break;
			case ExpressionType.Convert:
			case ExpressionType.ConvertChecked:
			case ExpressionType.UnaryPlus:
				break;
			}
			return u;
		}

		internal override Expression VisitBinary(BinaryExpression b)
		{
			VisitOperand(b.Left);
			builder.Append(' ');
			string value = TranslateOperator(b.NodeType);
			if (string.IsNullOrEmpty(value))
			{
				cantTranslateExpression = true;
			}
			else
			{
				builder.Append(value);
			}
			builder.Append(' ');
			VisitOperand(b.Right);
			return b;
		}

		internal override Expression VisitTypeIs(TypeBinaryExpression b)
		{
			builder.Append("isof");
			builder.Append('(');
			if (!IsInputReference(b.Expression))
			{
				Visit(b.Expression);
				builder.Append(',');
				builder.Append(' ');
			}
			builder.Append('\'');
			builder.Append(TypeNameForUri(b.TypeOperand));
			builder.Append('\'');
			builder.Append(')');
			return b;
		}

		internal override Expression VisitParameter(ParameterExpression p)
		{
			return p;
		}

		private static bool IsInputReference(Expression exp)
		{
			if (!(exp is InputReferenceExpression))
			{
				return exp is ParameterExpression;
			}
			return true;
		}

		private string TypeNameForUri(Type type)
		{
			type = (Nullable.GetUnderlyingType(type) ?? type);
			if (ClientConvert.IsKnownType(type))
			{
				if (ClientConvert.IsSupportedPrimitiveTypeForUri(type))
				{
					return ClientConvert.ToTypeName(type);
				}
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "Can't cast to unsupported type '{0}'", type.Name));
			}
			return null;
		}

		private void VisitOperand(Expression e)
		{
			if (e is BinaryExpression || e is UnaryExpression)
			{
				builder.Append('(');
				Visit(e);
				builder.Append(')');
			}
			else
			{
				Visit(e);
			}
		}

		private string Translate(Expression e)
		{
			Visit(e);
			return builder.ToString();
		}

		protected virtual string TranslateMemberName(string memberName)
		{
			return memberName;
		}

		protected virtual object TranslateConstantValue(object value)
		{
			return value;
		}

		protected virtual string TranslateOperator(ExpressionType type)
		{
			switch (type)
			{
			case ExpressionType.And:
			case ExpressionType.AndAlso:
				return "and";
			case ExpressionType.Or:
			case ExpressionType.OrElse:
				return "or";
			case ExpressionType.Equal:
				return "eq";
			case ExpressionType.NotEqual:
				return "ne";
			case ExpressionType.LessThan:
				return "lt";
			case ExpressionType.LessThanOrEqual:
				return "le";
			case ExpressionType.GreaterThan:
				return "gt";
			case ExpressionType.GreaterThanOrEqual:
				return "ge";
			case ExpressionType.Add:
			case ExpressionType.AddChecked:
				return "add";
			case ExpressionType.Subtract:
			case ExpressionType.SubtractChecked:
				return "sub";
			case ExpressionType.Multiply:
			case ExpressionType.MultiplyChecked:
				return "mul";
			case ExpressionType.Divide:
				return "div";
			case ExpressionType.Modulo:
				return "mod";
			case ExpressionType.Negate:
			case ExpressionType.NegateChecked:
				return "-";
			default:
				return null;
			}
		}
	}
}
