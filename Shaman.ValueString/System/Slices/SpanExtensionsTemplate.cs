﻿




//------------------------------------------------------------------------------
// <auto-generated>look at the SpanExtensionsTemplate.tt</auto-generated>
//------------------------------------------------------------------------------
// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace System
{
    public static partial class SpanExtensions
    {

        /// <summary>
        /// Determines whether two spans are equal (byte-wise) by comparing the elements by using memcmp
        /// </summary>
        /// <param name="first">A span of bytes to compare to second.</param>
        /// <param name="second">A span of bytes T to compare to first.</param>
        public static bool SequenceEqual(this Span<byte> first, Span<byte> second)
        {
            return first.Length >= 320
                ? MemoryUtils.MemCmp(first, second)
                : SequenceEqual<byte>(first, second);
        }

		/// <summary>
        /// Determines whether two read-only spans are equal (byte-wise) by comparing the elements by using memcmp
        /// </summary>
        /// <param name="first">A span of bytes to compare to second.</param>
        /// <param name="second">A span of bytes T to compare to first.</param>
        public static bool SequenceEqual(this ReadOnlySpan<byte> first, ReadOnlySpan<byte> second)
        {
            return first.Length >= 320
                ? MemoryUtils.MemCmp(first, second)
                : SequenceEqual<byte>(first, second);
        }


        /// <summary>
        /// Determines whether two spans are equal (byte-wise) by comparing the elements by using memcmp
        /// </summary>
        /// <param name="first">A span of characters to compare to second.</param>
        /// <param name="second">A span of characters T to compare to first.</param>
        public static bool SequenceEqual(this Span<char> first, Span<char> second)
        {
            return first.Length >= 512
                ? MemoryUtils.MemCmp(first, second)
                : SequenceEqual<char>(first, second);
        }

		/// <summary>
        /// Determines whether two read-only spans are equal (byte-wise) by comparing the elements by using memcmp
        /// </summary>
        /// <param name="first">A span of characters to compare to second.</param>
        /// <param name="second">A span of characters T to compare to first.</param>
        public static bool SequenceEqual(this ReadOnlySpan<char> first, ReadOnlySpan<char> second)
        {
            return first.Length >= 512
                ? MemoryUtils.MemCmp(first, second)
                : SequenceEqual<char>(first, second);
        }


        /// <summary>
        /// Determines whether two spans are equal (byte-wise) by comparing the elements by using memcmp
        /// </summary>
        /// <param name="first">A span of shorts to compare to second.</param>
        /// <param name="second">A span of shorts T to compare to first.</param>
        public static bool SequenceEqual(this Span<short> first, Span<short> second)
        {
            return first.Length >= 512
                ? MemoryUtils.MemCmp(first, second)
                : SequenceEqual<short>(first, second);
        }

		/// <summary>
        /// Determines whether two read-only spans are equal (byte-wise) by comparing the elements by using memcmp
        /// </summary>
        /// <param name="first">A span of shorts to compare to second.</param>
        /// <param name="second">A span of shorts T to compare to first.</param>
        public static bool SequenceEqual(this ReadOnlySpan<short> first, ReadOnlySpan<short> second)
        {
            return first.Length >= 512
                ? MemoryUtils.MemCmp(first, second)
                : SequenceEqual<short>(first, second);
        }


        /// <summary>
        /// Determines whether two spans are equal (byte-wise) by comparing the elements by using memcmp
        /// </summary>
        /// <param name="first">A span of integers to compare to second.</param>
        /// <param name="second">A span of integers T to compare to first.</param>
        public static bool SequenceEqual(this Span<int> first, Span<int> second)
        {
            return first.Length >= 256
                ? MemoryUtils.MemCmp(first, second)
                : SequenceEqual<int>(first, second);
        }

		/// <summary>
        /// Determines whether two read-only spans are equal (byte-wise) by comparing the elements by using memcmp
        /// </summary>
        /// <param name="first">A span of integers to compare to second.</param>
        /// <param name="second">A span of integers T to compare to first.</param>
        public static bool SequenceEqual(this ReadOnlySpan<int> first, ReadOnlySpan<int> second)
        {
            return first.Length >= 256
                ? MemoryUtils.MemCmp(first, second)
                : SequenceEqual<int>(first, second);
        }


        /// <summary>
        /// Determines whether two spans are equal (byte-wise) by comparing the elements by using memcmp
        /// </summary>
        /// <param name="first">A span of long integers to compare to second.</param>
        /// <param name="second">A span of long integers T to compare to first.</param>
        public static bool SequenceEqual(this Span<long> first, Span<long> second)
        {
            return first.Length >= 256
                ? MemoryUtils.MemCmp(first, second)
                : SequenceEqual<long>(first, second);
        }

		/// <summary>
        /// Determines whether two read-only spans are equal (byte-wise) by comparing the elements by using memcmp
        /// </summary>
        /// <param name="first">A span of long integers to compare to second.</param>
        /// <param name="second">A span of long integers T to compare to first.</param>
        public static bool SequenceEqual(this ReadOnlySpan<long> first, ReadOnlySpan<long> second)
        {
            return first.Length >= 256
                ? MemoryUtils.MemCmp(first, second)
                : SequenceEqual<long>(first, second);
        }


    }
}
