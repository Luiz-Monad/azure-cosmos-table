using Microsoft.Azure.Cosmos.Table;

namespace Microsoft.Azure.Cosmos.Tables.SharedFiles
{
	internal class TableOperationWrapper
	{
		public bool IsTableEntity
		{
			get;
			set;
		}

		public TableOperationType OperationType
		{
			get;
			set;
		}
	}
}
