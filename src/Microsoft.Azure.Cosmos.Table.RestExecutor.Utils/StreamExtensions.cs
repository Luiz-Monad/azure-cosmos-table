using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.Utils
{
	internal static class StreamExtensions
	{
		[DebuggerNonUserCode]
		internal static int GetBufferSize(Stream inStream)
		{
			if (inStream.CanSeek && inStream.Length - inStream.Position > 0)
			{
				return (int)Math.Min(inStream.Length - inStream.Position, 65536L);
			}
			return 65536;
		}
	}
}
