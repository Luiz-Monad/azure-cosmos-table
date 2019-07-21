using System;

namespace Microsoft.Azure.Cosmos.Table
{
	[Flags]
	public enum SharedAccessAccountServices
	{
		None = 0x0,
		Blob = 0x1,
		File = 0x2,
		Queue = 0x4,
		Table = 0x8
	}
}
