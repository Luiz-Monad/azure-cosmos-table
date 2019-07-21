namespace Microsoft.Azure.Cosmos.Table
{
	public enum TableOperationType
	{
		Invalid = -1,
		Insert,
		Delete,
		Replace,
		Merge,
		InsertOrReplace,
		InsertOrMerge,
		Retrieve
	}
}
