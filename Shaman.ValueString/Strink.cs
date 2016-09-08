using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Reflection;

namespace Shaman.Runtime
{

    public static partial class ValueStringExtensions
    {
        public static ValueString AsValueString(this string str)
        {
            return new ValueString(str);
        }
        public static StringBuilder AppendValueString(this StringBuilder sb, ValueString str)
        {
            return sb.AppendValueString(str._string, str._start, str._length);
        }
        public static StringBuilder AppendValueString(this StringBuilder sb, ValueString str, int start, int count)
        {
            return sb.Append(str._string, str._start + start, count);
        }
        public static void WriteValueString(this TextWriter writer, ValueString str)
        {
            var start = str._start;
            var end = start + str._length;
            var text = str._string;
            for (int i = start; i < end; i++)
            {
                writer.Write(text[i]);
            }
        }
        public static void WriteValueStringLine(this TextWriter writer, ValueString str)
        {
            writer.WriteValueString(str);
            writer.WriteLine();
        }



        public static string[] SplitFast(this string str, char ch)
        {
            return SplitFast(str, ch, StringSplitOptions.None);
        }

        public static string[] SplitFast(this string str, char ch, StringSplitOptions options)
        {
            var removeEmpty = (options & StringSplitOptions.RemoveEmptyEntries) != 0;
            var count = 0;
            if (removeEmpty)
            {
                var prevWasSplit = true;
                for (int i = 0; i < str.Length; i++)
                {
                    if (str[i] == ch)
                    {
                        prevWasSplit = true;
                    }
                    else
                    {
                        if (prevWasSplit) count++;
                        prevWasSplit = false;
                    }

                }
            }
            else
            {
                count++;
                for (int i = 0; i < str.Length; i++)
                {
                    if (str[i] == ch)
                    {
                        count++;
                    }
                }
            }
            if (count == 0) return EmptyStringArray;
            var arr = new string[count];
            var index = 0;
            var pos = 0;
            while (index < count)
            {
                var idx = str.IndexOf(ch, pos);
                if (idx == -1) Debug.Assert(index == count - 1);
                var length = idx != -1 ? idx - pos : str.Length - pos;
                if (!removeEmpty || length != 0)
                {
#if PROJJSONBUILD
                    arr[index] = str.Substring(pos, length);
#else
                    arr[index] = str.SubstringCached(pos, length);
#endif
                    index++;
                }
                pos = idx + 1;
            }
            return arr;
        }

        private readonly static string[] EmptyStringArray = new string[0];

    }

    public struct ValueString : IEquatable<ValueString>
    {
        public readonly static ValueString Empty;

        internal string _string;
        internal int _start;
        internal int _length;

        public ValueString(string str)
        {
            this._string = str;
            this._start = 0;
            this._length = str != null ? str.Length : 0;
        }
        public ValueString(string str, int start, int length)
        {
            Debug.Assert(start >= 0);
            Debug.Assert(length >= 0);
            Debug.Assert(start + length <= str.Length);
            this._string = str;
            this._start = start;
            this._length = length;
        }

        public ValueString Substring(int begin, int count)
        {
            var r = new ValueString(_string, _start + begin, count);
            AssertEqual(r.ToClrString(), ToClrString().Substring(begin, count));
            return r;
        }

        [Conditional("NEVER")]
        private static void AssertEqual(string a, string b)
        {
            if (a != b) System.Diagnostics.Debugger.Break();
        }

        [Conditional("NEVER")]
        private static void AssertEqual(int a, int b)
        {
            if (a != b) System.Diagnostics.Debugger.Break();
        }

        [Conditional("NEVER")]
        private static void AssertEqual(bool a, bool b)
        {
            if (a != b) System.Diagnostics.Debugger.Break();
        }

        public override bool Equals(object obj)
        {
            if (obj is ValueString)
            {
                return this.Equals((ValueString)obj);
            }
            return false;
        }

        public static bool operator !=(ValueString a, ValueString b)
        {
            return !(a == b);
        }

        public static bool operator ==(ValueString a, ValueString b)
        {
            if (a.Length != b.Length) return false;
            if (object.ReferenceEquals(a._string, b._string) && a._start == b._start) return true;
            var bb = b._start;
            var aa = a._start;
            var len = a._length;
            for (int i = 0; i < len; i++)
            {
                if (a._string[aa + i] != b._string[bb + i]) return false;
            }
            return true;
        }

