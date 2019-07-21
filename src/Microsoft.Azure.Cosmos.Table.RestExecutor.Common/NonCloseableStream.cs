using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.Common
{
	internal class NonCloseableStream : Stream
	{
		private readonly Stream wrappedStream;

		public override bool CanRead => wrappedStream.CanRead;

		public override bool CanSeek => wrappedStream.CanSeek;

		public override bool CanTimeout => wrappedStream.CanTimeout;

		public override bool CanWrite => wrappedStream.CanWrite;

		public override long Length => wrappedStream.Length;

		public override long Position
		{
			get
			{
				return wrappedStream.Position;
			}
			set
			{
				wrappedStream.Position = value;
			}
		}

		public override int ReadTimeout
		{
			get
			{
				return wrappedStream.ReadTimeout;
			}
			set
			{
				wrappedStream.ReadTimeout = value;
			}
		}

		public override int WriteTimeout
		{
			get
			{
				return wrappedStream.WriteTimeout;
			}
			set
			{
				wrappedStream.WriteTimeout = value;
			}
		}

		public NonCloseableStream(Stream wrappedStream)
		{
			CommonUtility.AssertNotNull("WrappedStream", wrappedStream);
			this.wrappedStream = wrappedStream;
		}

		public override void Flush()
		{
			wrappedStream.Flush();
		}

		public override void SetLength(long value)
		{
			wrappedStream.SetLength(value);
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return wrappedStream.Seek(offset, origin);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return wrappedStream.Read(buffer, offset, count);
		}

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return wrappedStream.ReadAsync(buffer, offset, count, cancellationToken);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			wrappedStream.Write(buffer, offset, count);
		}

		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return wrappedStream.WriteAsync(buffer, offset, count, cancellationToken);
		}

		protected override void Dispose(bool disposing)
		{
		}
	}
}
