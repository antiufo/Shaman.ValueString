using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shaman.Runtime
{
    public class MultiValueStringBuilder
    {
        public MultiValueStringBuilder(int blockSize)
        {
            this.blockSize = blockSize;
            this.str = ValueString.AllocateString(blockSize);
        }
        private string str;
        private int blockSize;
        private int used;

        public int BlockSize
        {
            get { return blockSize; }
        }

        public ValueString CreateValueString(StringBuilder sb)
        {
            return CreateValueString(sb, 0, sb.Length);
        }

        private void EnsureSpace(int length)
        {
            if (used + length > str.Length)
            {
                str = ValueString.AllocateString(Math.Max(length, blockSize));
                used = 0;
            }
        }

        public unsafe ValueString CreateValueString(StringBuilder sb, int start, int length)
        {
            if (start + length > sb.Length) throw new ArgumentException();
            EnsureSpace(length);
            fixed (char* ptr = str)
            {
                var offset = used;
                var deleg = ValueString.CopyStringBuilderChars;
                if (deleg != null)
                {
                    deleg(ptr + used, sb, length);
                }
                else
                {
                    var d = ptr + used;
                    for (int i = 0; i < length; i++)
                    {
                        (*d++) = sb[start + i];
                    }
                }
                used += length;
                return new ValueString(str, offset, length);
            }
        }


        public ValueString CreateValueString(char[] arr)
        {
            return CreateValueString(arr, 0, arr.Length);
        }

        public unsafe ValueString CreateValueString(char[] arr, int start, int length)
        {
            if (start + length > arr.Length) throw new ArgumentException();
            EnsureSpace(length);
            fixed (char* ptr = str)
            {
                fixed (char* src = arr)
                {
                    ValueString.CopyChars(ptr + used, src + start, length);
                    var offset = used;
                    used += length;
                    return new ValueString(str, offset, length);
                }
            }
        }


    }
}
