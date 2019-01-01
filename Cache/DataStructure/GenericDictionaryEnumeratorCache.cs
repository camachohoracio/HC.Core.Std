#region

using System;
using System.Collections.Generic;

//using HC.Core.Caches.BerkeleyCache;

#endregion

namespace HC.Core.Cache.DataStructure
{
    public class GenericDictionaryEnumeratorCache<TKey, TValue> :
        AbstractEnumeratorCache, IEnumerator<KeyValuePair<TKey, TValue>>
    {
        #region Constructors

        public GenericDictionaryEnumeratorCache(
            ICache cache) : base(cache)
        {
        }

        #endregion

        // Return the current item.

        #region IEnumerator<KeyValuePair<TKey,TValue>> Members

        public Object Current
        {
            get { return GetKvp(); }
        }


        KeyValuePair<TKey, TValue> IEnumerator<KeyValuePair<TKey, TValue>>.Current
        {
            get { return GetKvp(); }
        }

        #endregion

        private KeyValuePair<TKey, TValue> GetKvp()
        {
            //KeyValuePair<TKey, TValue> kvp =
            //    new KeyValuePair<TKey, TValue>(
            //        (TKey) CacheBerkeley.DeserializeDatabaseEntry(
            //                   m_dbEnumerator.Current.Key,
            //                   m_cache.CompressItems),
            //        (TValue) CacheBerkeley.DeserializeDatabaseEntry(
            //                     m_dbEnumerator.Current.Value,
            //                     m_cache.CompressItems));
            //return kvp;
            return new KeyValuePair<TKey, TValue>();
        }
    }
}



