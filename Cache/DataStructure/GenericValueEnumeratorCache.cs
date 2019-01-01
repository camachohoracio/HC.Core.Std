#region

using System.Collections;
using System.Collections.Generic;

#endregion

namespace HC.Core.Cache.DataStructure
{
    public class GenericValueEnumeratorCache<T> :
        AbstractEnumeratorCache, IEnumerator<T>
    {
        #region Constructors

        public GenericValueEnumeratorCache(
            object cacheBerkeley) : base(null)
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
                //               m_dbEnumerator.Current.Value,
                //               m_cache.CompressItems);
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return null;
                //return CacheBerkeley.DeserializeDatabaseEntry(
                //    m_dbEnumerator.Current.Value,
                //    m_cache.CompressItems);
            }
        }

        #endregion
    }
}



