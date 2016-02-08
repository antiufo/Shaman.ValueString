using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if SALTARELLE
using StringBuilder = System.Text.Saltarelle.StringBuilder;
#endif
namespace Shaman.Runtime
{
    public static partial class ValueStringExtensions
    {
        private const int size = 6841;
#if !SALTARELLE
        [ThreadStatic]
#endif
        private static CacheItem[] cache;
#if !SALTARELLE

        [ThreadStatic]
        private static StringBuilderReader stringBuilderReader;
        [ThreadStatic]
        private static ReseekableStringBuilderReader reseekablestringBuilderReader;
        [ThreadStatic]
        private static ValueStringReader valueStringReader;
        [ThreadStatic]
        private static ClrStringReader clrStringReader;
        [ThreadStatic]
        private static CharArrayReader charArrayReader;
#else
        private static string tempReader;
#endif
        public static void ClearForCurrentThread()
        {
            cache = null;
            _buffer = null;
            charArrayReader = null;
            clrStringReader = null;
            reseekablestringBuilderReader = null;
            stringBuilderReader = null;
            valueStringReader = null;
        }
        public static string SubstringCached(this string str, int startIndex)
        {
            return str.SubstringCached(startIndex, str.Length - startIndex);
        }
#if !SALTARELLE
        public static string SubstringCached(this ValueString str)
        {
            return str.SubstringCached(0, str.Length);
        }
        public static string SubstringCached(this ValueString str, int startIndex)
        {
            return str.SubstringCached(startIndex, str.Length - startIndex);
        }
        public static string SubstringCached(this ValueString str, int startIndex, int length)
        {
            var reader = valueStringReader ?? (valueStringReader = new ValueStringReader());
            reader.ValueString = str;
            var result = reader.SubstringCached(startIndex, length);
            reader.ValueString._string = null;
            return result;
        }

        public static string SubstringCached(this char[] str)
        {
            return str.SubstringCached(0, str.Length);
        }
        public static string SubstringCached(this char[] str, int startIndex)
        {
            return str.SubstringCached(startIndex, str.Length - startIndex);
        }
        public static string SubstringCached(this char[] str, int startIndex, int length)
        {
            var reader = charArrayReader ?? (charArrayReader = new CharArrayReader());
            reader.Array = str;
            var result = reader.SubstringCached(startIndex, length);
            reader.Array = null;
            return result;
        }
#endif
        public static string ToStringCached(this StringBuilder str)
        {
            return str.SubstringCached(0, str.Length);
        }
#if !SALTARELLE

        public static string ToStringCached(this ReseekableStringBuilder str)
        {
            return str.SubstringCached(0, str.Length);
        }
        public static string ToStringCached(this ValueString str)
        {
            return str.SubstringCached(0, str.Length);
        }
        public static string ToStringCached(this char[] str)
        {
            return str.SubstringCached(0, str.Length);
        }
#endif
        public static string SubstringCached(this StringBuilder str, int startIndex)
        {
            return str.SubstringCached(startIndex, str.Length - startIndex);
        }
        public static string SubstringCached(this StringBuilder str, int startIndex, int length)
        {
#if SALTARELLE
            string text = str.ToString();
            if (startIndex != 0 || length != text.Length)
            {
                return text.Substr(startIndex, length);
            }
            return text;
#else
            var reader = stringBuilderReader ?? (stringBuilderReader = new StringBuilderReader());
            reader.StringBuilder = str;
            string result = reader.SubstringCached(startIndex, length);
            reader.StringBuilder = null;
            return result;
#endif
        }


#if !SALTARELLE
        public static string SubstringCached(this ReseekableStringBuilder str, int startIndex)
        {
            return str.SubstringCached(startIndex, str.Length - startIndex);
        }
        public static string SubstringCached(this ReseekableStringBuilder str, int startIndex, int length)
        {
            var reader = reseekablestringBuilderReader ?? (reseekablestringBuilderReader = new ReseekableStringBuilderReader());
            reader.StringBuilder = str;
            string result = reader.SubstringCached(startIndex, length);
            reader.StringBuilder = null;
            return result;
        }
#endif
#if !SALTARELLE
        public static string SubstringCached(this string str, int startIndex, int length)
        {
            var reader = clrStringReader ?? (clrStringReader = new ClrStringReader());
            reader.ClrString = str;
            string result = reader.SubstringCached(startIndex, length);
            reader.ClrString = null;
            return result;
        }
#endif

#if SALTARELLE
        public static string SubstringCached(this string str, int startIndex, int length)
#else
        public static string SubstringCached(this LazyTextReader str, int startIndex, int length)
#endif
        {
            if (length == 0)
            {
                return string.Empty;
            }
            int num = CalculateHash(str[startIndex], str[startIndex + length - 1], length);
            if (cache == null)
            {
                cache = new CacheItem[6841];
            }
            CacheItem cacheItem = cache[num];
#if SALTARELLE
            if (cacheItem == null)
            {
                cacheItem = new CacheItem();
                StringCache.cache[num] = cacheItem;
            }
#endif
            List<string> list = cacheItem.List;
            if (list != null)
            {
                int count = list.Count;
                for (int i = 0; i < count; i++)
                {
                    string text = list[i];
                    if (text.Length == length)
                    {
                        bool flag = true;
                        for (int j = 0; j < text.Length; j++)
                        {
                            if (str[startIndex + j] != text[j])
                            {
                                flag = false;
                                break;
                            }
                        }
                        if (flag)
                        {
                            return text;
                        }
                    }
                }
            }
            bool flag2 = list != null && list.Count >= 5;
            string text2;
            if (!flag2)
            {
                text2 = cacheItem.Candidate;
                if (text2 != null && text2.Length == length)
                {
                    bool flag3 = true;
                    for (int k = 0; k < text2.Length; k++)
                    {
                        if (str[startIndex + k] != text2[k])
                        {
                            flag3 = false;
                            break;
                        }
                    }
                    if (flag3)
                    {
                        if (list == null)
                        {
                            list = new List<string>();
                        }
                        list.Add(text2);
                        cacheItem.Candidate = null;
                        cacheItem.List = list;
#if !SALTARELLE
                        cache[num] = cacheItem;
#endif
                        return text2;
                    }
                }
            }
            text2 = str.Substring(startIndex, length);
            if (!flag2)
            {
                cacheItem.Candidate = text2;
#if !SALTARELLE
                cache[num] = cacheItem;
#endif
            }
            return text2;
        }
        private static int CalculateHash(char firstChar, char lastChar, int length)
        {
            return ((int)(firstChar * '\u2971' + lastChar * '\u3847') + length) % 6841;
        }
        public static string ToLowerFast(this string str)
        {
            int length = str.Length;
            bool flag = true;
            for (int i = 0; i < length; i++)
            {
                char c = str[i];
                if (c > '\u0080' || ('A' <= c && c <= 'Z'))
                {
                    flag = false;
                    break;
                }
            }
            if (!flag)
            {
#if SALTARELLE
                return str.ToLower();
#else
                return str.ToLowerInvariant();
#endif
            }
            return str;
        }
#if !SALTARELLE
        public static string Dump()
        {
            return string.Join("\n", cache.Where((CacheItem x) => x.List != null).SelectMany((CacheItem x) => x.List).Select((string x) => x.ToString()).ToArray<string>());
        }
#endif
    }
}