        public ValueString Trim()
        {
            var start = 0;
            for (; start < _length; start++)
            {
                if (!char.IsWhiteSpace(this[start])) break;
            }
            if (start == _length) return ValueString.Empty;
            var end = _length - 1;
            for (; end >= 0; end--)
            {
                if (!char.IsWhiteSpace(this[end])) break;
            }
            return this.Substring(start, end + 1 - start);
        }

        unsafe static ValueString()
        {
            AllocateString = length => new string('\0', length);
            CopyChars = CharCopy;
        }



        public static int ParseInt32(ValueString str)
        {
            return Convert.ToInt32(ParseInt64(str));
        }
        public static uint ParseUInt32(ValueString str)
        {
            return Convert.ToUInt32(ParseUInt64(str));
        }

        public static bool TryParseUInt32(ValueString str, out uint value)
        {
            value = 0;
            ulong val;
            if (!TryParseUInt64(str, out val))
            {
                return false;
            }
            if (val > uint.MaxValue) return false;
            value = (uint)val;
            return true;
        }
        public static bool TryParseInt32(ValueString str, out int value)
        {
            value = 0;
            long val;
            if (!TryParseInt64(str, out val))
            {
                return false;
            }
            if (val > int.MaxValue || val < int.MinValue) return false;
            value = (int)val;
            return true;
        }

        public static long ParseInt64(ValueString str)
        {
            long val;
            if (!TryParseInt64(str, out val)) throw new FormatException();
            return val;
        }

        public static bool TryParseInt64(ValueString str, out long value)
        {
            value = 0;
            if (str.Length == 0) return false;
            if (str[0] == '-')
            {
                ulong k;
                if (!TryParseUInt64(str.Substring(1), out k)) return false;
                if (k == 9223372036854775808 /* -long.MinValue */)
                {
                    value = long.MinValue;
                    return true;
                }
                if (k > long.MaxValue) return false;
                value = -(long)k;
                return true;
            }
            else
            {
                ulong k;
                if (!TryParseUInt64(str, out k)) return false;
                if (k > long.MaxValue) return false;
                value = (long)k;
                return true;
            }
        }

        public static ulong ParseUInt64(ValueString str)
        {
            ulong val;
            if (!TryParseUInt64(str, out val)) throw new FormatException();
            return val;
        }

