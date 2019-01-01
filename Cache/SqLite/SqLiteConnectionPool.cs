using System;
using System.IO;
using System.Threading;
using HC.Core.Exceptions;
using HC.Core.Io;
using HC.Core.Logging;
using HC.Core.Threading;
using HC.Core.Threading.Buffer;

namespace HC.Core.Cache.SqLite
{
    public static class SqLiteConnectionPool
    {
        #region Members

        private static readonly EfficientMemoryBuffer<string, ISqLiteCacheBase> m_sqlLiteCacheWrappersBuffer;
        private static readonly object m_lockBufferCaches = new object();
        private static readonly object m_requestLock = new object();

        #endregion

        static SqLiteConnectionPool()
        {
            m_sqlLiteCacheWrappersBuffer = 
                new EfficientMemoryBuffer<string, ISqLiteCacheBase>(SqliteConstants.DB_OPEN_CONNECTIONS);
            m_sqlLiteCacheWrappersBuffer.OnItemRemoved += DisposeConnection;
        }

        public static SqLiteAdoWrapper<T> GetCacheWrapper0<T>(
            string strFileName)
        {
            try
            {
                lock (m_requestLock)
                {
                    ISqLiteCacheBase cacheItem;
                    if (!m_sqlLiteCacheWrappersBuffer.TryGetValue(
                        strFileName,
                        out cacheItem))
                    {
                        lock (LockObjectHelper.GetLockObject(strFileName))
                        {
                            if (!m_sqlLiteCacheWrappersBuffer.TryGetValue(
                                strFileName,
                                out cacheItem))
                            {
                                cacheItem = LoadDatabase<T>(strFileName);
                                lock (m_lockBufferCaches)
                                {
                                    m_sqlLiteCacheWrappersBuffer.Add( // this add blocks unitll the disposed item is disposed. WE cannot add until old disposed
                                        strFileName,
                                        cacheItem);
                                }
                            }
                        }
                    }
                    return (SqLiteAdoWrapper<T>) cacheItem;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        private static void DisposeConnection(
            ISqLiteCacheBase item)
        {
            if (item == null)
            {
                return;
            }
            try
            {
                //string strFileName = item.FileName;
                item.Dispose();
                //Logger.Log("Disposed db connection [" +
                //           strFileName + "]");
                //Console.WriteLine(strFileName);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                Thread.Sleep(60000);
                DisposeConnection(item);
            }
        }

        private static SqLiteAdoWrapper<T> LoadDatabase<T>(string strFileName)
        {
            try
            {
                strFileName = FileHelper.CleanFileName(strFileName);
                var fi = new FileInfo(strFileName);
                string strDirName = fi.DirectoryName;
                if (string.IsNullOrEmpty(strDirName))
                {
                    throw new HCException("Empty directory");
                }
                if (!DirectoryHelper.Exists(
                    strDirName,
                    false))
                {
                    DirectoryHelper.CreateDirectory(strDirName);
                }

                var sqLiteAdoWrapper = new SqLiteAdoWrapper<T>(strFileName);

                if (!FileHelper.Exists(
                    strFileName,
                    false))
                {
                    Thread.Sleep(1000);
                    sqLiteAdoWrapper = new SqLiteAdoWrapper<T>(strFileName);
                    if (!FileHelper.Exists(
                        strFileName,
                        false))
                    {
                        throw new Exception("Db file not found [" +
                                            strFileName + "]");
                    }
                }

                return sqLiteAdoWrapper;
            }
            catch (Exception ex)
            {
                Logger.Log(new Exception("Failed to load db file [" +
                    strFileName + "]"));
                Logger.Log(ex);
            }
            return null;
        }
    }
}
