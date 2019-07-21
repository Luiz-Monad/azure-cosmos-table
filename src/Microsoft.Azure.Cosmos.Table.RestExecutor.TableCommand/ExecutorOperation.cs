namespace Microsoft.Azure.Cosmos.Table.RestExecutor.TableCommand
{
	internal enum ExecutorOperation
	{
		BeginOperation,
		BeginGetResponse,
		EndGetResponse,
		PreProcess,
		GetResponseStream,
		BeginDownloadResponse,
		PostProcess,
		EndOperation
	}
}