        public static bool TryParseUInt64(ValueString str, out ulong value)
        {
            // Precondition replacement
            if (str.Length < 1)
            {
                value = 0;
                return false;
            }

            value = 0;
            var bytesConsumed = 0;


            for (int byteIndex = 0; byteIndex < str.Length; byteIndex++)
            {
                byte nextByteVal = (byte)((byte)str[byteIndex] - '0');
                if (nextByteVal > 9)
                {
                    if (bytesConsumed == 0)
                    {
                        value = default(ulong);
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else if (value > UInt64.MaxValue / 10) // overflow
                {
                    value = 0;
                    bytesConsumed = 0;
                    return false;
                }
                else if (UInt64.MaxValue - value * 10 < (ulong)(nextByteVal)) // overflow
                {
                    value = 0;
                    bytesConsumed = 0;
                    return false;
                }
                ulong candidate = value * 10 + nextByteVal;

                value = candidate;
                bytesConsumed++;
            }

            return true;
        }




        internal static unsafe void CharCopy(char* dest, char* src, int count)
        {
            // Same rules as for memcpy, but with the premise that 
            // chars can only be aligned to even addresses if their
            // enclosing types are correctly aligned
            if ((((int)(byte*)dest | (int)(byte*)src) & 3) != 0)
            {
                if (((int)(byte*)dest & 2) != 0 && ((int)(byte*)src & 2) != 0 && count > 0)
                {
                    ((short*)dest)[0] = ((short*)src)[0];
                    dest++;
                    src++;
                    count--;
                }
                if ((((int)(byte*)dest | (int)(byte*)src) & 2) != 0)
                {
                    memcpy2((byte*)dest, (byte*)src, count * 2);
                    return;
                }
            }
            memcpy4((byte*)dest, (byte*)src, count * 2);
        }


        static unsafe void memcpy4(byte* dest, byte* src, int size)
        {
            /*while (size >= 32) {
				// using long is better than int and slower than double
				// FIXME: enable this only on correct alignment or on platforms
				// that can tolerate unaligned reads/writes of doubles
				((double*)dest) [0] = ((double*)src) [0];
				((double*)dest) [1] = ((double*)src) [1];
				((double*)dest) [2] = ((double*)src) [2];
				((double*)dest) [3] = ((double*)src) [3];
				dest += 32;
				src += 32;
				size -= 32;
			}*/
            while (size >= 16)
            {
                ((int*)dest)[0] = ((int*)src)[0];
                ((int*)dest)[1] = ((int*)src)[1];
                ((int*)dest)[2] = ((int*)src)[2];
                ((int*)dest)[3] = ((int*)src)[3];
                dest += 16;
                src += 16;
                size -= 16;
            }
            while (size >= 4)
            {
                ((int*)dest)[0] = ((int*)src)[0];
                dest += 4;
                src += 4;
                size -= 4;
            }
            while (size > 0)
            {
                ((byte*)dest)[0] = ((byte*)src)[0];
                dest += 1;
                src += 1;
                --size;
            }
        }
        static unsafe void memcpy2(byte* dest, byte* src, int size)
        {
            while (size >= 8)
            {
                ((short*)dest)[0] = ((short*)src)[0];
                ((short*)dest)[1] = ((short*)src)[1];
                ((short*)dest)[2] = ((short*)src)[2];
                ((short*)dest)[3] = ((short*)src)[3];
                dest += 8;
                src += 8;
                size -= 8;
            }
            while (size >= 2)
            {
                ((short*)dest)[0] = ((short*)src)[0];
                dest += 2;
                src += 2;
                size -= 2;
            }
            if (size > 0)
                ((byte*)dest)[0] = ((byte*)src)[0];
        }

        public static Func<int, string> AllocateString;
        public static CopyCharsDelegate CopyChars;
        public static CopyStringBuilderCharsDelegate CopyStringBuilderChars;
        public static Func<StringBuilder, bool> ShouldKeepReleasedStringBuilder = x => true;


        public unsafe delegate void CopyStringBuilderCharsDelegate(char* destination, StringBuilder sb, int length);
        public unsafe delegate void CopyCharsDelegate(char* dest, char* src, int count);

        public override int GetHashCode()
        {

            int hash1 = (5381 << 16) + 5381;

            int hash2 = hash1;


            var len = _length;

            var s = _start;
            while (len >= 2)
            {
                hash1 = ((hash1 << 5) + hash1 + (hash1 >> 27)) ^ _string[s];
                s++;
                hash2 = ((hash2 << 5) + hash2 + (hash2 >> 27)) ^ _string[s];
                s++;
                len -= 2;
            }
            if (len != 0)
                hash2 = ((hash2 << 5) + hash2 + (hash2 >> 27)) ^ _string[s];

            return hash1 + (hash2 * 1566083941);


        }


        public unsafe static ValueString Concat(params ValueString[] args)
        {
            var totalLength = 0;
            for (int i = 0; i < args.Length; i++)
            {
                totalLength += args[i]._length;
            }
            var str = AllocateString(totalLength);
            fixed (char* ptr = str)
            {
                var p = ptr;
                for (int i = 0; i < args.Length; i++)
                {
                    fixed (char* src = args[i]._string)
                    {
                        CopyChars(p, src + args[i]._start, args[i]._length);
                        p += args[i]._length;
                    }
                }
            }
            return str;
        }

        public unsafe static ValueString Concat(ValueString arg0, ValueString arg1)
        {
            var str = AllocateString(arg0._length + arg1._length);
            fixed (char* ptr = str)
            {
                var p = ptr;
                fixed (char* src = arg0._string)
                {
                    CopyChars(p, src + arg0._start, arg0._length);
                    p += arg0._length;
                }
                fixed (char* src = arg1._string)
                {
                    CopyChars(p, src + arg1._start, arg1._length);
                }
            }
            return str;
        }

        public unsafe static ValueString Concat(ValueString arg0, ValueString arg1, ValueString arg2)
        {
            var str = AllocateString(arg0._length + arg1._length + arg2._length);
            fixed (char* ptr = str)
            {
                var p = ptr;
                fixed (char* src = arg0._string)
                {
                    CopyChars(p, src + arg0._start, arg0._length);
                    p += arg0._length;
                }
                fixed (char* src = arg1._string)
                {
                    CopyChars(p, src + arg1._start, arg1._length);
                    p += arg1._length;
                }
                fixed (char* src = arg2._string)
                {
                    CopyChars(p, src + arg2._start, arg2._length);
                }
            }
            return str;
        }

        public unsafe static ValueString Concat(ValueString arg0, ValueString arg1, ValueString arg2, ValueString arg3)
        {
            var str = AllocateString(arg0._length + arg1._length + arg2._length + arg2._length);
            fixed (char* ptr = str)
            {
                var p = ptr;
                fixed (char* src = arg0._string)
                {
                    CopyChars(p, src + arg0._start, arg0._length);
                    p += arg0._length;
                }
                fixed (char* src = arg1._string)
                {
                    CopyChars(p, src + arg1._start, arg1._length);
                    p += arg1._length;
                }
                fixed (char* src = arg2._string)
                {
                    CopyChars(p, src + arg2._start, arg2._length);
                    p += arg2._length;
                }
                fixed (char* src = arg3._string)
                {
                    CopyChars(p, src + arg3._start, arg3._length);
                    p += arg3._length;
                }
            }
            return str;
        }

        public ValueString Replace(char search, char replacement)
        {
            StringBuilder newsb = null;
            for (int i = 0; i < _length; i++)
            {
                var ch = _string[_start + i];
                if (ch == search && newsb == null)
                {
                    newsb = new StringBuilder(this._length);
                    newsb.Append(_string, _start, i);
                }
                if (newsb != null)
                {
                    newsb.Append(ch == search ? replacement : ch);
                }
            }
            var r = newsb != null ? new ValueString(newsb.ToString()) : this;
            AssertEqual(r.ToClrString(), ToClrString().Replace(search, replacement));
            return r;
        }

        public static explicit operator string (ValueString str)
        {
            return str.ToClrString();
        }

        public static implicit operator ValueString(string str)
        {
            return new ValueString(str);
        }

        public string ToClrString()
        {
            if (_length == 0) return string.Empty;
            if (_start == 0 && _length == _string.Length) return _string;
            var newstr = _string.Substring(_start, _length);
            _string = newstr;
            _start = 0;
            return newstr;
        }

        public ValueString Substring(int p)
        {
            var r = new ValueString(_string, _start + p, _length - p);
            AssertEqual(r.ToClrString(), ToClrString().Substring(p));
            return r;
        }

        private int ImplOffsetToPublicOffset(int index)
        {
            if (index != -1) index -= _start;
            return index;
        }

        public int IndexOf(ValueString substr, int startIndex)
        {
            var r = _string == null ? (substr._length == 0 ? 0 : -1) : ImplOffsetToPublicOffset(_string.IndexOf(substr.ToClrString(), _start + startIndex, _length - startIndex, StringComparison.Ordinal));
            AssertEqual(r, ToClrString().IndexOf(substr.ToClrString(), StringComparison.Ordinal));
            return r;
        }

        public int Length
        {
            get
            {
                return _length;
            }
        }

        public char this[int index]
        {
            get
            {
                return _string[_start + index];
            }
        }
        
        public int IndexOf(char ch)
        {
            return IndexOf(ch, 0, _length);
        }
        
        public int IndexOf(char ch, int start)
        {
            return IndexOf(ch, start, _length - start);
        }

        public int IndexOf(char ch, int start, int count)
        {
            if (_string == null) return -1;
            var r = ImplOffsetToPublicOffset(_string.IndexOf(ch, _start + start, count));
            AssertEqual(r, ToClrString().IndexOf(ch, start, count));
            return r;
        }
        
        public int LastIndexOf(char ch)
        {
            return LastIndexOf(ch, _length - 1, _length);
        }
        public int LastIndexOf(char ch, int start)
        {
            return LastIndexOf(ch, start, start + 1);
        }
        public int LastIndexOf(char ch, int start, int count)
        {
            if (_string == null) return -1;
            var r = ImplOffsetToPublicOffset(_string.LastIndexOf(ch, _start + start, count));
            AssertEqual(r, ToClrString().LastIndexOf(ch, start, count));
            return r;
        }
        

        public int IndexOf(ValueString str)
        {
            return IndexOf(str, 0);
        }

        public int LastIndexOf(ValueString substr)
        {
            return LastIndexOf(substr, _length - 1);
            //// TODO useless allocation
            //var r = ToClrString().LastIndexOf(substr.ToClrString(), _start + _length, _length);
            //AssertEqual(r, ToClrString().LastIndexOf(substr));
            //return r;
        }

        public ValueString Remove(int pos, int count)
        {
            if (count == 0) return this;
            ValueString r;
            if (pos == 0) r = new ValueString(_string, _start + count, _length - count);
            else if (pos + count == _length) r = new ValueString(_string, _start, _length - count);
            else
            {

                var sb = new StringBuilder(_length - count);
                sb.Append(_string, _start, pos);
                sb.Append(_string, _start + pos + count, _length - count - pos);
                r = sb.ToString();
            }
            AssertEqual(r.ToClrString(), ToClrString().Remove(pos, count));
            return r;
        }

        public ValueString Insert(int pos, ValueString str)
        {
            // TODO useless allocation
            var r = ToClrString().Insert(pos, str.ToClrString());

            AssertEqual(r, ToClrString().Insert(pos, str.ToClrString()));
            return r;
        }



        public ValueString[] Split(char ch)
        {
            return Split(ch, StringSplitOptions.None);
        }

        public ValueString[] Split(char ch, StringSplitOptions options)
        {
            ValueString[] result = null;
            Split(ch, options, ref result);
            return result;
        }

        public void Split(char ch, StringSplitOptions options, ref ValueString[] arr)
        {
            var removeEmpty = (options & StringSplitOptions.RemoveEmptyEntries) != 0;
            var count = 0;
            if (removeEmpty)
            {
                var prevWasSplit = true;
                for (int i = 0; i < this.Length; i++)
                {
                    if (this[i] == ch)
                    {
                        prevWasSplit = true;
                    }
                    else
                    {
                        if (prevWasSplit) count++;
                        prevWasSplit = false;
                    }

                }
            }
            else
            {
                count++;
                for (int i = 0; i < this.Length; i++)
                {
                    if (this[i] == ch)
                    {
                        count++;
                    }
                }
            }
            if (count == 0){ arr = EmptyStringArray; return; }
            if (arr == null || arr.Length != count) arr = new ValueString[count];
            var index = 0;
            var pos = 0;
            while (index < count)
            {
                var idx = this.IndexOf(ch, pos);
                if (idx == -1) Debug.Assert(index == count - 1);
                var length = idx != -1 ? idx - pos : this.Length - pos;
                if (!removeEmpty || length != 0)
                {
                    arr[index] = this.Substring(pos, length);
                    index++;
                }
                pos = idx + 1;
            }
        }

        private readonly static ValueString[] EmptyStringArray = new ValueString[0];


        public ValueString Replace(string search, string replacement)
        {
            StringBuilder newsb = null;
            for (int i = 0; i < _length; i++)
            {

                var eq = this.StartsWith(search, i);
                if (eq && newsb == null)
                {
                    newsb = new StringBuilder(this._length);
                    newsb.Append(_string, _start, i);
                }
                if (newsb != null)
                {
                    if (eq)
                    {
                        newsb.Append(replacement);
                        i += search.Length;
                        i--;
                    }
                    else
                    {
                        newsb.Append(_string[_start + i]);
                    }
                }
            }
            var r = newsb != null ? new ValueString(newsb.ToString()) : this;
            AssertEqual(r.ToClrString(), ToClrString().Replace(search, replacement));
            return r;
        }

        public bool EndsWith(ValueString str)
        {
            bool result;
            if (_string == null) result = str._length == 0;
            else if (str._length > this._length) result = false;
            else
            {
                result = true;
                for (int i = str._length - 1; i >= 0; i--)
                {
                    if (_string[_start + i] != str._string[str._start + i])
                    {
                        result = false;
                        break;
                    }
                }
            }
            AssertEqual(result, ToClrString().EndsWith(str.ToClrString()));
            return result;
        }

        public bool StartsWith(ValueString str, int startIdx)
        {
            bool result;
            if (_string == null) result = str._length == 0;
            else if (str._length > this._length - startIdx) result = false;
            else
            {
                result = true;
                for (int i = 0; i < str._length; i++)
                {
                    if (_string[_start + startIdx + i] != str._string[str._start + i])
                    {
                        result = false;
                        break;
                    }
                }
            }
            AssertEqual(result, ToClrString().StartsWith(str.ToClrString()));
            return result;
        }


        public bool StartsWith(ValueString str)
        {
            return StartsWith(str, 0);
        }

        public int LastIndexOf(ValueString substr, int p)
        {

            if (_string == null) return substr._length == 0 ? p : -1;
            // TODO useless conversion
            var r = ImplOffsetToPublicOffset(_string.LastIndexOf(substr.ToClrString(), _start + p, p, StringComparison.Ordinal));
            AssertEqual(r, ToClrString().LastIndexOf(substr.ToClrString(), p, StringComparison.Ordinal));
            return r;
        }

        public char[] ToCharArray()
        {
            var ch = new char[_length];
            for (int i = 0; i < _length; i++)
            {
                ch[i] = _string[_start + i];
            }
            return ch;
        }

        public override string ToString()
        {
            return ToClrString();
        }

        public bool Equals(ValueString other)
        {
            return this == other;
        }
    }


    public class ReseekableStringBuilder
    {

        public static void ClearThreadLocalCache()
        {
            _unusedStringBuilders = null;
            _unusedReseekableStringBuilders = null;
        }

        [ThreadStatic]
        internal static List<StringBuilder> _unusedStringBuilders;

        [ThreadStatic]
        internal static List<ReseekableStringBuilder> _unusedReseekableStringBuilders;

        public static StringBuilder AcquirePooledStringBuilder()
        {
            var u = _unusedStringBuilders;
            if (u == null)
            {
                u = new List<StringBuilder>();
                _unusedStringBuilders = u;
            }
            if (u.Count == 0)
            {
                return new StringBuilder();
            }
            else
            {
                var s = u[u.Count - 1];
                u.RemoveAt(u.Count - 1);
                return s;
            }

        }
        

        public static string GetValueAndRelease(StringBuilder sb)
        {
            var s = sb.ToString();
            Release(sb);
            return s;
        }
        public static string GetValueAndRelease(ReseekableStringBuilder sb)
        {
            var s = sb.ToString();
            Release(sb);
            return s;
        }

        public static void Release(StringBuilder sb)
        {
            var u = _unusedStringBuilders;
            if (u != null && u.Count < 4 && ValueString.ShouldKeepReleasedStringBuilder(sb))
            {
                sb.Length = 0;
                u.Add(sb);
            }
        }
        public static void Release(ReseekableStringBuilder sb)
        {
            var u = _unusedReseekableStringBuilders;
            if (u != null && u.Count < 4 && ValueString.ShouldKeepReleasedStringBuilder(sb._sb))
            {
                sb._start = 0;
                sb._sb.Length = 0;
                u.Add(sb);
            }
        }

        public static ReseekableStringBuilder AcquirePooledReseekableStringBuilder()
        {
            var u = _unusedReseekableStringBuilders;
            if (u == null)
            {
                u = new List<ReseekableStringBuilder>();
                _unusedReseekableStringBuilders = u;
            }
            if (u.Count == 0)
            {
                return new ReseekableStringBuilder();
            }
            else
            {
                var s = u[u.Count - 1];
                u.RemoveAt(u.Count - 1);
                return s;
            }
        }



        private int _start;
        private StringBuilder _sb = new StringBuilder();
        public int Length
        {
            get
            {
                return _sb.Length - _start;
            }
        }

        public void AppendValueString(ValueString str)
        {
            ValueStringExtensions.AppendValueString(_sb, str);
        }


        public override string ToString()
        {
            return _sb.ToString(_start, Length);
        }


        public ValueString Substring(int start, int count)
        {
            return _sb.ToString(_start + start, count);
        }

        public void TransformSubstring(int start)
        {
            _start += start;
        }

        public void TransformSubstring(int start, int count)
        {
            _start += start;
            _sb.Length = _start + count;
        }

        public ValueString Substring(int start)
        {
            return _sb.ToString(_start + start, Length - start);
        }

        internal string ToString(int startIndex, int length)
        {
            return _sb.ToString(_start + startIndex, length);
        }

        public char this[int pos]
        {
            get
            {
                return _sb[_start + pos];
            }
        }



        public void Clear()
        {
            _sb.Length = 0;
            _start = 0;
        }


    }

}
