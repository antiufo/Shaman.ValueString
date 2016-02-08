using System;
using System.Collections.Generic;
using System.Linq;

namespace Shaman.Runtime
{
    internal sealed class ClrStringReader : LazyTextReader
    {
        public string ClrString;
        public override char this[int index]
        {
            get
            {
                return ClrString[index];
            }
        }

        public override string Substring(int startIndex, int length)
        {
            return ClrString.Substring(startIndex, length);
        }
    }
}
