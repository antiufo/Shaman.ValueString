// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
#if !NET35
namespace System.Text.Utf16
{
    internal static class Utf16LittleEndianEncoder
    {
        const uint MaskLow10Bits = 0x3FF;

        public static bool TryDecodeCodePoint(Span<byte> buffer, out UnicodeCodePoint codePoint, out int encodedBytes)
        {
            if (buffer.Length < 2)
            {
                codePoint = default(UnicodeCodePoint);
                encodedBytes = default(int);
                // buffer too small
                return false;
            }

            uint codePointValue = buffer.ReadUInt16();
            encodedBytes = 2;
            if (UnicodeCodePoint.IsSurrogate((UnicodeCodePoint)codePointValue))
            {
                // TODO: Check if compiler optimized it so codePointValue low range is checked only once
                if (!UnicodeCodePoint.IsHighSurrogate((UnicodeCodePoint)codePointValue) || buffer.Length < 4)
                {
                    codePoint = default(UnicodeCodePoint);
                    encodedBytes = default(int);
                    // invalid high surrogate or buffer too small
                    return false;
                }
                unchecked
                {
                    codePointValue -= UnicodeConstants.Utf16HighSurrogateFirstCodePoint;
                    encodedBytes += 2;
                }
                // high surrogate contains 10 first bits of the code point
                codePointValue <<= 10;

                uint lowSurrogate = buffer.ReadUInt32() >> 16;
                if (!UnicodeCodePoint.IsLowSurrogate((UnicodeCodePoint)lowSurrogate))
                {
                    codePoint = default(UnicodeCodePoint);
                    encodedBytes = default(int);
                    // invalid low surrogate character
                    return false;
                }

                unchecked
                {
                    lowSurrogate -= UnicodeConstants.Utf16LowSurrogateFirstCodePoint;
                }
                codePointValue |= lowSurrogate;
            }

            codePoint = (UnicodeCodePoint)codePointValue;

            return true;
        }

        public unsafe static bool TryEncodeCodePoint(UnicodeCodePoint codePoint, char* buffer, out int encodedChars)
        {
            if (!UnicodeCodePoint.IsSupportedCodePoint(codePoint))
            {
                encodedChars = default(int);
                return false;
            }

            // TODO: Can we add this in UnicodeCodePoint class?
            // Should be represented as Surrogate?
            encodedChars = ((uint)codePoint >= 0x10000) ? 2 : 1;

            /*
            Never happens. Max encodedBytes = 4 bytes = 2 chars. We already preallocate 2 chars for every UTF8 byte.
            if (buffer.Length < encodedBytes)
            {
                codePoint = default(UnicodeCodePoint);
                encodedBytes = default(int);
                // buffer too small
                return false;
            }
            */

            if (encodedChars == 1)
            {
                unchecked
                {
                    Write(buffer, (ushort)codePoint);
                }
            }
            else
            {
                unchecked
                {
                    uint highSurrogate = ((uint)(codePoint.Value - 0x10000) >> 10) + UnicodeConstants.Utf16HighSurrogateFirstCodePoint;
                    uint lowSurrogate = ((uint)codePoint & MaskLow10Bits) + UnicodeConstants.Utf16LowSurrogateFirstCodePoint;

                    Write(buffer, highSurrogate | (lowSurrogate << 16));
                }
            }
            return true;
        }

        public static unsafe void Write(char* array, uint v)
        {
            *(uint*)array = v;
        }
        public static unsafe void Write(char* array, ushort v)
        {
            *(ushort*)array = v;
        }


        // TODO: Should we rewrite this to not use char.ConvertToUtf32 or is it fast enough?
        public static bool TryDecodeCodePointFromString(string s, int index, out UnicodeCodePoint codePoint, out int encodedChars)
        {
            if (index < 0 || index >= s.Length)
            {
                codePoint = default(UnicodeCodePoint);
                encodedChars = 0;
                return false;
            }

            if (index == s.Length - 1 && char.IsSurrogate(s[index]))
            {
                codePoint = default(UnicodeCodePoint);
                encodedChars = 0;
                return false;
            }

            encodedChars = char.IsHighSurrogate(s[index]) ? 2 : 1;
            codePoint = (UnicodeCodePoint)(unchecked((uint)char.ConvertToUtf32(s, index)));

            return true;
        }
    }
}
#endif