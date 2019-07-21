using System;

namespace Microsoft.Azure.Cosmos.Table
{
	[Flags]
	internal enum EntityReadFlags
	{
		PartitionKey = 0x1,
		RowKey = 0x2,
		Timestamp = 0x4,
		Etag = 0x8,
		Properties = 0x10,
		All = 0x1F
	}
}
