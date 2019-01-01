#region



//using BerkeleyDB;
//using HC.Core.Caches.BerkeleyCache;

#endregion

namespace HC.Core.Cache.DataStructure
{
    public abstract class AbstractEnumeratorCache
    {
        #region Members

        // A copy of the SimpleDictionary object's key/value pairs.
        protected readonly ICache m_cache;
        //private Cursor m_cursor;
        //protected IEnumerator<KeyValuePair<DatabaseEntry, DatabaseEntry>> m_dbEnumerator;

        #endregion

        #region Constructors

        public AbstractEnumeratorCache(
            ICache cache)
        {
            // Make a copy of the dictionary entries currently in the SimpleDictionary object.
            m_cache = cache;
            //m_dbEnumerator = ((CacheBerkeley)m_cache).GetEnumerator(
            //    out m_cursor);
        }

        #endregion

        // Advance to the next item.
        public bool MoveNext()
        {
            return false;
            //return m_dbEnumerator.MoveNext();
        }

        // Reset the index to restart the enumeration.
        public void Reset()
        {
            CloseCursor();
            //m_dbEnumerator = ((CacheBerkeley)m_cache).GetEnumerator(out m_cursor);
        }

        public void Dispose()
        {
            CloseCursor();
        }

        private void CloseCursor()
        {
            //if (m_cursor != null)
            //{
            //    m_cursor.Close();
            //}
        }
    }
}



