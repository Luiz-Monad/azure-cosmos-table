namespace Microsoft.Azure.Cosmos.Table.Queryable
{
	internal enum ResourceExpressionType
	{
		RootResourceSet = 10000,
		ResourceNavigationProperty,
		ResourceNavigationPropertySingleton,
		TakeQueryOption,
		SkipQueryOption,
		OrderByQueryOption,
		FilterQueryOption,
		InputReference,
		ProjectionQueryOption,
		RequestOptions,
		OperationContext,
		Resolver
	}
}
