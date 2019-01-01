#region

using System.Collections;
using System.Collections.Generic;

#endregion

namespace HC.Core.Cache.DataStructure
{
    public class GenericKeyEnumeratorCache<T> :
        AbstractEnumeratorCache, IEnumerator<T>
    {
        #region Constructors

        public GenericKeyEnumeratorCache(
            ICache cache)
            : base(cache)
        {
        }

        #endregion

        #region IEnumerator<T> Members

        public T Current
        {
            get
            {
                return default(T);
                //return (T) CacheBerkeley.DeserializeDatabaseEntry(
                //               m_dbEnumerator.Current.Key,
                //               m_cache.CompressItems);
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return null;
                //return CacheBerkeley.DeserializeDatabaseEntry(
                //    m_dbEnumerator.Current.Key,
                //    m_cache.CompressItems);
            }
        }

        #endregion
    }
}



