#region

using System;
using System.Collections;
using System.Collections.Generic;

#endregion

namespace HC.Core.Cache.DataStructure
{
    public class GenericValueCollectionCache<T> :
        AbstractCollectionCache, ICollection<T>
    {
        #region Constructors

        public GenericValueCollectionCache(ICache db) :
            base(db)
        {
        }

        #endregion

        #region ICollection<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            return new GenericKeyEnumeratorCache<T>(m_db);
        }

        public void Add(T item)
        {
            m_db.Add(item, item);
        }

        public bool Contains(T item)
        {
            return m_db.ContainsKey(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            m_db.Delete(item);
            return true;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new ValueEnumeratorCache(m_db);
        }

        #endregion
    }
}



