#region

using System;
using System.Collections;

//using HC.Core.Caches.BerkeleyCache;

#endregion

namespace HC.Core.Cache.DataStructure
{
    public class DictionaryEnumeratorCache :
        AbstractEnumeratorCache, IDictionaryEnumerator
    {
        #region Constructors

        public DictionaryEnumeratorCache(
            object cacheBerkeley) :
                base(null)
        {
        }

        #endregion

        // Return the current item.

        #region IDictionaryEnumerator Members

        public Object Current
        {
            get
            {
                return null;
                //return CacheBerkeley.DeserializeDatabaseEntry(
                //    m_dbEnumerator.Current.Value,
                //    m_cache.CompressItems);
            }
        }

        // Return the current dictionary entry.
        public DictionaryEntry Entry
        {
            get
            {
                var dictionaryEntry =
                    new DictionaryEntry(Key,
                                        Value);
                return dictionaryEntry;
            }
        }

        // Return the key of the current item.
        public Object Key
        {
            get
            {
                return null;
                //return CacheBerkeley.DeserializeDatabaseEntry(
                //    m_dbEnumerator.Current.Key,
                //    m_cache.CompressItems);
            }
        }

        // Return the value of the current item.
        public Object Value
        {
            get { return Current; }
        }

        #endregion
    }
}



