using System;
using System.Collections.Generic;
using System.Linq;

namespace Shaman.Runtime
{
    internal sealed class ValueStringReader : LazyTextReader
    {
        public ValueString ValueString;
        public override char this[int index]
        {
            get
            {
                return ValueString[index];
            }
        }

        public override string Substring(int startIndex, int length)
        {
            return ValueString._string.Substring(ValueString._start + startIndex, length);
        }
    }
}
