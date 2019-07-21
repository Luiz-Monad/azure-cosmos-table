using System;

namespace Microsoft.Azure.Cosmos.Table
{
	[Flags]
	public enum SharedAccessAccountPermissions
	{
		None = 0x0,
		Read = 0x1,
		Add = 0x2,
		Create = 0x4,
		Update = 0x8,
		ProcessMessages = 0x10,
		Write = 0x20,
		Delete = 0x40,
		List = 0x80
	}
}
