#region

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using HC.Core.Io.Serialization.Interfaces;
using HC.Core.Logging;
using HC.Core.Threading.ProducerConsumerQueues.Support;
using HC.Core.Zip;

#endregion

namespace HC.Core.Cache.SqLite
{
    public class SqLiteCacheBLob<T> : ASqliteCache<T>
    {
        #region Constructors

        public SqLiteCacheBLob(
            string strFileName)
            : this(strFileName, typeof(T).Name, null, SqliteConstants.KEY_COL_NAME) { }

        public SqLiteCacheBLob(string strFileName,
            string strTableName,
            string[] strIndexes,
            string strDefaultIndex)
            : base(strFileName,
                    strTableName,
                    strIndexes,
                    strDefaultIndex)
        {
        }

        #endregion

        #region Public

        public override KeyValuePair<string, DbType>[] ColNameToTypeArr { get; set; }

        private readonly Verboser m_verboser = new Verboser();

        public override object[][] GetImportArrBLob(
            List<KeyValuePair<string, object>> importList,
            bool blnUseCompression)
        {
            throw new NotImplementedException();
        }

        public override object[][] GetImportArr(List<KeyValuePair<string, object>> importList,
            bool blnUseCompression)
        {
            throw new NotImplementedException();
        }

        public override void GetColNameToTypeArr(string p)
        {
            throw new NotImplementedException();
        }

        protected override T GetRow(List<object[]> data, int i)
        {
            try
            {
                const int intIndeOfObj = 1;
                var instance = (ISerializable)m_reflector.CreateInstance();
                var bytes = (byte[]) data[i][intIndeOfObj];
                if (UseCompression)
                {
                    bytes =
                        (byte[]) MemoryZipper.UnZipMemory(new MemoryStream(bytes));
                }
                var obj = (T) instance.Deserialize(bytes);
                data[i][0] = null;
                data[i][intIndeOfObj] = null;
                return obj;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return default(T);
        }


        public override List<TaskWrapper> Insert(Dictionary<string, T> objs)
        {
            var map = objs.ToDictionary(t => t.Key, t => new List<T>(new[] {t.Value}));
            return Insert(map);
        }

        public override List<TaskWrapper> Insert(Dictionary<string, List<T>> objs)
        {
            try
            {
                var keyValuePairs = new List<KeyValuePair<string, object>>();
                foreach (KeyValuePair<string, List<T>> keyValuePair in objs)
                {
                    foreach (T tObj in keyValuePair.Value)
                    {
                        keyValuePairs.Add(
                            new KeyValuePair<string, object>(
                                keyValuePair.Key.Replace("'", string.Empty),
                                tObj));
                    }
                    keyValuePair.Value.Clear();
                }
                var tasks = new List<TaskWrapper>
                                {
                                    SqLiteAdoWrapper<T>.BulkInsertBLob(
                                        m_strTableName,
                                        keyValuePairs,
                                        FileName,
                                        this,
                                        UseCompression)
                                };
                objs.Clear();
                return tasks;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return new List<TaskWrapper>();
        }

        public override TaskWrapper Insert(
            string strKey,
            List<T> objs)
        {
            try
            {
                var logTime = DateTime.Now;
                strKey = strKey.Replace("'", string.Empty);
                int intObjCounter = objs.Count;
                var kvpList = new List<KeyValuePair<string, object>>();
                for (int i = 0; i < objs.Count; i++)
                {
                    kvpList.Add(
                        new KeyValuePair<string, object>(
                            strKey,
                            objs[i]));
                }
                TaskWrapper task = SqLiteAdoWrapper<T>.BulkInsertBLob(
                    m_strTableName,
                    kvpList,
                    FileName,
                    this,
                    UseCompression);
                string strMessage = "Inserted [" + intObjCounter + "] rows. File [" +
                                  new FileInfo(FileName).Name + "] Time [" +
                                  (DateTime.Now - logTime).TotalSeconds + "] secs";
                m_verboser.DoTalk(strMessage);
                Logger.Log(strMessage);
                return task;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        #endregion

        #region Private & protected

        public override void ValidateTable()
        {
            try
            {
                var strSql = "create table if not exists " +
                             m_strTableName + " (" + SqliteConstants.KEY_COL_NAME +
                             " varchar(100), Obj BLOB)";
                SqLiteTaskQueues.EnqueueWrite(
                    m_strTableName,
                    true,
                    FileName,
                    this,
                    strSql,
                    false).Wait();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public override List<T> LoadDataFromQuery(string strQuery)
        {
            try
            {
                return SqLiteAdoWrapper<T>.LoadBLob(
                    strQuery,
                    FileName,
                    this);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return new List<T>();
        }

        #endregion
    }
}