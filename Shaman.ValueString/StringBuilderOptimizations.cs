/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// File:	StringBuilderExtNumeric.cs
// Date:	9th March 2010
// Author:	Gavin Pugh
// Details:	Extension methods for the 'StringBuilder' standard .NET class, to allow garbage-free concatenation of
//			a selection of simple numeric types.  
//
// Copyright (c) Gavin Pugh 2010 - Released under the zlib license: http://www.opensource.org/licenses/zlib-license.php
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace Shaman.Runtime
{
    public static partial class ValueStringExtensions
    {
        // These digits are here in a static array to support hex with simple, easily-understandable code. 
        // Since A-Z don't sit next to 0-9 in the ascii table.
        private static readonly char[] ms_digits = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        //private static readonly uint ms_default_decimal_places = 5; //< Matches standard .NET formatting dp's

        //! Convert a given unsigned integer value to a string and concatenate onto the stringbuilder. Any base value allowed.
        public static StringBuilder AppendFast(this StringBuilder string_builder, uint uint_val)
        {
            const uint base_val = 10;
            // Calculate length of integer when written out
            uint length = 0;
            uint length_calc = uint_val;

            do
            {
                length_calc /= base_val;
                length++;
            }
            while (length_calc > 0);

            // Pad out space for writing.
            string_builder.Append('0', (int)length);

            int strpos = string_builder.Length;

            // We're writing backwards, one character at a time.
            while (length > 0)
            {
                strpos--;

                // Lookup from static char array, to cover hex values too
                string_builder[strpos] = ms_digits[uint_val % base_val];

                uint_val /= base_val;
                length--;
            }

            return string_builder;
        }


        public static void WriteFast(this TextWriter string_builder, uint uint_val)
        {
            const uint base_val = 10;
            // Calculate length of integer when written out
            uint length = 0;
            uint length_calc = uint_val;

            do
            {
                length_calc /= base_val;
                length++;
            }
            while (length_calc > 0);

            var buffer = _buffer ?? (_buffer = new char[20]);

            int strpos = (int)length;
            var originalLength = strpos;
            // We're writing backwards, one character at a time.
            while (length > 0)
            {
                strpos--;

                // Lookup from static char array, to cover hex values too
                buffer[strpos] = ms_digits[uint_val % base_val];

                uint_val /= base_val;
                length--;
            }
            string_builder.Write(buffer, 0, originalLength);
        }


        public static void WriteFast(this TextWriter string_builder, ulong uint_val)
        {
            const uint base_val = 10;
            // Calculate length of integer when written out
            uint length = 0;
            ulong length_calc = uint_val;

            do
            {
                length_calc /= base_val;
                length++;
            }
            while (length_calc > 0);

            var buffer = _buffer ?? (_buffer = new char[20]);

            int strpos = (int)length;
            var originalLength = strpos;
            // We're writing backwards, one character at a time.
            while (length > 0)
            {
                strpos--;

                // Lookup from static char array, to cover hex values too
                buffer[strpos] = ms_digits[uint_val % base_val];

                uint_val /= base_val;
                length--;
            }
            string_builder.Write(buffer, 0, originalLength);
        }


        [ThreadStatic]
        internal static char[] _buffer;


        public static StringBuilder AppendFast(this StringBuilder string_builder, ulong uint_val)
        {

            const uint base_val = 10;
            // Calculate length of integer when written out
            uint length = 0;
            ulong length_calc = uint_val;

            do
            {
                length_calc /= base_val;
                length++;
            }
            while (length_calc > 0);

            // Pad out space for writing.
            string_builder.Append('0', (int)length);

            int strpos = string_builder.Length;

            // We're writing backwards, one character at a time.
            while (length > 0)
            {
                strpos--;

                // Lookup from static char array, to cover hex values too
                string_builder[strpos] = ms_digits[uint_val % base_val];

                uint_val /= base_val;
                length--;
            }

            return string_builder;
        }



        //! Convert a given signed integer value to a string and concatenate onto the stringbuilder. Any base value allowed.
        public static StringBuilder AppendFast(this StringBuilder string_builder, int int_val)
        {
            // Deal with negative numbers
            if (int_val < 0)
            {
                string_builder.Append('-');
                uint uint_val = uint.MaxValue - ((uint)int_val) + 1; //< This is to deal with Int32.MinValue
                string_builder.AppendFast(uint_val);
            }
            else
            {
                string_builder.AppendFast((uint)int_val);
            }

            return string_builder;
        }

        public static void WriteFast(this TextWriter string_builder, long int_val)
        {
            // Deal with negative numbers
            if (int_val < 0)
            {
                string_builder.Write('-');
                ulong uint_val = ulong.MaxValue - ((ulong)int_val) + 1; //< This is to deal with Int64.MinValue
                string_builder.WriteFast(uint_val);
            }
            else
            {
                string_builder.WriteFast((ulong)int_val);
            }

        }
        public static void WriteFast(this TextWriter string_builder, int int_val)
        {
            // Deal with negative numbers
            if (int_val < 0)
            {
                string_builder.Write('-');
                uint uint_val = uint.MaxValue - ((uint)int_val) + 1; //< This is to deal with Int32.MinValue
                string_builder.WriteFast(uint_val);
            }
            else
            {
                string_builder.WriteFast((uint)int_val);
            }

        }



        public static StringBuilder AppendFast(this StringBuilder string_builder, long int_val)
        {
            // Deal with negative numbers
            if (int_val < 0)
            {
                string_builder.Append('-');
                ulong uint_val = ulong.MaxValue - ((ulong)int_val) + 1; //< This is to deal with Int32.MinValue
                string_builder.AppendFast(uint_val);
            }
            else
            {
                string_builder.AppendFast((ulong)int_val);
            }

            return string_builder;
        }


#if false
        //! Convert a given float value to a string and concatenate onto the stringbuilder
        public static StringBuilder Concat(this StringBuilder string_builder, float float_val, uint decimal_places, uint pad_amount, char pad_char)
        {
            Debug.Assert(pad_amount >= 0);

            if (decimal_places == 0)
            {
                // No decimal places, just round up and print it as an int

                // Agh, Math.Floor() just works on doubles/decimals. Don't want to cast! Let's do this the old-fashioned way.
                int int_val;
                if (float_val >= 0.0f)
                {
                    // Round up
                    int_val = (int)(float_val + 0.5f);
                }
                else
                {
                    // Round down for negative numbers
                    int_val = (int)(float_val - 0.5f);
                }

                string_builder.Concat(int_val, pad_amount, pad_char, 10);
            }
            else
            {
                int int_part = (int)float_val;

                // First part is easy, just cast to an integer
                string_builder.Concat(int_part, pad_amount, pad_char, 10);

                // Decimal point
                string_builder.Append('.');

                // Work out remainder we need to print after the d.p.
                float remainder = Math.Abs(float_val - int_part);

                // Multiply up to become an int that we can print
                do
                {
                    remainder *= 10;
                    decimal_places--;
                }
                while (decimal_places > 0);

                // Round up. It's guaranteed to be a positive number, so no extra work required here.
                remainder += 0.5f;

                // All done, print that as an int!
                string_builder.Concat((uint)remainder, 0, '0', 10);
            }
            return string_builder;
        }
#endif

    }
}

