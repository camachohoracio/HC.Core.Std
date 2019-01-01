using System;
using System.Collections.Concurrent;

namespace HC.Core.DataStructures
{
    public class ConcurrentDictionaryWrapper<T1, T2> : IDisposable
    {
        public ConcurrentDictionary<T1, T2> ConcurrentDictionary { get; set; }

        public ConcurrentDictionaryWrapper(ConcurrentDictionary<T1,T2> concurrentDictionary)
        {
            ConcurrentDictionary = concurrentDictionary;
        }

        public void Dispose()
        {
            if(ConcurrentDictionary != null)
            {
                ConcurrentDictionary.Clear();
                ConcurrentDictionary = null;
            }
        }
    }
}