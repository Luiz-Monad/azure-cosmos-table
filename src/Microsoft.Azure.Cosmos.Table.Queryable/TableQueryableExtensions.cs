using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Azure.Cosmos.Table.Queryable
{
	public static class TableQueryableExtensions
	{
		internal static MethodInfo WithOptionsMethodInfo
		{
			get;
			set;
		}

		internal static MethodInfo WithContextMethodInfo
		{
			get;
			set;
		}

		internal static MethodInfo ResolveMethodInfo
		{
			get;
			set;
		}

		static TableQueryableExtensions()
		{
			MethodInfo[] methods = typeof(TableQueryableExtensions).GetMethods(BindingFlags.Static | BindingFlags.Public);
			WithOptionsMethodInfo = (from m in methods
			where m.Name == "WithOptions"
			select m).FirstOrDefault();
			WithContextMethodInfo = (from m in methods
			where m.Name == "WithContext"
			select m).FirstOrDefault();
			ResolveMethodInfo = (from m in methods
			where m.Name == "Resolve"
			select m).FirstOrDefault();
		}

		public static TableQuery<TElement> WithOptions<TElement>(this IQueryable<TElement> query, TableRequestOptions options)
		{
			CommonUtility.AssertNotNull("options", options);
			if (!(query is TableQuery<TElement>))
			{
				throw new NotSupportedException("Query must be a TableQuery<T>");
			}
			return (TableQuery<TElement>)query.Provider.CreateQuery<TElement>(Expression.Call(null, WithOptionsMethodInfo.MakeGenericMethod(typeof(TElement)), new Expression[2]
			{
				query.Expression,
				Expression.Constant(options, typeof(TableRequestOptions))
			}));
		}

		public static TableQuery<TElement> WithContext<TElement>(this IQueryable<TElement> query, OperationContext operationContext)
		{
			CommonUtility.AssertNotNull("operationContext", operationContext);
			if (!(query is TableQuery<TElement>))
			{
				throw new NotSupportedException("Query must be a TableQuery<T>");
			}
			return (TableQuery<TElement>)query.Provider.CreateQuery<TElement>(Expression.Call(null, WithContextMethodInfo.MakeGenericMethod(typeof(TElement)), new Expression[2]
			{
				query.Expression,
				Expression.Constant(operationContext, typeof(OperationContext))
			}));
		}

		public static TableQuery<TResolved> Resolve<TElement, TResolved>(this IQueryable<TElement> query, EntityResolver<TResolved> resolver)
		{
			CommonUtility.AssertNotNull("resolver", resolver);
			if (!(query is TableQuery<TElement>))
			{
				throw new NotSupportedException("Query must be a TableQuery<T>");
			}
			return (TableQuery<TResolved>)query.Provider.CreateQuery<TResolved>(Expression.Call(null, ResolveMethodInfo.MakeGenericMethod(typeof(TElement), typeof(TResolved)), new Expression[2]
			{
				query.Expression,
				Expression.Constant(resolver, typeof(EntityResolver<TResolved>))
			}));
		}

		public static TableQuery<TElement> AsTableQuery<TElement>(this IQueryable<TElement> query)
		{
			TableQuery<TElement> obj = query as TableQuery<TElement>;
			if (obj == null)
			{
				throw new NotSupportedException("Query must be a TableQuery<T>");
			}
			return obj;
		}
	}
}
