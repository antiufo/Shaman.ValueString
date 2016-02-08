using System;
using System.Collections.Generic;
namespace Shaman.Runtime
{
#if SALTARELLE
    internal class CacheItem
#else
    internal struct CacheItem
#endif
    {
		public string Candidate;
		public List<string> List;
	}
}
