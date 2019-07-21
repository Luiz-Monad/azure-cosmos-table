using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Azure.Cosmos.Table.Queryable
{
	internal class ExpressionNormalizer : DataServiceALinqExpressionVisitor
	{
		private abstract class Pattern
		{
			internal abstract PatternKind Kind
			{
				get;
			}
		}

		private enum PatternKind
		{
			Compare
		}

		private sealed class ComparePattern : Pattern
		{
			internal readonly Expression Left;

			internal readonly Expression Right;

			internal override PatternKind Kind => PatternKind.Compare;

			internal ComparePattern(Expression left, Expression right)
			{
				Left = left;
				Right = right;
			}
		}

		private const bool LiftToNull = false;

		private readonly Dictionary<Expression, Pattern> patterns = new Dictionary<Expression, Pattern>(ReferenceEqualityComparer<Expression>.Instance);

		private readonly Dictionary<Expression, Expression> normalizerRewrites;

		private static readonly MethodInfo StaticRelationalOperatorPlaceholderMethod = typeof(ExpressionNormalizer).GetMethod("RelationalOperatorPlaceholder", BindingFlags.Static | BindingFlags.NonPublic);

		internal Dictionary<Expression, Expression> NormalizerRewrites => normalizerRewrites;

		private ExpressionNormalizer(Dictionary<Expression, Expression> normalizerRewrites)
		{
			this.normalizerRewrites = normalizerRewrites;
		}

		internal static Expression Normalize(Expression expression, Dictionary<Expression, Expression> rewrites)
		{
			return new ExpressionNormalizer(rewrites).Visit(expression);
		}

		internal override Expression VisitBinary(BinaryExpression b)
		{
			BinaryExpression binaryExpression = (BinaryExpression)base.VisitBinary(b);
			if (binaryExpression.NodeType == ExpressionType.Equal)
			{
				Expression expression = UnwrapObjectConvert(binaryExpression.Left);
				Expression expression2 = UnwrapObjectConvert(binaryExpression.Right);
				if (expression != binaryExpression.Left || expression2 != binaryExpression.Right)
				{
					binaryExpression = CreateRelationalOperator(ExpressionType.Equal, expression, expression2);
				}
			}
			if (patterns.TryGetValue(binaryExpression.Left, out Pattern value) && value.Kind == PatternKind.Compare && IsConstantZero(binaryExpression.Right))
			{
				ComparePattern comparePattern = (ComparePattern)value;
				if (TryCreateRelationalOperator(binaryExpression.NodeType, comparePattern.Left, comparePattern.Right, out BinaryExpression result))
				{
					binaryExpression = result;
				}
			}
			RecordRewrite(b, binaryExpression);
			return binaryExpression;
		}

		internal override Expression VisitUnary(UnaryExpression u)
		{
			Expression expression = (UnaryExpression)base.VisitUnary(u);
			RecordRewrite(u, expression);
			return expression;
		}

		private static Expression UnwrapObjectConvert(Expression input)
		{
			if (input.NodeType == ExpressionType.Constant && input.Type == typeof(object))
			{
				ConstantExpression constantExpression = (ConstantExpression)input;
				if (constantExpression.Value != null && constantExpression.Value.GetType() != typeof(object))
				{
					return Expression.Constant(constantExpression.Value, constantExpression.Value.GetType());
				}
			}
			while (ExpressionType.Convert == input.NodeType && typeof(object) == input.Type)
			{
				input = ((UnaryExpression)input).Operand;
			}
			return input;
		}

		private static bool IsConstantZero(Expression expression)
		{
			if (expression.NodeType == ExpressionType.Constant)
			{
				return ((ConstantExpression)expression).Value.Equals(0);
			}
			return false;
		}

		internal override Expression VisitMethodCall(MethodCallExpression call)
		{
			Expression expression = VisitMethodCallNoRewrite(call);
			RecordRewrite(call, expression);
			return expression;
		}

		internal Expression VisitMethodCallNoRewrite(MethodCallExpression call)
		{
			MethodCallExpression methodCallExpression = (MethodCallExpression)base.VisitMethodCall(call);
			if (methodCallExpression.Method.IsStatic && methodCallExpression.Method.Name.StartsWith("op_", StringComparison.Ordinal))
			{
				if (methodCallExpression.Arguments.Count == 2)
				{
					switch (methodCallExpression.Method.Name)
					{
					case "op_Equality":
						return Expression.Equal(methodCallExpression.Arguments[0], methodCallExpression.Arguments[1], liftToNull: false, methodCallExpression.Method);
					case "op_Inequality":
						return Expression.NotEqual(methodCallExpression.Arguments[0], methodCallExpression.Arguments[1], liftToNull: false, methodCallExpression.Method);
					case "op_GreaterThan":
						return Expression.GreaterThan(methodCallExpression.Arguments[0], methodCallExpression.Arguments[1], liftToNull: false, methodCallExpression.Method);
					case "op_GreaterThanOrEqual":
						return Expression.GreaterThanOrEqual(methodCallExpression.Arguments[0], methodCallExpression.Arguments[1], liftToNull: false, methodCallExpression.Method);
					case "op_LessThan":
						return Expression.LessThan(methodCallExpression.Arguments[0], methodCallExpression.Arguments[1], liftToNull: false, methodCallExpression.Method);
					case "op_LessThanOrEqual":
						return Expression.LessThanOrEqual(methodCallExpression.Arguments[0], methodCallExpression.Arguments[1], liftToNull: false, methodCallExpression.Method);
					case "op_Multiply":
						return Expression.Multiply(methodCallExpression.Arguments[0], methodCallExpression.Arguments[1], methodCallExpression.Method);
					case "op_Subtraction":
						return Expression.Subtract(methodCallExpression.Arguments[0], methodCallExpression.Arguments[1], methodCallExpression.Method);
					case "op_Addition":
						return Expression.Add(methodCallExpression.Arguments[0], methodCallExpression.Arguments[1], methodCallExpression.Method);
					case "op_Division":
						return Expression.Divide(methodCallExpression.Arguments[0], methodCallExpression.Arguments[1], methodCallExpression.Method);
					case "op_Modulus":
						return Expression.Modulo(methodCallExpression.Arguments[0], methodCallExpression.Arguments[1], methodCallExpression.Method);
					case "op_BitwiseAnd":
						return Expression.And(methodCallExpression.Arguments[0], methodCallExpression.Arguments[1], methodCallExpression.Method);
					case "op_BitwiseOr":
						return Expression.Or(methodCallExpression.Arguments[0], methodCallExpression.Arguments[1], methodCallExpression.Method);
					case "op_ExclusiveOr":
						return Expression.ExclusiveOr(methodCallExpression.Arguments[0], methodCallExpression.Arguments[1], methodCallExpression.Method);
					}
				}
				if (methodCallExpression.Arguments.Count == 1)
				{
					switch (methodCallExpression.Method.Name)
					{
					case "op_UnaryNegation":
						return Expression.Negate(methodCallExpression.Arguments[0], methodCallExpression.Method);
					case "op_UnaryPlus":
						return Expression.UnaryPlus(methodCallExpression.Arguments[0], methodCallExpression.Method);
					case "op_Explicit":
					case "op_Implicit":
						return Expression.Convert(methodCallExpression.Arguments[0], methodCallExpression.Type, methodCallExpression.Method);
					case "op_OnesComplement":
					case "op_False":
						return Expression.Not(methodCallExpression.Arguments[0], methodCallExpression.Method);
					}
				}
			}
			if (methodCallExpression.Method.IsStatic && methodCallExpression.Method.Name == "Equals" && methodCallExpression.Arguments.Count > 1)
			{
				return Expression.Equal(methodCallExpression.Arguments[0], methodCallExpression.Arguments[1], liftToNull: false, methodCallExpression.Method);
			}
			if (!methodCallExpression.Method.IsStatic && methodCallExpression.Method.Name == "Equals" && methodCallExpression.Arguments.Count > 0)
			{
				return CreateRelationalOperator(ExpressionType.Equal, methodCallExpression.Object, methodCallExpression.Arguments[0]);
			}
			if (methodCallExpression.Method.IsStatic && methodCallExpression.Method.Name == "CompareString" && methodCallExpression.Method.DeclaringType.FullName == "Microsoft.VisualBasic.CompilerServices.Operators")
			{
				return CreateCompareExpression(methodCallExpression.Arguments[0], methodCallExpression.Arguments[1]);
			}
			if (!methodCallExpression.Method.IsStatic && methodCallExpression.Method.Name == "CompareTo" && methodCallExpression.Arguments.Count == 1 && methodCallExpression.Method.ReturnType == typeof(int))
			{
				return CreateCompareExpression(methodCallExpression.Object, methodCallExpression.Arguments[0]);
			}
			if (methodCallExpression.Method.IsStatic && methodCallExpression.Method.Name == "Compare" && methodCallExpression.Arguments.Count > 1 && methodCallExpression.Method.ReturnType == typeof(int))
			{
				return CreateCompareExpression(methodCallExpression.Arguments[0], methodCallExpression.Arguments[1]);
			}
			return NormalizePredicateArgument(methodCallExpression);
		}

		private static MethodCallExpression NormalizePredicateArgument(MethodCallExpression callExpression)
		{
			if (HasPredicateArgument(callExpression, out int argumentOrdinal) && TryMatchCoalescePattern(callExpression.Arguments[argumentOrdinal], out Expression normalized))
			{
				List<Expression> list = new List<Expression>(callExpression.Arguments);
				list[argumentOrdinal] = normalized;
				return Expression.Call(callExpression.Object, callExpression.Method, list);
			}
			return callExpression;
		}

		private static bool HasPredicateArgument(MethodCallExpression callExpression, out int argumentOrdinal)
		{
			argumentOrdinal = 0;
			bool result = false;
			if (2 <= callExpression.Arguments.Count && ReflectionUtil.TryIdentifySequenceMethod(callExpression.Method, out SequenceMethod sequenceMethod))
			{
				switch (sequenceMethod)
				{
				case SequenceMethod.Where:
				case SequenceMethod.WhereOrdinal:
				case SequenceMethod.TakeWhile:
				case SequenceMethod.TakeWhileOrdinal:
				case SequenceMethod.SkipWhile:
				case SequenceMethod.SkipWhileOrdinal:
				case SequenceMethod.FirstPredicate:
				case SequenceMethod.FirstOrDefaultPredicate:
				case SequenceMethod.LastPredicate:
				case SequenceMethod.LastOrDefaultPredicate:
				case SequenceMethod.SinglePredicate:
				case SequenceMethod.SingleOrDefaultPredicate:
				case SequenceMethod.AnyPredicate:
				case SequenceMethod.All:
				case SequenceMethod.CountPredicate:
				case SequenceMethod.LongCountPredicate:
					argumentOrdinal = 1;
					result = true;
					break;
				}
			}
			return result;
		}

		private static bool TryMatchCoalescePattern(Expression expression, out Expression normalized)
		{
			normalized = null;
			bool result = false;
			if (expression.NodeType == ExpressionType.Quote)
			{
				if (TryMatchCoalescePattern(((UnaryExpression)expression).Operand, out normalized))
				{
					result = true;
					normalized = Expression.Quote(normalized);
				}
			}
			else if (expression.NodeType == ExpressionType.Lambda)
			{
				LambdaExpression lambdaExpression = (LambdaExpression)expression;
				if (lambdaExpression.Body.NodeType == ExpressionType.Coalesce && lambdaExpression.Body.Type == typeof(bool))
				{
					BinaryExpression binaryExpression = (BinaryExpression)lambdaExpression.Body;
					if (binaryExpression.Right.NodeType == ExpressionType.Constant && false.Equals(((ConstantExpression)binaryExpression.Right).Value))
					{
						normalized = Expression.Lambda(lambdaExpression.Type, Expression.Convert(binaryExpression.Left, typeof(bool)), lambdaExpression.Parameters);
						result = true;
					}
				}
			}
			return result;
		}

		private static bool RelationalOperatorPlaceholder<TLeft, TRight>(TLeft left, TRight right)
		{
			return (object)left == (object)right;
		}

		private static BinaryExpression CreateRelationalOperator(ExpressionType op, Expression left, Expression right)
		{
			TryCreateRelationalOperator(op, left, right, out BinaryExpression result);
			return result;
		}

		private static bool TryCreateRelationalOperator(ExpressionType op, Expression left, Expression right, out BinaryExpression result)
		{
			MethodInfo method = StaticRelationalOperatorPlaceholderMethod.MakeGenericMethod(left.Type, right.Type);
			switch (op)
			{
			case ExpressionType.Equal:
				result = Expression.Equal(left, right, liftToNull: false, method);
				return true;
			case ExpressionType.NotEqual:
				result = Expression.NotEqual(left, right, liftToNull: false, method);
				return true;
			case ExpressionType.LessThan:
				result = Expression.LessThan(left, right, liftToNull: false, method);
				return true;
			case ExpressionType.LessThanOrEqual:
				result = Expression.LessThanOrEqual(left, right, liftToNull: false, method);
				return true;
			case ExpressionType.GreaterThan:
				result = Expression.GreaterThan(left, right, liftToNull: false, method);
				return true;
			case ExpressionType.GreaterThanOrEqual:
				result = Expression.GreaterThanOrEqual(left, right, liftToNull: false, method);
				return true;
			default:
				result = null;
				return false;
			}
		}

		private Expression CreateCompareExpression(Expression left, Expression right)
		{
			Expression expression = Expression.Condition(CreateRelationalOperator(ExpressionType.Equal, left, right), Expression.Constant(0), Expression.Condition(CreateRelationalOperator(ExpressionType.GreaterThan, left, right), Expression.Constant(1), Expression.Constant(-1)));
			patterns[expression] = new ComparePattern(left, right);
			return expression;
		}

		private void RecordRewrite(Expression source, Expression rewritten)
		{
			if (source != rewritten)
			{
				NormalizerRewrites.Add(rewritten, source);
			}
		}
	}
}
