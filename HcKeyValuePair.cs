using System;
using System.Collections.Generic;

namespace HC.Core
{
    public class HcKeyValuePair<T1, T2> : IDisposable
    {
        public T1 Key { get; set; }
        public T2 Value { get; set; }
        
        public HcKeyValuePair()
        {
        }

        public HcKeyValuePair(KeyValuePair<T1,T2> kvp) : this(kvp.Key,kvp.Value)
        {
        }

        public HcKeyValuePair(T1 key, T2 value)
        {
            Key = key;
            Value = value;
        }

        public void Dispose()
        {
            Key = default(T1);
            Value = default(T2);
        }
    }
}
