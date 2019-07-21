using System;

namespace Microsoft.Azure.Cosmos.Table
{
	[Flags]
	public enum SharedAccessAccountResourceTypes
	{
		None = 0x0,
		Service = 0x1,
		Container = 0x2,
		Object = 0x4
	}
}
