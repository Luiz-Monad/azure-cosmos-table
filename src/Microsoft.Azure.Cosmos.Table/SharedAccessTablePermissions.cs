using System;

namespace Microsoft.Azure.Cosmos.Table
{
	[Flags]
	public enum SharedAccessTablePermissions
	{
		None = 0x0,
		Query = 0x1,
		Add = 0x2,
		Update = 0x4,
		Delete = 0x8
	}
}
