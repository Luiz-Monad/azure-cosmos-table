using Microsoft.Azure.Cosmos.Table.RestExecutor.TableCommand;
using System;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Cosmos.Table.RestExecutor.Utils
{
	internal class AsyncStreamCopier<T> : IDisposable
	{
		private int buffSize;

		private Stream src;

		private Stream dest;

		private CancellationTokenSource cancellationTokenSourceAbort;

		private CancellationTokenSource cancellationTokenSourceTimeout;

		private CancellationTokenSource cancellationTokenSourceCombined;

		private ExecutionState<T> state;

		private Action previousCancellationDelegate;

		private bool disposed;

		public AsyncStreamCopier(Stream src, Stream dest, ExecutionState<T> state, int? buffSize)
		{
			this.src = src;
			this.dest = dest;
			this.state = state;
			this.buffSize = (buffSize ?? 65536);
		}

		public Task StartCopyStream(long? copyLength, long? maxLength, CancellationToken cancellationToken)
		{
			Task task = StartCopyStreamAsync(copyLength, maxLength, cancellationToken);
			task.ContinueWith(delegate(Task completedStreamCopyTask)
			{
				state.CancelDelegate = previousCancellationDelegate;
				if (completedStreamCopyTask.IsFaulted)
				{
					state.ExceptionRef = completedStreamCopyTask.Exception.InnerException;
				}
				else if (completedStreamCopyTask.IsCanceled)
				{
					bool flag = false;
					try
					{
						flag = !cancellationTokenSourceAbort.IsCancellationRequested;
						if (!flag && cancellationTokenSourceTimeout != null)
						{
							cancellationTokenSourceTimeout.Dispose();
						}
					}
					catch (Exception)
					{
					}
					try
					{
						if (state.Req != null)
						{
							try
							{
								state.ReqTimedOut = flag;
							}
							catch (Exception ex2)
							{
								Logger.LogWarning(state.OperationContext, "Aborting the request failed with exception: {0}", ex2);
							}
						}
					}
					catch (Exception)
					{
					}
					state.ExceptionRef = (flag ? Exceptions.GenerateTimeoutException((state.Cmd != null) ? state.Cmd.CurrentResult : null, null) : Exceptions.GenerateCancellationException((state.Cmd != null) ? state.Cmd.CurrentResult : null, null));
				}
				Dispose();
			});
			return task;
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposed && disposing)
			{
				if (cancellationTokenSourceAbort != null)
				{
					cancellationTokenSourceAbort.Dispose();
				}
				if (cancellationTokenSourceTimeout != null)
				{
					cancellationTokenSourceTimeout.Dispose();
					cancellationTokenSourceCombined.Dispose();
				}
				state = null;
			}
			disposed = true;
		}

		public async Task StartCopyStreamAsync(long? copyLength, long? maxLength, CancellationToken cancellationToken)
		{
			cancellationTokenSourceAbort = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			lock (state.CancellationLockerObject)
			{
				previousCancellationDelegate = state.CancelDelegate;
				state.CancelDelegate = cancellationTokenSourceAbort.Cancel;
			}
			if (state.OperationExpiryTime.HasValue)
			{
				cancellationTokenSourceTimeout = new CancellationTokenSource(state.RemainingTimeout);
				cancellationTokenSourceCombined = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSourceAbort.Token, cancellationTokenSourceTimeout.Token);
			}
			else
			{
				cancellationTokenSourceCombined = cancellationTokenSourceAbort;
			}
			await StartCopyStreamAsyncHelper(copyLength, maxLength, cancellationTokenSourceCombined.Token).ConfigureAwait(continueOnCapturedContext: false);
		}

		private async Task StartCopyStreamAsyncHelper(long? copyLength, long? maxLength, CancellationToken token)
		{
			if (copyLength.HasValue && maxLength.HasValue)
			{
				throw new ArgumentException("Cannot specify both copyLength and maxLength.");
			}
			if (src.CanSeek && maxLength.HasValue && src.Length - src.Position > maxLength)
			{
				throw new InvalidOperationException("The length of the stream exceeds the permitted length.");
			}
			if (src.CanSeek && copyLength.HasValue && src.Length - src.Position < copyLength)
			{
				throw new ArgumentOutOfRangeException("copyLength", "The requested number of bytes exceeds the length of the stream remaining from the specified position.");
			}
			token.ThrowIfCancellationRequested();
			byte[] readBuff2 = new byte[buffSize];
			byte[] writeBuff2 = new byte[buffSize];
			int count = CalculateBytesToCopy(copyLength, 0L);
			int num = await src.ReadAsync(readBuff2, 0, count, token).ConfigureAwait(continueOnCapturedContext: false);
			long totalBytes = num;
			CheckMaxLength(maxLength, totalBytes);
			byte[] array = readBuff2;
			readBuff2 = writeBuff2;
			writeBuff2 = array;
			ExceptionDispatchInfo readException = null;
			while (num > 0)
			{
				token.ThrowIfCancellationRequested();
				Task task = dest.WriteAsync(writeBuff2, 0, num, token);
				count = CalculateBytesToCopy(copyLength, totalBytes);
				Task<int> readTask = null;
				if (count > 0)
				{
					try
					{
						readTask = src.ReadAsync(readBuff2, 0, count, token);
					}
					catch (Exception source)
					{
						readException = ExceptionDispatchInfo.Capture(source);
					}
				}
				else
				{
					readTask = Task.FromResult(0);
				}
				await task.ConfigureAwait(continueOnCapturedContext: false);
				readException?.Throw();
				num = await readTask.WithCancellation(token).ConfigureAwait(continueOnCapturedContext: false);
				totalBytes += num;
				CheckMaxLength(maxLength, totalBytes);
				array = readBuff2;
				readBuff2 = writeBuff2;
				writeBuff2 = array;
			}
			if (copyLength.HasValue && totalBytes != copyLength.Value)
			{
				throw new ArgumentOutOfRangeException("copyLength", "The requested number of bytes exceeds the length of the stream remaining from the specified position.");
			}
		}

		private static void CheckMaxLength(long? maxLength, long totalBytes)
		{
			if (maxLength.HasValue && totalBytes > maxLength.Value)
			{
				throw new InvalidOperationException("The length of the stream exceeds the permitted length.");
			}
		}

		private int CalculateBytesToCopy(long? copyLength, long totalBytes)
		{
			int num = buffSize;
			if (copyLength.HasValue)
			{
				if (totalBytes > copyLength.Value)
				{
					throw new InvalidOperationException($"Internal Error - negative copyLength requested when attempting to copy a stream.  CopyLength = {copyLength.Value}, totalBytes = {totalBytes}.");
				}
				num = (int)Math.Min(num, copyLength.Value - totalBytes);
			}
			return num;
		}
	}
}
