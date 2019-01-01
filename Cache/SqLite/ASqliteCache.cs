#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading;
using HC.Core.Comunication;
using HC.Core.ConfigClasses;
using HC.Core.DataStructures;
using HC.Core.Events;
using HC.Core.Exceptions;
using HC.Core.Logging;
using HC.Core.Reflection;
using HC.Core.Threading.ProducerConsumerQueues.Support;

#endregion

namespace HC.Core.Cache.SqLite
{
    public abstract class ASqliteCache<T> : ISqLiteCache<T>
    {
        #region Propeties

        public bool UseCompression { get; set; }
        public string FileName { get; private set; }
        public SQLiteConnection DbConn { get { throw new HCException("Not implemented"); }}
        public bool IsDisposed { get; private set; }
        public ReaderWriterLock DisposeLock { get; private set; }
        public abstract KeyValuePair<string, DbType>[] ColNameToTypeArr { get; set; }
        public abstract object[][] GetImportArrBLob(List<KeyValuePair<string, object>> importList, bool blnUseCompression);
        public abstract object[][] GetImportArr(List<KeyValuePair<string, object>> importList, bool blnUseCompression);
        public abstract void GetColNameToTypeArr(string p);

        public int Count
        {
            get
            {
                try
                {
                    if (string.IsNullOrEmpty(m_strDefaultIndex))
                    {
                        throw new HCException("Key not found");
                    }

                    string strQuery = "SELECT COUNT(*) " +
                                      " from [" + m_strTableName +
                                      "]";

                    return (int)ExecuteScalar<long>(strQuery);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
                return 0;
            }
        }

        public TK ExecuteScalar<TK>(string strQuery)
        {
            List<object[]> data = null;
            try
            {
                data = new List<object[]>();
                SqLiteTaskQueues.EnqueueLoad(
                    strQuery,
                    data,
                    FileName,
                    this,
                    true);

                if (data.Count > 0)
                {
                    object[] resultArr = data[0];
                    if (resultArr != null && resultArr.Length > 0)
                    {
                        var result = resultArr[0];
                        resultArr[0] = null;
                        return (TK)result;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(new HCException("Exception in database [" +
                    FileName +
                    "]"));
                Logger.Log(ex);
            }
            finally 
            {
                try
                {
                    if (data != null)
                    {
                        data.Clear();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            return default(TK);
        }

        public List<string> ContainsKeys(List<string> strKeys)
        {
            try
            {
                if (strKeys == null || strKeys.Count == 0)
                {
                    return new List<string>();
                }

                if (string.IsNullOrEmpty(m_strDefaultIndex))
                {
                    throw new HCException("Key not found");
                }

                string strQuery = "SELECT distinct " + m_strDefaultIndex +
                                " FROM [" + m_strTableName +
                               "] WHERE " + m_strDefaultIndex + " IN (" +
                      GetInStatement(strKeys) + ")";

                var data = new List<object[]>();
                Execute(strQuery, data);
                var keysResult = new List<string>();
                foreach (object[] objectse in data)
                {
                    if (objectse[0] != null)
                    {
                        keysResult.Add((string) objectse[0]);
                    }
                }
                return keysResult;
            }
            catch (Exception ex)
            {
                Logger.Log(new HCException("Exception in database [" +
                    FileName +
                    "]"));
                Logger.Log(ex);
            }
            return new List<string>();
        }

        #endregion

        #region Members

        protected List<string> m_readWritePropertyNames;
        protected IReflector m_reflector;
        protected string m_strDefaultIndex;
        private string[] m_strIndexes;
        protected string m_strTableName;
        private static readonly ConcurrentDictionary<string, object> m_validatedTables;

        #endregion

        #region Constructors

        static ASqliteCache()
        {
            //m_lockMap = new ConcurrentDictionary<string, ReaderWriterLock>();
            m_validatedTables = new ConcurrentDictionary<string, object>();
        }

        protected ASqliteCache(
            string strFileName,
            string strTableName,
            string[] strIndexes,
            string strDefaultIndex)
        {
            try
            {
                if (NetworkHelper.IsADistWorkerConnected)
                {
                    LiveGuiPublisherEvent.PublishGrid(
                        "Admin",
                        "CloudWorkers",
                        "SqLite_" + HCConfig.ClientUniqueName,
                        Environment.StackTrace.GetHashCode().ToString(),
                        new StringWrapper(Environment.StackTrace),
                        0,
                        false);
                    Logger.Log(
                        new HCException(
                        "Worker should not access data! " +
                        Environment.StackTrace));
                }

                strTableName = strTableName
                    .Replace("'", string.Empty)
                    .Replace("`", string.Empty);
                DisposeLock = new ReaderWriterLock();
                m_reflector = ReflectorCache.GetReflector(typeof (T));
                
                m_readWritePropertyNames = m_reflector.GetReadWritePropertyNames().Distinct().ToList();
                
                FileName = strFileName;
                m_strTableName = strTableName;
                m_strIndexes = strIndexes;
                m_strDefaultIndex = strDefaultIndex;

                object obj;
                if (!string.IsNullOrEmpty(strTableName) &&
                    !m_validatedTables.TryGetValue(
                        FileName + "_" + strTableName, out obj))
                {
                    ValidateTable();
                    CreateIndexes();
                    m_validatedTables[FileName + "_" + strTableName] = null;
                }
            }
            catch(Exception ex)
            {
                Logger.Log(new Exception("Failed to load db file [" + strFileName + "]"));
                Logger.Log(ex);
            }
        }

        #endregion

        #region Public

        protected void AddToMap(
            Dictionary<string, List<T>> dataMap,
            int i,
            object currLockObj,
            int intKeyCol,
            List<object[]> data)
        {
            try
            {
                var strKey =
                    (data[i] == null ||
                    data[i][intKeyCol] == null)
                        ? string.Empty
                        : (string)data[i][intKeyCol];

                T tobj = GetRow(data, i);
                lock (currLockObj)
                {
                    if (!string.IsNullOrEmpty(strKey))
                    {
                        List<T> currList;
                        if (!dataMap.TryGetValue(strKey, out currList))
                        {
                            currList = new List<T>();
                            dataMap[strKey] = currList;
                        }
                        currList.Add(tobj);
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        protected abstract T GetRow(List<object[]> data, int i);

        public List<T> LoadAllData()
        {
            try
            {
                if (string.IsNullOrEmpty(m_strDefaultIndex))
                {
                    throw new HCException("Index not found");
                }
                StringBuilder sb = GetSelectAllFrom();
                return LoadDataFromQuery(sb.ToString());
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return new List<T>();
        }

        public Dictionary<string, List<T>> LoadAllDataMap()
        {
            if (string.IsNullOrEmpty(m_strDefaultIndex))
            {
                throw new HCException("Index not found");
            }
            var sb = GetSelectAllFrom();
            return LoadDataMap(sb.ToString());
        }

        public List<T> LoadDataFromWhere(string strWhere)
        {
            try
            {
                if (string.IsNullOrEmpty(m_strDefaultIndex))
                {
                    throw new HCException("Index not found");
                }
                var sb = GetSelectAllFrom();
                sb.Append(" where " + strWhere);
                return LoadDataFromQuery(sb.ToString());
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return new List<T>();
        }

        public Dictionary<string, List<T>> LoadDataMapFromWhere(string strWhere)
        {
            try
            {
                if (string.IsNullOrEmpty(m_strDefaultIndex))
                {
                    throw new HCException("Index not found");
                }
                var sb = GetSelectAllFrom();
                sb.Append(" where " + strWhere);
                return LoadDataMap(sb.ToString());
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return new Dictionary<string, List<T>>();
        }


        public int GetCountFromWhere(string strWhere)
        {
            try
            {
                if (string.IsNullOrEmpty(m_strDefaultIndex))
                {
                    throw new HCException("Index not found");
                }
                string strQuery = "select count(" + m_strDefaultIndex  + 
                    ") from " + m_strTableName +
                    " where " + strWhere;
                return (int)ExecuteScalar<long>(strQuery);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return 0;
        }

        public TaskWrapper Insert(string strKey, T obj)
        {
            return Insert(strKey, new List<T>(new[] {obj}));
        }

        public List<T> LoadDataFromKeys(List<string> strKeys)
        {
            if (string.IsNullOrEmpty(m_strDefaultIndex))
            {
                throw new HCException("Index not found");
            }
            var sb = GetSelectAllFrom();
            sb.Append(" where " + m_strDefaultIndex + " IN (" +
                      GetInStatement(strKeys) + ")");
            return LoadDataFromQuery(sb.ToString());
        }

        public List<T> LoadDataFromKey(string strKey)
        {
            try
            {
                if (string.IsNullOrEmpty(m_strDefaultIndex))
                {
                    throw new HCException("Index not found");
                }
                var sb = GetSelectAllFrom();

                sb.Append("where " + m_strDefaultIndex + " = '" +
                          strKey.Replace("'", string.Empty) + "'");
                return LoadDataFromQuery(sb.ToString());
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
                Logger.Log(new HCException("Exception in database [" +
                    FileName +
                    "]"));
            }
            return new List<T>();
        }

        public void DropDefaultIndex()
        {
            try
            {
                const string strQuery = "drop index if exists " +
                                              SqliteConstants.KEY_COL_NAME + "_INDEX ";
                SqLiteTaskQueues.EnqueueWrite(
                    m_strTableName,
                    false,
                    FileName,
                    this,
                    strQuery,
                    false).Wait();

                const string strMessage = "Dropped index in SqLite [" + strQuery + "]";
                Logger.Log(strMessage);
                Console.WriteLine(strMessage);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                Logger.Log(new HCException("Exception in database [" +
                    FileName +
                    "]"));
            }
        }

        public void CreateIndex(string strIndex)
        {
            try
            {
                string strSql = "create index if not exists " +
                             m_strTableName + "_" + SqliteConstants.KEY_COL_NAME + "_INDEX ON " +
                             m_strTableName + " (" + SqliteConstants.KEY_COL_NAME + ")";
                
                SqLiteTaskQueues.EnqueueWrite(
                    m_strTableName,
                    false,
                    FileName,
                    this,
                    strSql,
                    false).Wait();
            }
            catch (Exception ex)
            {
                Logger.Log(new HCException("Exception in database [" +
                    FileName +
                    "]"));
                Logger.Log(ex);
            }
        }

        public void ShrinkDb()
        {
            try
            {
                const string strSql = "vacuum";

                SqLiteTaskQueues.EnqueueWrite(
                    m_strTableName,
                    false,
                    FileName,
                    this,
                    strSql,
                    false).Wait();
            }
            catch (Exception ex)
            {
                Logger.Log(new HCException("Exception in database [" +
                    FileName +
                    "]"));
                Logger.Log(ex);
            }
        }

        public void TrunkateTable(string strTableName)
        {
            try
            {
                string strSql = "delete from " + strTableName;
                
                SqLiteTaskQueues.EnqueueWrite(
                    m_strTableName,
                    false,
                    FileName,
                    this,
                    strSql,
                    false).Wait();
            }
            catch (Exception ex)
            {
                Logger.Log(new HCException("Exception in database [" +
                    FileName +
                    "]"));
                Logger.Log(ex);
            }
        }

        public void DropTable(string strTableName)
        {
            try
            {
                string strSql = "drop table if exists " + strTableName;
                
                SqLiteTaskQueues.EnqueueWrite(
                    m_strTableName,
                    false,
                    FileName,
                    this,
                    strSql,
                    false).Wait();
            }
            catch (Exception ex)
            {
                Logger.Log(new HCException("Exception in database [" +
                    FileName +
                    "]"));
                Logger.Log(ex);
            }
        }

        public List<string> LoadAllKeys()
        {
            return LoadAllKeys(string.Empty);
        }

        public List<string> LoadAllKeys(string strWhere)
        {
            try
            {
                string strQuery = "select " + m_strDefaultIndex +
                                  " from [" + m_strTableName + "] " +
                                  (string.IsNullOrEmpty(strWhere)
                                       ? string.Empty
                                       : " where " + strWhere);

                var data = new List<object[]>();
                Execute(strQuery, data);
                var outList = new List<string>();

                for (int i = 0; i < data.Count; i++)
                {
                    outList.Add(data[i][0].ToString());
                }

                return outList;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return new List<string>();
        }

        public void Delete(string strKey)
        {
            try
            {
                string strQuery = "DELETE FROM [" + m_strTableName + "] WHERE " +
                               m_strDefaultIndex + " = '" +
                               strKey.Replace("'", string.Empty) + "'";

                SqLiteTaskQueues.EnqueueWrite(
                    m_strTableName,
                    false,
                    FileName,
                    this,
                    strQuery,
                    false).Wait();
            }
            catch (Exception ex)
            {
                Logger.Log(new HCException("Exception in database [" +
                    FileName +
                    "]"));
                Logger.Log(ex);
            }
        }

        public void DeleteFromWhere(string strWhere)
        {
            try
            {
                string strQuery = "DELETE FROM [" + m_strTableName + "] WHERE " +
                               strWhere;

                SqLiteTaskQueues.EnqueueWrite(
                    m_strTableName,
                    false,
                    FileName,
                    this,
                    strQuery,
                    false).Wait();
            }
            catch (Exception ex)
            {
                Logger.Log(new HCException("Exception in database [" +
                    FileName +
                    "]"));
                Logger.Log(ex);
            }
        }

        public void Clear()
        {
            try
            {
                var strQuery = "DELETE FROM [" + m_strTableName + "]";
                
                SqLiteTaskQueues.EnqueueWrite(
                    m_strTableName,
                    false,
                    FileName,
                    this,
                    strQuery,
                    false).Wait();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                Logger.Log(new HCException("Exception in database [" +
                    FileName +
                    "]"));
            }
        }


        public void Delete(List<string> strKeys)
        {
            try
            {
                string strQuery = "DELETE FROM [" + m_strTableName + "] WHERE " +
                               m_strDefaultIndex + " IN (" +
                               GetInStatement(strKeys) + ")";
                
                SqLiteTaskQueues.EnqueueWrite(
                    m_strTableName,
                    false,
                    FileName,
                    this,
                    strQuery,
                    false).Wait();
            }
            catch (Exception ex)
            {
                Logger.Log(new HCException("Exception in database [" +
                    FileName +
                    "]"));
                Logger.Log(ex);
            }
        }

        #endregion

        #region Private

        private void CreateIndexes()
        {
            try
            {
                if (m_strIndexes != null)
                {
                    foreach (string strIndex in m_strIndexes)
                    {
                        CreateIndex(strIndex);
                    }
                }

                if (!string.IsNullOrEmpty(m_strDefaultIndex))
                {
                    CreateIndex(m_strDefaultIndex);
                }
            }
            catch(Exception ex)
            {
                Logger.Log(new HCException("Exception in database [" +
                    FileName +
                    "]"));
                Logger.Log(ex);
            }
        }

        public StringBuilder GetSelectAllFrom()
        {
            try
            {
                var sb = new StringBuilder(
                    "select * ");
                sb.Append(" from [" + m_strTableName +
                          "] ");
                return sb;
            }
            catch(Exception ex)
            {
                Logger.Log(new HCException("Exception in database [" +
                    FileName +
                    "]"));
                Logger.Log(ex);
            }
            return null;
        }

        public static string GetInStatement(List<string> list)
        {
            try
            {
                var sb = new StringBuilder();
                var blnIsTitle = true;
                foreach (string str in list)
                {if(string.IsNullOrEmpty( str ))
                {
                    continue;
                }
                    if (!blnIsTitle)
                    {
                        sb.Append(",");
                    }
                    blnIsTitle = false;
                    sb.Append("'" + str.Replace("'", string.Empty) + "'");
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return string.Empty;
        }

        #endregion

        #region Abstract methods

        public void Execute(
            string strQuery,
            List<object[]> data)
        {
            string[] colNames;
            Execute(
                strQuery,
                data,
                false,
                out colNames);
        }

        public void Execute(
            string strQuery,
            List<object[]> data,
            bool blnLoadCols,
            out string[] colNames)
        {
            colNames = null;
            try
            {
                SqLiteTaskQueues.EnqueueLoad(
                    strQuery,
                    data,
                    FileName,
                    this,
                    false,
                    blnLoadCols,
                    out colNames);
            }
            catch (Exception ex)
            {
                Logger.Log(new HCException("Exception in database [" +
                    FileName +
                    "]"));
                Logger.Log(ex);
            }
        }

        public abstract List<TaskWrapper> Insert(Dictionary<string, T> objs);

        public abstract List<TaskWrapper> Insert(Dictionary<string, List<T>> objs);

        public abstract TaskWrapper Insert(
            string strKey,
            List<T> objs);

        public abstract void ValidateTable();

        public abstract List<T> LoadDataFromQuery(string strQuery);

        public bool ContainsKey(string strKey)
        {
            try
            {
                if (string.IsNullOrEmpty(m_strDefaultIndex))
                {
                    throw new HCException("Key not found");
                }

                if (string.IsNullOrEmpty(strKey))
                {
                    throw new HCException("Null key [" + FileName + "]");
                }

                strKey = strKey.Replace("'", string.Empty);

                string strQuery = "SELECT EXISTS(select " + m_strDefaultIndex +
                                " from [" + m_strTableName +
                               "] where " + m_strDefaultIndex + " = '" + strKey + "' LIMIT 1)";

                return ExecuteScalar<long>(strQuery) == 1L;
            }
            catch (Exception ex)
            {
                Logger.Log(new HCException("Exception in database [" +
                    FileName +
                    "]"));
                Logger.Log(ex);
            }
            return false;
        }

        public string GetTableName()
        {
            return m_strTableName;
        }

        public Dictionary<string, List<T>> LoadDataMap(string strQuery)
        {
            try
            {
                var data = new List<object[]>();
                SqLiteTaskQueues.EnqueueLoad(
                    strQuery,
                    data,
                    FileName,
                    this,
                    false);

                const int intKeyCol = 0;
                var dataMap = new Dictionary<string, List<T>>();
                var currLockObj = new object();
                for (int i = 0; i < data.Count; i++)
                {
                    try
                    {
                        AddToMap(dataMap,
                            i,
                            currLockObj,
                            intKeyCol,
                            data);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                    }
                }
                return dataMap;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return new Dictionary<string, List<T>>();
        }
        
        #endregion

        public string[] GetColNames()
        {
            try
            {
                if (string.IsNullOrEmpty(m_strTableName))
                {
                    throw new HCException("Table not found");
                }
                string strQuery = "SELECT sql FROM sqlite_master " +
                                  " WHERE tbl_name = " + "'" + m_strTableName + "'" +
                                  " AND type = " + "'" + "table" + "'";
                
                var data = new List<object[]>();
                string[] colNames;
                SqLiteTaskQueues.EnqueueLoad(
                    strQuery,
                    data,
                    FileName,
                    this,
                    false,
                    true,
                    out colNames);

                return colNames;
            }
            catch(Exception ex)
            {
                Logger.Log(new HCException("Exception in database [" +
                    FileName +
                    "]"));
                Logger.Log(ex);
            }
            return null;
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            try
            {
                DisposeLock.AcquireWriterLock(
                    SqliteConstants.DISPOSE_TIME_OUT_MILLSECS);
                try
                {
                    if (IsDisposed)
                    {
                        return;
                    }
                    IsDisposed = true;
                    EventHandlerHelper.RemoveAllEventHandlers(this);
                    m_readWritePropertyNames = null;
                    m_reflector = null;
                    m_strDefaultIndex = null;
                    m_strIndexes = null;
                    m_strTableName = null;
                    FileName = null;
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
                finally
                {
                    DisposeLock.ReleaseWriterLock();
                }
            }
            catch(Exception ex)
            {
                Logger.Log(new HCException("Exception in database [" +
                    FileName +
                    "]"));
                Logger.Log(ex);
            }
            finally
            {
                IsDisposed = true;
                DisposeLock = null;
            }
        }


        public ISqLiteCacheBase GetCacheWrapperBase()
        {
            try
            {
                string strFileName = FileName;
                if (string.IsNullOrEmpty(strFileName))
                {
                    Console.WriteLine("SQLITE Null db name!");
                    return null;
                }
                return SqLiteConnectionPool.GetCacheWrapper0<T>(strFileName);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }
    }
}



