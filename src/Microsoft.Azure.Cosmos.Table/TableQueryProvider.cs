using Microsoft.Azure.Cosmos.Table.Queryable;
using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.Azure.Cosmos.Table
{
	internal class TableQueryProvider : IQueryProvider
	{
		internal CloudTable Table
		{
			get;
			private set;
		}

		public TableQueryProvider(CloudTable table)
		{
			Table = table;
		}

		public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
		{
			CommonUtility.AssertNotNull("expression", expression);
			return new TableQuery<TElement>(expression, this);
		}

		public IQueryable CreateQuery(Expression expression)
		{
			CommonUtility.AssertNotNull("expression", expression);
			Type elementType = TypeSystem.GetElementType(expression.Type);
			Type type = typeof(TableQuery<>).MakeGenericType(elementType);
			object[] arguments = new object[2]
			{
				expression,
				this
			};
			return (IQueryable)ConstructorInvoke(type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[2]
			{
				typeof(Expression),
				typeof(TableQueryProvider)
			}, null), arguments);
		}

		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		public object Execute(Expression expression)
		{
			CommonUtility.AssertNotNull("expression", expression);
			return ReflectionUtil.TableQueryProviderReturnSingletonMethodInfo.MakeGenericMethod(expression.Type).Invoke(this, new object[1]
			{
				expression
			});
		}

		public TResult Execute<TResult>(Expression expression)
		{
			CommonUtility.AssertNotNull("expression", expression);
			return (TResult)ReflectionUtil.TableQueryProviderReturnSingletonMethodInfo.MakeGenericMethod(typeof(TResult)).Invoke(this, new object[1]
			{
				expression
			});
		}

		internal TElement ReturnSingleton<TElement>(Expression expression)
		{
			IQueryable<TElement> source = new TableQuery<TElement>(expression, this);
			MethodCallExpression methodCallExpression = expression as MethodCallExpression;
			if (ReflectionUtil.TryIdentifySequenceMethod(methodCallExpression.Method, out SequenceMethod sequenceMethod))
			{
				switch (sequenceMethod)
				{
				case SequenceMethod.Single:
					return source.AsEnumerable().Single();
				case SequenceMethod.SingleOrDefault:
					return source.AsEnumerable().SingleOrDefault();
				case SequenceMethod.First:
					return source.AsEnumerable().First();
				case SequenceMethod.FirstOrDefault:
					return source.AsEnumerable().FirstOrDefault();
				}
			}
			throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "The method '{0}' is not supported.", methodCallExpression.Method.Name));
		}

		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		internal static object ConstructorInvoke(ConstructorInfo constructor, object[] arguments)
		{
			if (constructor == null)
			{
				throw new MissingMethodException();
			}
			return constructor.Invoke(arguments);
		}
	}
}
