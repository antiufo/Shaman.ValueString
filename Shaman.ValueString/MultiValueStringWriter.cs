using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
#if NET35
using ListValueString = System.Collections.Generic.IList<Shaman.Runtime.ValueString>;
#else
using ListValueString = System.Collections.Generic.IReadOnlyList<Shaman.Runtime.ValueString>;
using System.Text.Utf16;
using System.Text.Utf8;
#endif
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
            set
            {
                blockSize = value;
                this.str = ValueString.AllocateString(blockSize);
            }
        }

        public void DestroyPreviousValueStrings()
        {
            used = 0;
        }

        public ValueString CreateValueString(StringBuilder sb)
        {
            return CreateValueString(sb, 0, sb.Length);
        }

        public void EnsureSpace(int length)
        {
            if (used + length > str.Length)
            {
                str = ValueString.AllocateString(Math.Max(length, blockSize));
                used = 0;
            }
        }

        public unsafe ValueString Concatenate(ListValueString strings)
        {
            if (strings.Count == 0) return ValueString.Empty;
            if (strings.Count == 1) return strings[0];
            var first = strings[0];
            ValueString all;
            if (IsAllFromSameString(strings, out all))
            {
                return all;
            }
            var total = 0;
            foreach (var item in strings)
            {
                total += item._length;
            }
            EnsureSpace(total);
            var m = used;
            foreach (var item in strings)
            {
                CreateValueString(item);
            }
            return new ValueString(str, m, total);
        }
        public unsafe ValueString Concatenate(List<ValueString> strings)
        {
            if (strings.Count == 0) return ValueString.Empty;
            if (strings.Count == 1) return strings[0];
            var first = strings[0];
            ValueString all;
            if (IsAllFromSameString(strings, out all))
            {
                return all;
            }
            var total = 0;
            foreach (var item in strings)
            {
                total += item._length;
            }
            EnsureSpace(total);
            var m = used;
            foreach (var item in strings)
            {
                CreateValueString(item);
            }
            return new ValueString(str, m, total);
        }

        private bool IsAllFromSameString(ListValueString strings, out ValueString all)
        {
            all = default(ValueString);
            int nextStart = -1;
            foreach (var item in strings)
            {
                if (!TryAddComponent(item, ref all, ref nextStart)) return false;
            }
            return true;
        }
        private bool IsAllFromSameString(List<ValueString> strings, out ValueString all)
        {
            all = default(ValueString);
            int nextStart = -1;
            foreach (var item in strings)
            {
                if (!TryAddComponent(item, ref all, ref nextStart)) return false;
            }
            return true;
        }



        private bool TryAddComponent(ValueString item, ref ValueString all, ref int nextStart)
        {
            if (item._length == 0) return true;
            if (all._string == null)
            {
                nextStart = item._start;
                all._start = item._start;
                all._string = item._string;
            }
            else
            {
                if (!object.ReferenceEquals(item._string, all._string)) return false;
                if (item._start != nextStart) return false;
            }
            all._length += item._length;

            nextStart += item._length;
            return true;
        }


        public unsafe ValueString Concatenate(ValueString vs1, ValueString vs2)
        {
            if (object.ReferenceEquals(vs1._string, vs2._string) && vs1._start + vs1._length == vs2._start)
            {
                return new ValueString(vs1._string, vs1._start, vs1._length + vs2._length);
            }
            EnsureSpace(vs1.Length + vs2.Length);
            var m = used;
            CreateValueString(vs1);
            CreateValueString(vs2);
            return new ValueString(str, m, vs1._length + vs2._length);
        }

        public unsafe ValueString CreateValueString(ValueString vs)
        {
            EnsureSpace(vs._length);

            fixed (char* dest = str)
            {
                var destx = dest + used;
                var offset = used;

                fixed (char* source = vs._string)
                {
#if NET35
                    ValueString.CopyChars(source + vs._start, destx, vs._length);
#else
                    Buffer.MemoryCopy((void*)(source + vs._start), destx, vs._length * 2, vs._length * 2);
#endif
                }
                used += vs._length;
                return new ValueString(str, offset, vs._length);
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
            if (length == 0) return ValueString.Empty;
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

        public unsafe ValueString CreateValueStringFromAscii(byte[] arr, int start, int length)
        {
            if (start + length > arr.Length) throw new ArgumentException();
            EnsureSpace(length);
            fixed (char* ptr = str)
            {
                var ptrx = ptr + used;
                fixed (byte* src = arr)
                {
                    var srcx = src;
                    for (int i = 0; i < length; i++)
                    {
                        *ptrx = (char)*srcx;
                        ptrx++;
                        srcx++;

                    }

                    var offset = used;
                    used += length;
                    return new ValueString(str, offset, length);
                }
            }
        }
#if !NET35
        public unsafe ValueString CreateValueStringFromUtf8(byte[] arr, int start, int length)
        {
            if (start + length > arr.Length) throw new ArgumentException();
            EnsureSpace(length * 2);
            fixed (char* ptr = str)
            {
                var ptrx = ptr + used;
                var offset = used;
                foreach (var codePoint in new Utf8String.CodePointEnumerable(arr, start, length))
                {
                    int charsEncoded;
                    if (!Utf16LittleEndianEncoder.TryEncodeCodePoint(codePoint, ptrx, out charsEncoded))
                    {
                        // TODO: Change Exception type
                        throw new Exception("invalid character");
                    }
                    ptrx += charsEncoded;
                }
                var strlen = (int)(ptrx - ptr - used);
                used += strlen;
                return new ValueString(str, offset, strlen);

            }
        }


#endif

    }
}
