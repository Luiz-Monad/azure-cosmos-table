using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Azure.Cosmos.Table.Queryable
{
	internal sealed class InputBinder : DataServiceALinqExpressionVisitor
	{
		private readonly HashSet<ResourceExpression> referencedInputs = new HashSet<ResourceExpression>(EqualityComparer<ResourceExpression>.Default);

		private readonly ResourceExpression input;

		private readonly ResourceSetExpression inputSet;

		private readonly ParameterExpression inputParameter;

		private InputBinder(ResourceExpression resource, ParameterExpression setReferenceParam)
		{
			input = resource;
			inputSet = (resource as ResourceSetExpression);
			inputParameter = setReferenceParam;
		}

		internal static Expression Bind(Expression e, ResourceExpression currentInput, ParameterExpression inputParameter, List<ResourceExpression> referencedInputs)
		{
			InputBinder inputBinder = new InputBinder(currentInput, inputParameter);
			Expression result = inputBinder.Visit(e);
			referencedInputs.AddRange(inputBinder.referencedInputs);
			return result;
		}

		internal override Expression VisitMemberAccess(MemberExpression m)
		{
			if (inputSet == null || !inputSet.HasTransparentScope)
			{
				return base.VisitMemberAccess(m);
			}
			ParameterExpression parameterExpression = null;
			Stack<PropertyInfo> stack = new Stack<PropertyInfo>();
			MemberExpression memberExpression = m;
			while (memberExpression != null && memberExpression.Member.MemberType == MemberTypes.Property && memberExpression.Expression != null)
			{
				stack.Push((PropertyInfo)memberExpression.Member);
				if (memberExpression.Expression.NodeType == ExpressionType.Parameter)
				{
					parameterExpression = (ParameterExpression)memberExpression.Expression;
				}
				memberExpression = (memberExpression.Expression as MemberExpression);
			}
			if (parameterExpression != inputParameter || stack.Count == 0)
			{
				return m;
			}
			ResourceExpression resource = input;
			ResourceSetExpression resourceSetExpression = inputSet;
			bool flag = false;
			while (stack.Count > 0 && resourceSetExpression != null && resourceSetExpression.HasTransparentScope)
			{
				PropertyInfo propertyInfo = stack.Peek();
				if (propertyInfo.Name.Equals(resourceSetExpression.TransparentScope.Accessor, StringComparison.Ordinal))
				{
					resource = resourceSetExpression;
					stack.Pop();
					flag = true;
					continue;
				}
				if (!resourceSetExpression.TransparentScope.SourceAccessors.TryGetValue(propertyInfo.Name, out Expression value))
				{
					break;
				}
				flag = true;
				stack.Pop();
				InputReferenceExpression inputReferenceExpression = value as InputReferenceExpression;
				if (inputReferenceExpression == null)
				{
					resourceSetExpression = (value as ResourceSetExpression);
					if (resourceSetExpression == null || !resourceSetExpression.HasTransparentScope)
					{
						resource = (ResourceExpression)value;
					}
				}
				else
				{
					resourceSetExpression = (inputReferenceExpression.Target as ResourceSetExpression);
					resource = resourceSetExpression;
				}
			}
			if (!flag)
			{
				return m;
			}
			Expression expression = CreateReference(resource);
			while (stack.Count > 0)
			{
				expression = Expression.Property(expression, stack.Pop());
			}
			return expression;
		}

		internal override Expression VisitParameter(ParameterExpression p)
		{
			if ((inputSet == null || !inputSet.HasTransparentScope) && p == inputParameter)
			{
				return CreateReference(input);
			}
			return base.VisitParameter(p);
		}

		private Expression CreateReference(ResourceExpression resource)
		{
			referencedInputs.Add(resource);
			return resource.CreateReference();
		}
	}
}
