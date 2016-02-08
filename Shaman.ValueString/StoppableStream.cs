using System;
using System.IO;
using System.Threading;
#if !NET35
using System.Threading.Tasks;
#endif
namespace Shaman.Runtime
{
	internal class StoppableStream : Stream
	{
		private Stream stream;
		public override bool CanRead
		{
			get
			{
				return true;
			}
		}
		public override long Length
		{
			get
			{
				throw new NotSupportedException();
			}
		}
		public override bool CanSeek
		{
			get
			{
				return false;
			}
		}
		public override bool CanWrite
		{
			get
			{
				return false;
			}
		}
		public override long Position
		{
			get
			{
				throw new NotSupportedException();
			}
			set
			{
				throw new NotSupportedException();
			}
		}
		public StoppableStream(Stream stream)
		{
			this.stream = stream;
		}
		public override int Read(byte[] buffer, int offset, int count)
		{
			if (this.stream == null)
			{
				return 0;
			}
			return this.stream.Read(buffer, offset, count);
		}
#if !NET35
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			if (this.stream == null)
			{
				return Task.FromResult<int>(0);
			}
			return this.stream.ReadAsync(buffer, offset, count, cancellationToken);
		}
#endif
		public override int ReadByte()
		{
			if (this.stream == null)
			{
				return -1;
			}
			return base.ReadByte();
		}
		protected override void Dispose(bool disposing)
		{
		}
		public void Stop()
		{
			this.stream = null;
		}
		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}
		public override void Flush()
		{
			throw new NotSupportedException();
		}
		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}
		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}
	}
}
