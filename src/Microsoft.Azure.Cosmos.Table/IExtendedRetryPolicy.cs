namespace Microsoft.Azure.Cosmos.Table
{
	public interface IExtendedRetryPolicy : IRetryPolicy
	{
		RetryInfo Evaluate(RetryContext retryContext, OperationContext operationContext);
	}
}
