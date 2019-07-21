using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.Common
{
	internal class MultiBufferMemoryStream : Stream
	{
		private const int DefaultSmallBufferSize = 65536;

		private readonly int bufferSize;

		private List<byte[]> bufferBlocks;

		private long length;

		private long capacity;

		private long position;

		private volatile bool disposed;

		public override bool CanRead => !disposed;

		public override bool CanSeek => !disposed;

		public override bool CanWrite => !disposed;

		public override long Length => length;

		public override long Position
		{
			get
			{
				return position;
			}
			set
			{
				Seek(value, SeekOrigin.Begin);
			}
		}

		internal MultiBufferMemoryStream(int bufferSize = 65536)
		{
			bufferBlocks = new List<byte[]>();
			this.bufferSize = bufferSize;
			if (bufferSize <= 0)
			{
				throw new ArgumentOutOfRangeException("bufferSize", "Buffer size must be a positive, non-zero value");
			}
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			CommonUtility.AssertNotNull("buffer", buffer);
			CommonUtility.AssertInBounds("offset", offset, 0, buffer.Length);
			CommonUtility.AssertInBounds("count", count, 0, buffer.Length - offset);
			return ReadInternal(buffer, offset, count);
		}

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return Task.FromResult(Read(buffer, offset, count));
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			long val;
			switch (origin)
			{
			case SeekOrigin.Begin:
				val = offset;
				break;
			case SeekOrigin.Current:
				val = position + offset;
				break;
			case SeekOrigin.End:
				val = Length + offset;
				break;
			default:
				CommonUtility.ArgumentOutOfRange("origin", origin);
				throw new ArgumentOutOfRangeException("origin");
			}
			CommonUtility.AssertInBounds("offset", val, 0L, Length);
			position = val;
			return position;
		}

		public override void SetLength(long value)
		{
			Reserve(value);
			length = value;
			position = Math.Min(position, length);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			CommonUtility.AssertNotNull("buffer", buffer);
			CommonUtility.AssertInBounds("offset", offset, 0, buffer.Length);
			CommonUtility.AssertInBounds("count", count, 0, buffer.Length - offset);
			if (position + count > capacity)
			{
				Reserve(position + count);
			}
			WriteInternal(buffer, offset, count);
			length = Math.Max(length, position);
		}

		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			Write(buffer, offset, count);
			return Task.FromResult(result: true);
		}

		public override void Flush()
		{
		}

		public async Task FastCopyToAsync(Stream destination, DateTime? expiryTime, CancellationToken token)
		{
			CommonUtility.AssertNotNull("destination", destination);
			long leftToRead = Length - Position;
			try
			{
				while (true)
				{
					if (leftToRead == 0L)
					{
						return;
					}
					if (expiryTime.HasValue && DateTime.Now.CompareTo(expiryTime.Value) > 0)
					{
						break;
					}
					ArraySegment<byte> currentBlock = GetCurrentBlock();
					int blockReadLength = (int)Math.Min(leftToRead, currentBlock.Count);
					await destination.WriteAsync(currentBlock.Array, currentBlock.Offset, blockReadLength, token).ConfigureAwait(continueOnCapturedContext: false);
					AdvancePosition(ref leftToRead, blockReadLength);
				}
				throw new TimeoutException();
			}
			catch (Exception)
			{
				if (expiryTime.HasValue && DateTime.Now.CompareTo(expiryTime.Value) > 0)
				{
					throw new TimeoutException();
				}
				throw;
			}
		}

		public void FastCopyTo(Stream destination, DateTime? expiryTime)
		{
			CommonUtility.AssertNotNull("destination", destination);
			long leftToProcess = Length - Position;
			try
			{
				while (true)
				{
					if (leftToProcess == 0L)
					{
						return;
					}
					if (expiryTime.HasValue && DateTime.Now.CompareTo(expiryTime.Value) > 0)
					{
						break;
					}
					ArraySegment<byte> currentBlock = GetCurrentBlock();
					int num = (int)Math.Min(leftToProcess, currentBlock.Count);
					destination.Write(currentBlock.Array, currentBlock.Offset, num);
					AdvancePosition(ref leftToProcess, num);
				}
				throw new TimeoutException();
			}
			catch (Exception)
			{
				if (expiryTime.HasValue && DateTime.Now.CompareTo(expiryTime.Value) > 0)
				{
					throw new TimeoutException();
				}
				throw;
			}
		}

		private void Reserve(long requiredSize)
		{
			if (requiredSize < 0)
			{
				throw new ArgumentOutOfRangeException("requiredSize", "The size must be positive");
			}
			while (requiredSize > capacity)
			{
				AddBlock();
			}
		}

		private void AddBlock()
		{
			byte[] array = new byte[bufferSize];
			if (array.Length != bufferSize)
			{
				throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "The IBufferManager provided an incorrect length buffer to the stream, Expected {0}, received {1}. Buffer length should equal the value returned by IBufferManager.GetDefaultBufferSize().", bufferSize, array.Length));
			}
			bufferBlocks.Add(array);
			capacity += bufferSize;
		}

		private int ReadInternal(byte[] buffer, int offset, int count)
		{
			int num = (int)Math.Min(Length - Position, count);
			int leftToProcess = num;
			while (leftToProcess != 0)
			{
				ArraySegment<byte> currentBlock = GetCurrentBlock();
				int num2 = Math.Min(leftToProcess, currentBlock.Count);
				Buffer.BlockCopy(currentBlock.Array, currentBlock.Offset, buffer, offset, num2);
				AdvancePosition(ref offset, ref leftToProcess, num2);
			}
			return num;
		}

		private void WriteInternal(byte[] buffer, int offset, int count)
		{
			while (count != 0)
			{
				ArraySegment<byte> currentBlock = GetCurrentBlock();
				int num = Math.Min(count, currentBlock.Count);
				Buffer.BlockCopy(buffer, offset, currentBlock.Array, currentBlock.Offset, num);
				AdvancePosition(ref offset, ref count, num);
			}
		}

		private void AdvancePosition(ref int offset, ref int leftToProcess, int amountProcessed)
		{
			position += amountProcessed;
			offset += amountProcessed;
			leftToProcess -= amountProcessed;
		}

		private void AdvancePosition(ref long leftToProcess, int amountProcessed)
		{
			position += amountProcessed;
			leftToProcess -= amountProcessed;
		}

		private ArraySegment<byte> GetCurrentBlock()
		{
			int index = (int)(position / bufferSize);
			int num = (int)(position % bufferSize);
			byte[] array = bufferBlocks[index];
			return new ArraySegment<byte>(array, num, array.Length - num);
		}

		protected override void Dispose(bool disposing)
		{
			if (!disposed)
			{
				disposed = true;
				if (disposing)
				{
					bufferBlocks.Clear();
				}
			}
			base.Dispose(disposing);
		}
	}
}
