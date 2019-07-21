using System;

namespace Microsoft.Azure.Cosmos.Table
{
	[Flags]
	public enum CorsHttpMethods
	{
		None = 0x0,
		Get = 0x1,
		Head = 0x2,
		Post = 0x4,
		Put = 0x8,
		Delete = 0x10,
		Trace = 0x20,
		Options = 0x40,
		Connect = 0x80,
		Merge = 0x100
	}
}
