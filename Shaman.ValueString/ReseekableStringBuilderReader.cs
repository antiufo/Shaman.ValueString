using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shaman.Runtime
{
    internal sealed class ReseekableStringBuilderReader : LazyTextReader
    {
        public ReseekableStringBuilder StringBuilder;

        public override char this[int index]
        {
            get
            {
                return StringBuilder[index];
            }
        }

        public override string Substring(int startIndex, int length)
        {
            return StringBuilder.ToString(startIndex, length);
        }
    }
}
