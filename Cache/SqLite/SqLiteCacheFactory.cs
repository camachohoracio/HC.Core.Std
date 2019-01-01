#region

using System;
using System.Collections.Concurrent;

#endregion

namespace HC.Core.Cache.SqLite
{
    public static class SqLiteCacheFactory
    {
        #region Members

        private static readonly ConcurrentDictionary<string, object> m_sqlLiteCaches;
        private static readonly object m_lockObject;

        #endregion

        #region Constructors

        static SqLiteCacheFactory()
        {
            m_sqlLiteCaches = new ConcurrentDictionary<string, object>();
            //m_sqlLiteCaches.OnItemRemoved += bufferItem => ((IDisposable)bufferItem).Dispose();
            m_lockObject = new object();
        }

        #endregion

        #region Public

        public static ISqLiteCache<T> GetSqLiteDb<T>(
            string strDbFileName,
            string strTableName,
            EnumSqLiteCacheType enumSqLiteCacheType,
            bool blnUseCompression)
        {
            lock (m_lockObject)
            {
                ISqLiteCache<T> cache;
                object dbObj;
                if (!m_sqlLiteCaches.TryGetValue(
                    strDbFileName,
                    out dbObj) ||
                    (cache = dbObj as ISqLiteCache<T>) == null)
                {
                    dbObj = GetCacheObj<T>(strDbFileName, 
                        enumSqLiteCacheType, 
                        strTableName);
                    m_sqlLiteCaches[strDbFileName] = dbObj;
                    cache = dbObj as ISqLiteCache<T>;
                }
                cache.UseCompression = blnUseCompression;
                return cache;
            }
        }

        private static object GetCacheObj<T>(
            string strDbFileName, 
            EnumSqLiteCacheType enumSqLiteCacheType, 
            string strTableName)
        {
            object dbObj;
            if (enumSqLiteCacheType == EnumSqLiteCacheType.BLob)
            {
                dbObj = new SqLiteCacheBLob<T>(
                    strDbFileName,
                    strTableName,
                    null,
                    SqliteConstants.KEY_COL_NAME);
            }
            else if (enumSqLiteCacheType == EnumSqLiteCacheType.FullSchema)
            {
                dbObj = new SqliteCacheFullSchema<T>(
                    strDbFileName,
                    strTableName,
                    null,
                    SqliteConstants.KEY_COL_NAME);
            }
            else
            {
                throw new NotImplementedException();
            }
            return dbObj;
        }

        #endregion
    }
}



