using System;

namespace Microsoft.Azure.Cosmos.Table
{
	[Flags]
	public enum LoggingOperations
	{
		None = 0x0,
		Read = 0x1,
		Write = 0x2,
		Delete = 0x4,
		All = 0x7
	}
}
