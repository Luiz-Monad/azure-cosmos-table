using Microsoft.Azure.Cosmos.Table.RestExecutor.TableCommand;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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

		[DebuggerNonUserCode]
		internal static Task WriteToAsync<T>(this Stream stream, Stream toStream, ExecutionState<T> executionState, CancellationToken cancellationToken)
		{
			return new AsyncStreamCopier<T>(stream, toStream, executionState, GetBufferSize(stream)).StartCopyStream(null, null, cancellationToken);
		}
	}
}
