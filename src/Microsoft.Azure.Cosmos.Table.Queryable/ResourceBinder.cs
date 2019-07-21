using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Azure.Cosmos.Table.Queryable
{
	internal class ResourceBinder : DataServiceALinqExpressionVisitor
	{
		internal static class PatternRules
		{
			internal struct MatchNullCheckResult
			{
				internal Expression AssignExpression;

				internal bool Match;

				internal Expression TestToNullExpression;
			}

			internal struct MatchEqualityCheckResult
			{
				internal bool EqualityYieldsTrue;

				internal bool Match;

				internal Expression TestLeft;

				internal Expression TestRight;
			}

			internal static bool MatchConvertToAssignable(UnaryExpression expression)
			{
				if (expression.NodeType != ExpressionType.Convert && expression.NodeType != ExpressionType.ConvertChecked && expression.NodeType != ExpressionType.TypeAs)
				{
					return false;
				}
				return expression.Type.IsAssignableFrom(expression.Operand.Type);
			}

			internal static bool MatchParameterMemberAccess(Expression expression)
			{
				LambdaExpression lambdaExpression = StripTo<LambdaExpression>(expression);
				if (lambdaExpression == null || lambdaExpression.Parameters.Count != 1)
				{
					return false;
				}
				ParameterExpression parameterExpression = lambdaExpression.Parameters[0];
				for (MemberExpression memberExpression = StripTo<MemberExpression>(StripCastMethodCalls(lambdaExpression.Body)); memberExpression != null; memberExpression = StripTo<MemberExpression>(memberExpression.Expression))
				{
					if (memberExpression.Expression == parameterExpression)
					{
						return true;
					}
				}
				return false;
			}

			internal static bool MatchPropertyAccess(Expression e, out MemberExpression member, out Expression instance, out List<string> propertyPath)
			{
				instance = null;
				propertyPath = null;
				MemberExpression memberExpression = member = StripTo<MemberExpression>(e);
				while (memberExpression != null)
				{
					if (MatchNonPrivateReadableProperty(memberExpression, out PropertyInfo propInfo))
					{
						if (propertyPath == null)
						{
							propertyPath = new List<string>();
						}
						propertyPath.Insert(0, propInfo.Name);
						e = memberExpression.Expression;
						memberExpression = StripTo<MemberExpression>(e);
					}
					else
					{
						memberExpression = null;
					}
				}
				if (propertyPath != null)
				{
					instance = e;
					return true;
				}
				return false;
			}

			internal static bool MatchConstant(Expression e, out ConstantExpression constExpr)
			{
				constExpr = (e as ConstantExpression);
				return constExpr != null;
			}

			internal static bool MatchAnd(Expression e)
			{
				BinaryExpression binaryExpression = e as BinaryExpression;
				if (binaryExpression != null)
				{
					if (binaryExpression.NodeType != ExpressionType.And)
					{
						return binaryExpression.NodeType == ExpressionType.AndAlso;
					}
					return true;
				}
				return false;
			}

			internal static bool MatchNonPrivateReadableProperty(Expression e, out PropertyInfo propInfo)
			{
				MemberExpression memberExpression = e as MemberExpression;
				if (memberExpression == null)
				{
					propInfo = null;
					return false;
				}
				return MatchNonPrivateReadableProperty(memberExpression, out propInfo);
			}

			internal static bool MatchNonPrivateReadableProperty(MemberExpression me, out PropertyInfo propInfo)
			{
				propInfo = null;
				if (me.Member.MemberType == MemberTypes.Property)
				{
					PropertyInfo propertyInfo = (PropertyInfo)me.Member;
					if (propertyInfo.CanRead && !TypeSystem.IsPrivate(propertyInfo))
					{
						propInfo = propertyInfo;
						return true;
					}
				}
				return false;
			}

			internal static bool MatchReferenceEquals(Expression expression)
			{
				MethodCallExpression methodCallExpression = expression as MethodCallExpression;
				if (methodCallExpression == null)
				{
					return false;
				}
				return methodCallExpression.Method == typeof(object).GetMethod("ReferenceEquals");
			}

			internal static bool MatchResource(Expression expression, out ResourceExpression resource)
			{
				resource = (expression as ResourceExpression);
				return resource != null;
			}

			internal static bool MatchDoubleArgumentLambda(Expression expression, out LambdaExpression lambda)
			{
				return MatchNaryLambda(expression, 2, out lambda);
			}

			internal static bool MatchIdentitySelector(LambdaExpression lambda)
			{
				return lambda.Parameters[0] == StripTo<ParameterExpression>(lambda.Body);
			}

			internal static bool MatchSingleArgumentLambda(Expression expression, out LambdaExpression lambda)
			{
				return MatchNaryLambda(expression, 1, out lambda);
			}

			internal static bool MatchTransparentIdentitySelector(Expression input, LambdaExpression selector)
			{
				if (selector.Parameters.Count != 1)
				{
					return false;
				}
				ResourceSetExpression resourceSetExpression = input as ResourceSetExpression;
				if (resourceSetExpression == null || resourceSetExpression.TransparentScope == null)
				{
					return false;
				}
				Expression body = selector.Body;
				ParameterExpression parameterExpression = selector.Parameters[0];
				if (!MatchPropertyAccess(body, out MemberExpression _, out Expression instance, out List<string> propertyPath))
				{
					return false;
				}
				if (instance == parameterExpression && propertyPath.Count == 1)
				{
					return propertyPath[0] == resourceSetExpression.TransparentScope.Accessor;
				}
				return false;
			}

			internal static bool MatchIdentityProjectionResultSelector(Expression e)
			{
				LambdaExpression lambdaExpression = (LambdaExpression)e;
				return lambdaExpression.Body == lambdaExpression.Parameters[1];
			}

			internal static bool MatchTransparentScopeSelector(ResourceSetExpression input, LambdaExpression resultSelector, out ResourceSetExpression.TransparentAccessors transparentScope)
			{
				transparentScope = null;
				if (resultSelector.Body.NodeType != ExpressionType.New)
				{
					return false;
				}
				NewExpression newExpression = (NewExpression)resultSelector.Body;
				if (newExpression.Arguments.Count < 2)
				{
					return false;
				}
				if (newExpression.Type.BaseType != typeof(object))
				{
					return false;
				}
				ParameterInfo[] parameters = newExpression.Constructor.GetParameters();
				if (newExpression.Members.Count != parameters.Length)
				{
					return false;
				}
				ResourceSetExpression resourceSetExpression = input.Source as ResourceSetExpression;
				int num = -1;
				ParameterExpression parameterExpression = resultSelector.Parameters[0];
				ParameterExpression parameterExpression2 = resultSelector.Parameters[1];
				MemberInfo[] array = new MemberInfo[newExpression.Members.Count];
				PropertyInfo[] properties = newExpression.Type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
				Dictionary<string, Expression> dictionary = new Dictionary<string, Expression>(parameters.Length - 1, StringComparer.Ordinal);
				for (int i = 0; i < newExpression.Arguments.Count; i++)
				{
					Expression expression = newExpression.Arguments[i];
					MemberInfo member = newExpression.Members[i];
					if (!ExpressionIsSimpleAccess(expression, resultSelector.Parameters))
					{
						return false;
					}
					if (member.MemberType == MemberTypes.Method)
					{
						member = (from property in properties
						where property.GetGetMethod() == member
						select property).FirstOrDefault();
						if (member == null)
						{
							return false;
						}
					}
					if (member.Name != parameters[i].Name)
					{
						return false;
					}
					array[i] = member;
					ParameterExpression parameterExpression3 = StripTo<ParameterExpression>(expression);
					if (parameterExpression2 == parameterExpression3)
					{
						if (num != -1)
						{
							return false;
						}
						num = i;
						continue;
					}
					if (parameterExpression == parameterExpression3)
					{
						dictionary[member.Name] = resourceSetExpression.CreateReference();
						continue;
					}
					List<ResourceExpression> list = new List<ResourceExpression>();
					InputBinder.Bind(expression, resourceSetExpression, resultSelector.Parameters[0], list);
					if (list.Count != 1)
					{
						return false;
					}
					dictionary[member.Name] = list[0].CreateReference();
				}
				if (num == -1)
				{
					return false;
				}
				string name = array[num].Name;
				transparentScope = new ResourceSetExpression.TransparentAccessors(name, dictionary);
				return true;
			}

			internal static bool MatchPropertyProjectionSet(ResourceExpression input, Expression potentialPropertyRef, out MemberExpression navigationMember)
			{
				return MatchNavigationPropertyProjection(input, potentialPropertyRef, requireSet: true, out navigationMember);
			}

			internal static bool MatchPropertyProjectionSingleton(ResourceExpression input, Expression potentialPropertyRef, out MemberExpression navigationMember)
			{
				return MatchNavigationPropertyProjection(input, potentialPropertyRef, requireSet: false, out navigationMember);
			}

			private static bool MatchNavigationPropertyProjection(ResourceExpression input, Expression potentialPropertyRef, bool requireSet, out MemberExpression navigationMember)
			{
				Expression instance;
				if (MatchNonSingletonProperty(potentialPropertyRef) == requireSet && MatchPropertyAccess(potentialPropertyRef, out navigationMember, out instance, out List<string> _) && instance == input.CreateReference())
				{
					return true;
				}
				navigationMember = null;
				return false;
			}

			internal static bool MatchMemberInitExpressionWithDefaultConstructor(Expression source, LambdaExpression e)
			{
				MemberInitExpression memberInitExpression = StripTo<MemberInitExpression>(e.Body);
				if (MatchResource(source, out ResourceExpression _) && memberInitExpression != null)
				{
					return memberInitExpression.NewExpression.Arguments.Count == 0;
				}
				return false;
			}

			internal static bool MatchNewExpression(Expression source, LambdaExpression e)
			{
				if (MatchResource(source, out ResourceExpression _))
				{
					return e.Body is NewExpression;
				}
				return false;
			}

			internal static bool MatchNot(Expression expression)
			{
				return expression.NodeType == ExpressionType.Not;
			}

			internal static bool MatchNonSingletonProperty(Expression e)
			{
				if (TypeSystem.FindIEnumerable(e.Type) != null && e.Type != typeof(char[]))
				{
					return e.Type != typeof(byte[]);
				}
				return false;
			}

			internal static MatchNullCheckResult MatchNullCheck(Expression entityInScope, ConditionalExpression conditional)
			{
				MatchNullCheckResult result = default(MatchNullCheckResult);
				MatchEqualityCheckResult matchEqualityCheckResult = MatchEquality(conditional.Test);
				if (!matchEqualityCheckResult.Match)
				{
					return result;
				}
				Expression expression;
				if (matchEqualityCheckResult.EqualityYieldsTrue)
				{
					if (!MatchNullConstant(conditional.IfTrue))
					{
						return result;
					}
					expression = conditional.IfFalse;
				}
				else
				{
					if (!MatchNullConstant(conditional.IfFalse))
					{
						return result;
					}
					expression = conditional.IfTrue;
				}
				Expression expression2;
				if (MatchNullConstant(matchEqualityCheckResult.TestLeft))
				{
					expression2 = matchEqualityCheckResult.TestRight;
				}
				else
				{
					if (!MatchNullConstant(matchEqualityCheckResult.TestRight))
					{
						return result;
					}
					expression2 = matchEqualityCheckResult.TestLeft;
				}
				MemberAssignmentAnalysis memberAssignmentAnalysis = MemberAssignmentAnalysis.Analyze(entityInScope, expression);
				if (memberAssignmentAnalysis.MultiplePathsFound)
				{
					return result;
				}
				MemberAssignmentAnalysis memberAssignmentAnalysis2 = MemberAssignmentAnalysis.Analyze(entityInScope, expression2);
				if (memberAssignmentAnalysis2.MultiplePathsFound)
				{
					return result;
				}
				Expression[] expressionsToTargetEntity = memberAssignmentAnalysis.GetExpressionsToTargetEntity();
				Expression[] expressionsToTargetEntity2 = memberAssignmentAnalysis2.GetExpressionsToTargetEntity();
				if (expressionsToTargetEntity2.Length > expressionsToTargetEntity.Length)
				{
					return result;
				}
				for (int i = 0; i < expressionsToTargetEntity2.Length; i++)
				{
					Expression expression3 = expressionsToTargetEntity[i];
					Expression expression4 = expressionsToTargetEntity2[i];
					if (expression3 != expression4)
					{
						if (expression3.NodeType != expression4.NodeType || expression3.NodeType != ExpressionType.MemberAccess)
						{
							return result;
						}
						if (((MemberExpression)expression3).Member != ((MemberExpression)expression4).Member)
						{
							return result;
						}
					}
				}
				result.AssignExpression = expression;
				result.Match = true;
				result.TestToNullExpression = expression2;
				return result;
			}

			internal static bool MatchNullConstant(Expression expression)
			{
				ConstantExpression constantExpression = expression as ConstantExpression;
				if (constantExpression != null && constantExpression.Value == null)
				{
					return true;
				}
				return false;
			}

			internal static bool MatchBinaryExpression(Expression e)
			{
				return e is BinaryExpression;
			}

			internal static bool MatchBinaryEquality(Expression e)
			{
				if (MatchBinaryExpression(e))
				{
					return ((BinaryExpression)e).NodeType == ExpressionType.Equal;
				}
				return false;
			}

			internal static bool MatchStringAddition(Expression e)
			{
				if (e.NodeType == ExpressionType.Add)
				{
					BinaryExpression binaryExpression = e as BinaryExpression;
					if (binaryExpression != null && binaryExpression.Left.Type == typeof(string))
					{
						return binaryExpression.Right.Type == typeof(string);
					}
					return false;
				}
				return false;
			}

			internal static MatchEqualityCheckResult MatchEquality(Expression expression)
			{
				MatchEqualityCheckResult matchEqualityCheckResult = default(MatchEqualityCheckResult);
				matchEqualityCheckResult.Match = false;
				matchEqualityCheckResult.EqualityYieldsTrue = true;
				while (true)
				{
					if (MatchReferenceEquals(expression))
					{
						MethodCallExpression methodCallExpression = (MethodCallExpression)expression;
						matchEqualityCheckResult.Match = true;
						matchEqualityCheckResult.TestLeft = methodCallExpression.Arguments[0];
						matchEqualityCheckResult.TestRight = methodCallExpression.Arguments[1];
						break;
					}
					if (MatchNot(expression))
					{
						matchEqualityCheckResult.EqualityYieldsTrue = !matchEqualityCheckResult.EqualityYieldsTrue;
						expression = ((UnaryExpression)expression).Operand;
						continue;
					}
					BinaryExpression binaryExpression = expression as BinaryExpression;
					if (binaryExpression != null)
					{
						if (binaryExpression.NodeType == ExpressionType.NotEqual)
						{
							matchEqualityCheckResult.EqualityYieldsTrue = !matchEqualityCheckResult.EqualityYieldsTrue;
						}
						else if (binaryExpression.NodeType != ExpressionType.Equal)
						{
							break;
						}
						matchEqualityCheckResult.TestLeft = binaryExpression.Left;
						matchEqualityCheckResult.TestRight = binaryExpression.Right;
						matchEqualityCheckResult.Match = true;
					}
					break;
				}
				return matchEqualityCheckResult;
			}

			private static bool ExpressionIsSimpleAccess(Expression argument, ReadOnlyCollection<ParameterExpression> expressions)
			{
				Expression expression = argument;
				MemberExpression memberExpression;
				do
				{
					memberExpression = (expression as MemberExpression);
					if (memberExpression != null)
					{
						expression = memberExpression.Expression;
					}
				}
				while (memberExpression != null);
				ParameterExpression parameterExpression = expression as ParameterExpression;
				if (parameterExpression == null)
				{
					return false;
				}
				return expressions.Contains(parameterExpression);
			}

			private static bool MatchNaryLambda(Expression expression, int parameterCount, out LambdaExpression lambda)
			{
				lambda = null;
				LambdaExpression lambdaExpression = StripTo<LambdaExpression>(expression);
				if (lambdaExpression != null && lambdaExpression.Parameters.Count == parameterCount)
				{
					lambda = lambdaExpression;
				}
				return lambda != null;
			}
		}

		private static class ValidationRules
		{
			internal static void RequireCanNavigate(Expression e)
			{
				ResourceSetExpression resourceSetExpression = e as ResourceSetExpression;
				if (resourceSetExpression != null && resourceSetExpression.HasSequenceQueryOptions)
				{
					throw new NotSupportedException("Can only specify query options (orderby, where, take, skip) after last navigation.");
				}
				if (PatternRules.MatchResource(e, out ResourceExpression resource) && resource.Projection != null)
				{
					throw new NotSupportedException("Can only specify query options (orderby, where, take, skip) after last navigation.");
				}
			}

			internal static void RequireCanProject(Expression e)
			{
				ResourceExpression resource = (ResourceExpression)e;
				if (!PatternRules.MatchResource(e, out resource))
				{
					throw new NotSupportedException("Can only project the last entity type in the query being translated.");
				}
				if (resource.Projection != null)
				{
					throw new NotSupportedException("Cannot translate multiple Linq Select operations in a single 'select' query option.");
				}
				if (resource.ExpandPaths.Count > 0)
				{
					throw new NotSupportedException("Cannot create projection while there is an explicit expansion specified on the same query.");
				}
			}

			internal static void RequireCanAddCount(Expression e)
			{
				ResourceExpression resource = (ResourceExpression)e;
				if (!PatternRules.MatchResource(e, out resource))
				{
					throw new NotSupportedException("Cannot add count option to the resource set.");
				}
				if (resource.CountOption != 0)
				{
					throw new NotSupportedException("Cannot add count option to the resource set because it would conflict with existing count options.");
				}
			}

			internal static void RequireNonSingleton(Expression e)
			{
				ResourceExpression resourceExpression = e as ResourceExpression;
				if (resourceExpression != null && resourceExpression.IsSingleton)
				{
					throw new NotSupportedException("Cannot specify query options (orderby, where, take, skip) on single resource.");
				}
			}
		}

		private sealed class PropertyInfoEqualityComparer : IEqualityComparer<PropertyInfo>
		{
			internal static readonly PropertyInfoEqualityComparer Instance = new PropertyInfoEqualityComparer();

			private PropertyInfoEqualityComparer()
			{
			}

			public bool Equals(PropertyInfo left, PropertyInfo right)
			{
				if ((object)left == right)
				{
					return true;
				}
				if (null == left || null == right)
				{
					return false;
				}
				if ((object)left.DeclaringType == right.DeclaringType)
				{
					return left.Name.Equals(right.Name);
				}
				return false;
			}

			public int GetHashCode(PropertyInfo obj)
			{
				if (!(null != obj))
				{
					return 0;
				}
				return obj.GetHashCode();
			}
		}

		private sealed class ExpressionPresenceVisitor : DataServiceALinqExpressionVisitor
		{
			private readonly Expression target;

			private bool found;

			private ExpressionPresenceVisitor(Expression target)
			{
				this.target = target;
			}

			internal static bool IsExpressionPresent(Expression target, Expression tree)
			{
				ExpressionPresenceVisitor expressionPresenceVisitor = new ExpressionPresenceVisitor(target);
				expressionPresenceVisitor.Visit(tree);
				return expressionPresenceVisitor.found;
			}

			internal override Expression Visit(Expression exp)
			{
				if (found || target == exp)
				{
					found = true;
					return exp;
				}
				return base.Visit(exp);
			}
		}

		internal static Expression Bind(Expression e)
		{
			Expression expression = new ResourceBinder().Visit(e);
			VerifyKeyPredicates(expression);
			VerifyNotSelectManyProjection(expression);
			return expression;
		}

		internal static bool IsMissingKeyPredicates(Expression expression)
		{
			ResourceExpression resourceExpression = expression as ResourceExpression;
			if (resourceExpression != null)
			{
				if (IsMissingKeyPredicates(resourceExpression.Source))
				{
					return true;
				}
				if (resourceExpression.Source != null)
				{
					ResourceSetExpression resourceSetExpression = resourceExpression.Source as ResourceSetExpression;
					if (resourceSetExpression != null && !resourceSetExpression.HasKeyPredicate)
					{
						return true;
					}
				}
			}
			return false;
		}

		internal static void VerifyKeyPredicates(Expression e)
		{
			if (IsMissingKeyPredicates(e))
			{
				throw new NotSupportedException("Navigation properties can only be selected from a single resource. Specify a key predicate to restrict the entity set to a single instance.");
			}
		}

		internal static void VerifyNotSelectManyProjection(Expression expression)
		{
			ResourceSetExpression resourceSetExpression = expression as ResourceSetExpression;
			if (resourceSetExpression == null)
			{
				return;
			}
			ProjectionQueryOptionExpression projection = resourceSetExpression.Projection;
			if (projection != null)
			{
				MethodCallExpression methodCallExpression = StripTo<MethodCallExpression>(projection.Selector.Body);
				if (methodCallExpression != null && methodCallExpression.Method.Name == "SelectMany")
				{
					throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "The expression type {0} is not supported.", methodCallExpression));
				}
			}
			else if (resourceSetExpression.HasTransparentScope)
			{
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "The expression type {0} is not supported.", resourceSetExpression));
			}
		}

		private static Expression AnalyzePredicate(MethodCallExpression mce)
		{
			if (!TryGetResourceSetMethodArguments(mce, out ResourceSetExpression input, out LambdaExpression lambda))
			{
				ValidationRules.RequireNonSingleton(mce.Arguments[0]);
				return mce;
			}
			List<Expression> list = new List<Expression>();
			AddConjuncts(lambda.Body, list);
			Dictionary<ResourceSetExpression, List<Expression>> dictionary = new Dictionary<ResourceSetExpression, List<Expression>>(ReferenceEqualityComparer<ResourceSetExpression>.Instance);
			List<ResourceExpression> list2 = new List<ResourceExpression>();
			foreach (Expression item3 in list)
			{
				Expression item = InputBinder.Bind(item3, input, lambda.Parameters[0], list2);
				if (list2.Count > 1)
				{
					return mce;
				}
				ResourceSetExpression resourceSetExpression = (list2.Count == 0) ? input : (list2[0] as ResourceSetExpression);
				if (resourceSetExpression == null)
				{
					return mce;
				}
				List<Expression> value = null;
				if (!dictionary.TryGetValue(resourceSetExpression, out value))
				{
					value = (dictionary[resourceSetExpression] = new List<Expression>());
				}
				value.Add(item);
				list2.Clear();
			}
			list = null;
			if (dictionary.TryGetValue(input, out List<Expression> value2))
			{
				dictionary.Remove(input);
			}
			else
			{
				value2 = null;
			}
			if (value2 != null && value2.Count > 0)
			{
				if (input.KeyPredicate != null)
				{
					Expression item2 = BuildKeyPredicateFilter(input.CreateReference(), input.KeyPredicate);
					value2.Add(item2);
					input.KeyPredicate = null;
				}
				int num;
				Expression expression;
				if (input.Filter != null)
				{
					num = 0;
					expression = input.Filter.Predicate;
				}
				else
				{
					num = 1;
					expression = value2[0];
				}
				for (int i = num; i < value2.Count; i++)
				{
					expression = Expression.And(expression, value2[i]);
				}
				AddSequenceQueryOption(input, new FilterQueryOptionExpression(mce.Method.ReturnType, expression));
			}
			return input;
		}

		private static Expression BuildKeyPredicateFilter(InputReferenceExpression input, Dictionary<PropertyInfo, ConstantExpression> keyValuesDictionary)
		{
			Expression expression = null;
			foreach (KeyValuePair<PropertyInfo, ConstantExpression> item in keyValuesDictionary)
			{
				Expression expression2 = Expression.Equal(Expression.Property(input, item.Key), item.Value);
				expression = ((expression != null) ? Expression.And(expression, expression2) : expression2);
			}
			return expression;
		}

		private static void AddConjuncts(Expression e, List<Expression> conjuncts)
		{
			if (PatternRules.MatchAnd(e))
			{
				BinaryExpression obj = (BinaryExpression)e;
				AddConjuncts(obj.Left, conjuncts);
				AddConjuncts(obj.Right, conjuncts);
			}
			else
			{
				conjuncts.Add(e);
			}
		}

		internal bool AnalyzeProjection(MethodCallExpression mce, SequenceMethod sequenceMethod, out Expression e)
		{
			e = mce;
			bool matchMembers = true;
			ResourceExpression resourceExpression = Visit(mce.Arguments[0]) as ResourceExpression;
			if (resourceExpression == null)
			{
				return false;
			}
			if (sequenceMethod == SequenceMethod.SelectManyResultSelector)
			{
				Expression expression = mce.Arguments[1];
				if (!PatternRules.MatchParameterMemberAccess(expression))
				{
					return false;
				}
				if (!PatternRules.MatchDoubleArgumentLambda(mce.Arguments[2], out LambdaExpression lambda))
				{
					return false;
				}
				if (ExpressionPresenceVisitor.IsExpressionPresent(lambda.Parameters[0], lambda.Body))
				{
					return false;
				}
				List<ResourceExpression> referencedInputs = new List<ResourceExpression>();
				LambdaExpression lambdaExpression = StripTo<LambdaExpression>(expression);
				Expression expression2 = InputBinder.Bind(lambdaExpression.Body, resourceExpression, lambdaExpression.Parameters[0], referencedInputs);
				expression2 = StripCastMethodCalls(expression2);
				if (!PatternRules.MatchPropertyProjectionSet(resourceExpression, expression2, out MemberExpression navigationMember))
				{
					return false;
				}
				expression2 = navigationMember;
				ResourceExpression resourceExpression2 = CreateResourceSetExpression(mce.Method.ReturnType, resourceExpression, expression2, TypeSystem.GetElementType(expression2.Type));
				if (!PatternRules.MatchMemberInitExpressionWithDefaultConstructor(resourceExpression2, lambda) && !PatternRules.MatchNewExpression(resourceExpression2, lambda))
				{
					return false;
				}
				lambda = Expression.Lambda(lambda.Body, lambda.Parameters[1]);
				ResourceExpression resourceExpression3 = resourceExpression2.CreateCloneWithNewType(mce.Type);
				bool flag;
				try
				{
					flag = ProjectionAnalyzer.Analyze(lambda, resourceExpression3, matchMembers: false);
				}
				catch (NotSupportedException)
				{
					flag = false;
				}
				if (!flag)
				{
					return false;
				}
				e = resourceExpression3;
				ValidationRules.RequireCanProject(resourceExpression2);
			}
			else
			{
				if (!PatternRules.MatchSingleArgumentLambda(mce.Arguments[1], out LambdaExpression lambda2))
				{
					return false;
				}
				lambda2 = ProjectionRewriter.TryToRewrite(lambda2, resourceExpression.ResourceType);
				ResourceExpression resourceExpression4 = resourceExpression.CreateCloneWithNewType(mce.Type);
				if (!ProjectionAnalyzer.Analyze(lambda2, resourceExpression4, matchMembers))
				{
					return false;
				}
				ValidationRules.RequireCanProject(resourceExpression);
				e = resourceExpression4;
			}
			return true;
		}

		internal static Expression AnalyzeNavigation(MethodCallExpression mce)
		{
			Expression expression = mce.Arguments[0];
			if (!PatternRules.MatchSingleArgumentLambda(mce.Arguments[1], out LambdaExpression lambda))
			{
				return mce;
			}
			if (PatternRules.MatchIdentitySelector(lambda))
			{
				return expression;
			}
			if (PatternRules.MatchTransparentIdentitySelector(expression, lambda))
			{
				return RemoveTransparentScope(mce.Method.ReturnType, (ResourceSetExpression)expression);
			}
			ResourceExpression sourceExpression;
			Expression bound;
			if (IsValidNavigationSource(expression, out sourceExpression) && TryBindToInput(sourceExpression, lambda, out bound) && PatternRules.MatchPropertyProjectionSingleton(sourceExpression, bound, out MemberExpression navigationMember))
			{
				bound = navigationMember;
				return CreateNavigationPropertySingletonExpression(mce.Method.ReturnType, sourceExpression, bound);
			}
			return mce;
		}

		private static bool IsValidNavigationSource(Expression input, out ResourceExpression sourceExpression)
		{
			ValidationRules.RequireCanNavigate(input);
			sourceExpression = (input as ResourceExpression);
			return sourceExpression != null;
		}

		private static Expression LimitCardinality(MethodCallExpression mce, int maxCardinality)
		{
			if (mce.Arguments.Count != 1)
			{
				return mce;
			}
			ResourceSetExpression resourceSetExpression = mce.Arguments[0] as ResourceSetExpression;
			if (resourceSetExpression != null)
			{
				if (!resourceSetExpression.HasKeyPredicate && resourceSetExpression.NodeType != (ExpressionType)10001 && (resourceSetExpression.Take == null || (int)resourceSetExpression.Take.TakeAmount.Value > maxCardinality))
				{
					AddSequenceQueryOption(resourceSetExpression, new TakeQueryOptionExpression(mce.Type, Expression.Constant(maxCardinality)));
				}
				return mce.Arguments[0];
			}
			if (mce.Arguments[0] is NavigationPropertySingletonExpression)
			{
				return mce.Arguments[0];
			}
			return mce;
		}

		private static Expression AnalyzeCast(MethodCallExpression mce)
		{
			ResourceExpression resourceExpression = mce.Arguments[0] as ResourceExpression;
			if (resourceExpression != null)
			{
				return resourceExpression.CreateCloneWithNewType(mce.Method.ReturnType);
			}
			return mce;
		}

		private static ResourceSetExpression CreateResourceSetExpression(Type type, ResourceExpression source, Expression memberExpression, Type resourceType)
		{
			Type elementType = TypeSystem.GetElementType(type);
			ResourceSetExpression result = new ResourceSetExpression(typeof(IOrderedQueryable<>).MakeGenericType(elementType), source, memberExpression, resourceType, source.ExpandPaths.ToList(), source.CountOption, source.CustomQueryOptions.ToDictionary((KeyValuePair<ConstantExpression, ConstantExpression> kvp) => kvp.Key, (KeyValuePair<ConstantExpression, ConstantExpression> kvp) => kvp.Value), null);
			source.ExpandPaths.Clear();
			source.CountOption = CountOption.None;
			source.CustomQueryOptions.Clear();
			return result;
		}

		private static NavigationPropertySingletonExpression CreateNavigationPropertySingletonExpression(Type type, ResourceExpression source, Expression memberExpression)
		{
			NavigationPropertySingletonExpression result = new NavigationPropertySingletonExpression(type, source, memberExpression, memberExpression.Type, source.ExpandPaths.ToList(), source.CountOption, source.CustomQueryOptions.ToDictionary((KeyValuePair<ConstantExpression, ConstantExpression> kvp) => kvp.Key, (KeyValuePair<ConstantExpression, ConstantExpression> kvp) => kvp.Value), null);
			source.ExpandPaths.Clear();
			source.CountOption = CountOption.None;
			source.CustomQueryOptions.Clear();
			return result;
		}

		private static ResourceSetExpression RemoveTransparentScope(Type expectedResultType, ResourceSetExpression input)
		{
			ResourceSetExpression resourceSetExpression = new ResourceSetExpression(expectedResultType, input.Source, input.MemberExpression, input.ResourceType, input.ExpandPaths, input.CountOption, input.CustomQueryOptions, input.Projection);
			resourceSetExpression.KeyPredicate = input.KeyPredicate;
			foreach (QueryOptionExpression sequenceQueryOption in input.SequenceQueryOptions)
			{
				resourceSetExpression.AddSequenceQueryOption(sequenceQueryOption);
			}
			resourceSetExpression.OverrideInputReference(input);
			return resourceSetExpression;
		}

		internal static Expression StripConvertToAssignable(Expression e)
		{
			UnaryExpression unaryExpression = e as UnaryExpression;
			if (unaryExpression != null && PatternRules.MatchConvertToAssignable(unaryExpression))
			{
				return unaryExpression.Operand;
			}
			return e;
		}

		internal static T StripTo<T>(Expression expression) where T : Expression
		{
			Expression expression2;
			do
			{
				expression2 = expression;
				expression = ((expression.NodeType == ExpressionType.Quote) ? ((UnaryExpression)expression).Operand : expression);
				expression = StripConvertToAssignable(expression);
			}
			while (expression2 != expression);
			return expression2 as T;
		}

		internal override Expression VisitResourceSetExpression(ResourceSetExpression rse)
		{
			if (rse.NodeType == (ExpressionType)10000)
			{
				return new ResourceSetExpression(rse.Type, rse.Source, rse.MemberExpression, rse.ResourceType, null, CountOption.None, null, null);
			}
			return rse;
		}

		private static bool TryGetResourceSetMethodArguments(MethodCallExpression mce, out ResourceSetExpression input, out LambdaExpression lambda)
		{
			input = null;
			lambda = null;
			input = (mce.Arguments[0] as ResourceSetExpression);
			if (input != null && PatternRules.MatchSingleArgumentLambda(mce.Arguments[1], out lambda))
			{
				return true;
			}
			return false;
		}

		private static bool TryBindToInput(ResourceExpression input, LambdaExpression le, out Expression bound)
		{
			List<ResourceExpression> list = new List<ResourceExpression>();
			bound = InputBinder.Bind(le.Body, input, le.Parameters[0], list);
			if (list.Count > 1 || (list.Count == 1 && list[0] != input))
			{
				bound = null;
			}
			return bound != null;
		}

		private static Expression AnalyzeResourceSetConstantMethod(MethodCallExpression mce, Func<MethodCallExpression, ResourceExpression, ConstantExpression, Expression> constantMethodAnalyzer)
		{
			ResourceExpression arg = (ResourceExpression)mce.Arguments[0];
			ConstantExpression constantExpression = StripTo<ConstantExpression>(mce.Arguments[1]);
			if (constantExpression == null)
			{
				return mce;
			}
			return constantMethodAnalyzer(mce, arg, constantExpression);
		}

		private static Expression AnalyzeCountMethod(MethodCallExpression mce)
		{
			ResourceExpression resourceExpression = (ResourceExpression)mce.Arguments[0];
			if (resourceExpression == null)
			{
				return mce;
			}
			ValidationRules.RequireCanAddCount(resourceExpression);
			ValidationRules.RequireNonSingleton(resourceExpression);
			resourceExpression.CountOption = CountOption.ValueOnly;
			return resourceExpression;
		}

		private static void AddSequenceQueryOption(ResourceExpression target, QueryOptionExpression qoe)
		{
			ValidationRules.RequireNonSingleton(target);
			ResourceSetExpression resourceSetExpression = (ResourceSetExpression)target;
			if (qoe.NodeType == (ExpressionType)10006)
			{
				if (resourceSetExpression.Take != null)
				{
					throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "The {0} query option cannot be specified after the {1} query option.", "filter", "top"));
				}
				if (resourceSetExpression.Projection != null)
				{
					throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "The {0} query option cannot be specified after the {1} query option.", "filter", "select"));
				}
			}
			resourceSetExpression.AddSequenceQueryOption(qoe);
		}

		internal override Expression VisitBinary(BinaryExpression b)
		{
			Expression expression = base.VisitBinary(b);
			if (PatternRules.MatchStringAddition(expression))
			{
				BinaryExpression binaryExpression = StripTo<BinaryExpression>(expression);
				return Expression.Call(typeof(string).GetMethod("Concat", new Type[2]
				{
					typeof(string),
					typeof(string)
				}), new Expression[2]
				{
					binaryExpression.Left,
					binaryExpression.Right
				});
			}
			return expression;
		}

		internal override Expression VisitMemberAccess(MemberExpression m)
		{
			Expression expression = base.VisitMemberAccess(m);
			MemberExpression memberExpression = StripTo<MemberExpression>(expression);
			PropertyInfo propInfo;
			if (memberExpression != null && PatternRules.MatchNonPrivateReadableProperty(memberExpression, out propInfo) && TypeSystem.TryGetPropertyAsMethod(propInfo, out MethodInfo mi))
			{
				return Expression.Call(memberExpression.Expression, mi);
			}
			return expression;
		}

		internal override Expression VisitMethodCall(MethodCallExpression mce)
		{
			SequenceMethod sequenceMethod;
			if (ReflectionUtil.TryIdentifySequenceMethod(mce.Method, out sequenceMethod) && (sequenceMethod == SequenceMethod.Select || sequenceMethod == SequenceMethod.SelectManyResultSelector) && AnalyzeProjection(mce, sequenceMethod, out Expression e))
			{
				return e;
			}
			e = base.VisitMethodCall(mce);
			mce = (e as MethodCallExpression);
			if (mce != null)
			{
				if (ReflectionUtil.TryIdentifySequenceMethod(mce.Method, out sequenceMethod))
				{
					switch (sequenceMethod)
					{
					case SequenceMethod.Where:
						return AnalyzePredicate(mce);
					case SequenceMethod.Select:
						return AnalyzeNavigation(mce);
					case SequenceMethod.Take:
						return AnalyzeResourceSetConstantMethod(mce, delegate(MethodCallExpression callExp, ResourceExpression resource, ConstantExpression takeCount)
						{
							AddSequenceQueryOption(resource, new TakeQueryOptionExpression(callExp.Type, takeCount));
							return resource;
						});
					case SequenceMethod.First:
					case SequenceMethod.FirstOrDefault:
						return LimitCardinality(mce, 1);
					case SequenceMethod.Single:
					case SequenceMethod.SingleOrDefault:
						return LimitCardinality(mce, 2);
					case SequenceMethod.Cast:
						return AnalyzeCast(mce);
					case SequenceMethod.Count:
					case SequenceMethod.LongCount:
						return AnalyzeCountMethod(mce);
					default:
						throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "The method '{0}' is not supported", mce.Method.Name));
					}
				}
				if (mce.Method.DeclaringType == typeof(TableQueryableExtensions))
				{
					Type[] genericArguments = mce.Method.GetGenericArguments();
					Type type = genericArguments[0];
					if (mce.Method == TableQueryableExtensions.WithOptionsMethodInfo.MakeGenericMethod(type))
					{
						return AnalyzeResourceSetConstantMethod(mce, delegate(MethodCallExpression callExp, ResourceExpression resource, ConstantExpression options)
						{
							AddSequenceQueryOption(resource, new RequestOptionsQueryOptionExpression(callExp.Type, options));
							return resource;
						});
					}
					if (mce.Method == TableQueryableExtensions.WithContextMethodInfo.MakeGenericMethod(type))
					{
						return AnalyzeResourceSetConstantMethod(mce, delegate(MethodCallExpression callExp, ResourceExpression resource, ConstantExpression ctx)
						{
							AddSequenceQueryOption(resource, new OperationContextQueryOptionExpression(callExp.Type, ctx));
							return resource;
						});
					}
					if (genericArguments.Length > 1 && mce.Method == TableQueryableExtensions.ResolveMethodInfo.MakeGenericMethod(type, genericArguments[1]))
					{
						return AnalyzeResourceSetConstantMethod(mce, delegate(MethodCallExpression callExp, ResourceExpression resource, ConstantExpression resolver)
						{
							AddSequenceQueryOption(resource, new EntityResolverQueryOptionExpression(callExp.Type, resolver));
							return resource;
						});
					}
					throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "The method '{0}' is not supported", mce.Method.Name));
				}
				return mce;
			}
			return e;
		}

		private static Expression StripCastMethodCalls(Expression expression)
		{
			MethodCallExpression methodCallExpression = StripTo<MethodCallExpression>(expression);
			while (methodCallExpression != null && ReflectionUtil.IsSequenceMethod(methodCallExpression.Method, SequenceMethod.Cast))
			{
				expression = methodCallExpression.Arguments[0];
				methodCallExpression = StripTo<MethodCallExpression>(expression);
			}
			return expression;
		}
	}
}
