#region

#endregion

namespace HC.Core.Cache.DataStructure
{
    public abstract class AbstractCollectionCache
    {
        #region Members

        protected readonly ICache m_db;

        #endregion

        #region Constructors

        public AbstractCollectionCache(ICache db)
        {
            m_db = db;
        }

        #endregion

        public int Count
        {
            get { return m_db.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public void Clear()
        {
            m_db.Clear();
        }
    }
}



