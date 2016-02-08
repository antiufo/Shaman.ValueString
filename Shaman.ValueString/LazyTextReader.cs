using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
namespace Shaman.Runtime
{
	public class LazyTextReader : IDisposable
	{
		private const int blockSize = 16384;
		private TextReader reader;
		private Stream stream;
		private StoppableStream stoppable;
		private List<char[]> data = new List<char[]>();
		private int readChars;
		private Encoding encoding;
		private bool finished;
		public Encoding Encoding
		{
			get
			{
				return this.encoding;
			}
		}
		public int ReadChars
		{
			get
			{
				return this.readChars;
			}
		}
		public virtual char this[int index]
		{
			get
			{
				while (index >= this.readChars)
				{
					int num = this.reader.Read();
					if (num == -1)
					{
						throw new EndOfStreamException();
					}
					this.AppendChar((char)num);
				}
				int index2 = index / 16384;
				return this.data[index2][index % 16384];
			}
		}
		internal LazyTextReader()
		{
		}
		public LazyTextReader(TextReader reader)
		{
			this.reader = reader;
		}
		public LazyTextReader(Stream stream, Encoding initialEncoding)
		{
			this.encoding = initialEncoding;
			this.stream = stream;
			this.stoppable = new StoppableStream(stream);
            this.reader = new StreamReader(this.stoppable, initialEncoding ?? Encoding.UTF8, true, 512);
        }
		public bool TrySetEncoding(Encoding encoding)
		{
			if (encoding == this.encoding)
			{
				return true;
			}
			if (this.stream == null)
			{
				return false;
			}
			this.encoding = encoding;
			this.stoppable.Stop();
			while (true)
			{
				int num = this.reader.Read();
				if (num == -1)
				{
					break;
				}
				this.AppendChar((char)num);
			}
			this.stoppable = new StoppableStream(this.stream);
			this.reader = new StreamReader(this.stoppable, encoding, false);
			return true;
		}
		public bool ContainsIndex(int index)
		{
			while (index >= this.readChars)
			{
				if (this.finished)
				{
					return false;
				}
				int num = this.reader.Read();
				if (num == -1)
				{
					this.finished = false;
					return false;
				}
				this.AppendChar((char)num);
			}
			return true;
		}
		public void ReadToEnd()
		{
			if (this.finished)
			{
				return;
			}
			int num = -1;
			char[] array = null;
			while (true)
			{
				int num2 = this.readChars / 16384;
				if (num2 != num)
				{
					if (this.data.Count <= num2)
					{
						array = new char[16384];
						this.data.Add(array);
					}
					else
					{
						array = this.data[num2];
					}
					num = num2;
				}
				int num3 = this.readChars % 16384;
				int num4 = this.reader.Read(array, num3, Math.Min(16384 - num3, 1024));
				if (num4 == 0)
				{
					break;
				}
				this.readChars += num4;
			}
			this.finished = true;
		}
		private void AppendChar(char ch)
		{
			int num = this.readChars / 16384;
			char[] array;
			if (this.data.Count <= num)
			{
				array = new char[16384];
				this.data.Add(array);
			}
			else
			{
				array = this.data[num];
			}
			array[this.readChars % 16384] = ch;
			this.readChars++;
		}
		public void Dispose()
		{
			if (this.stream != null)
			{
				this.stream.Dispose();
			}
			if (this.reader != null)
			{
				this.reader.Dispose();
			}
		}
		public override string ToString()
		{
			return this.Substring(0, this.readChars);
		}
		public virtual string Substring(int startIndex, int length)
		{
			int num = startIndex / 16384;
			int num2 = (startIndex + length - 1) / 16384;
			if (num == num2)
			{
				return new string(this.data[num], startIndex % 16384, length);
			}
			StringBuilder stringBuilder = new StringBuilder(length);
			int num3 = startIndex % 16384;
			int num4 = num;
			for (int i = 0; i < length; i++)
			{
				stringBuilder.Append(this.data[num4][num3]);
				if (num3 != 16383)
				{
					num3++;
				}
				else
				{
					num4++;
					num3 = 0;
				}
			}
			return stringBuilder.ToString();
		}
	}
}
