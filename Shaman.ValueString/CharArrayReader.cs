using System;
using System.Collections.Generic;
using System.Linq;

namespace Shaman.Runtime
{
    internal sealed class CharArrayReader : LazyTextReader
    {
        public char[] Array;
        public override char this[int index]
        {
            get
            {
                return Array[index];
            }
        }

        public override string Substring(int startIndex, int length)
        {
            return new string(Array, startIndex, length); 
        }
    }
}
