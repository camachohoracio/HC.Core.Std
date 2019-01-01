#region

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HC.Core.Exceptions;
using HC.Core.Io;
using HC.Core.Logging;
using HC.Core.Threading;
using HC.Core.Threading.ProducerConsumerQueues;
using HC.Core.Threading.ProducerConsumerQueues.Support;

#endregion

namespace HC.Core.Cache.SqLite
{
    public static class SqLiteTaskQueues
    {

        #region Members

        private static readonly Dictionary<string, EfficientWorkerManager<SqLiteWriteJob>> m_writerQueueMap;
        private static readonly Dictionary<string, IThreadedQueue<SqLiteReadJob>> m_readerQueueMap;
        private static readonly Dictionary<string, string> m_mapFileNameToDriveLetter;
        private static DateTime m_lastReadLogTime;
        private static DateTime m_lastWriteLogTime;
        private static readonly object m_lockObj = new object();
        private static readonly Verboser m_verboser = new Verboser();
 
        #endregion

        #region Constructors

        static SqLiteTaskQueues()
        {
            m_writerQueueMap = new Dictionary<string, EfficientWorkerManager<SqLiteWriteJob>>();
            m_readerQueueMap = new Dictionary<string, IThreadedQueue<SqLiteReadJob>>();
            m_mapFileNameToDriveLetter = new Dictionary<string, string>();
        }

        #endregion

        #region Public 

        private static IThreadedQueue<SqLiteReadJob> GetReaderQueue(
            string strFileName)
        {
            try
            {
                IThreadedQueue<SqLiteReadJob> readerQueue;
                string strDriveLetter = GetDriveLetter(strFileName);
                if (!m_readerQueueMap.TryGetValue(strDriveLetter, out readerQueue))
                {
                    lock (m_lockObj)
                    {
                        if (!m_readerQueueMap.TryGetValue(strDriveLetter, out readerQueue))
                        {
                            readerQueue = new ProducerConsumerQueue<SqLiteReadJob>(
                                SqliteConstants.DB_READ_THREAD_SIZE,
                                SqliteConstants.DB_QUEUE_CAPACITY,
                                false,
                                false);
                            ((ProducerConsumerQueue<SqLiteReadJob>) readerQueue).Id = strDriveLetter;
                            readerQueue.OnWork += sqLiteReadJob =>
                                {
                                    if (sqLiteReadJob.ExecuteRead ||
                                        sqLiteReadJob.ExecuteScalar)
                                    {
                                        ExecuteRead(sqLiteReadJob);
                                    }
                                    else
                                    {
                                        ExecuteDataLoad(sqLiteReadJob);
                                    }
                                };
                            m_readerQueueMap[strDriveLetter] = readerQueue;
                            string strMessage = "Loaded read queue for drive letter [" +
                                                strDriveLetter + "]";
                            Console.WriteLine(strMessage);
                            Logger.Log(strMessage);
                        }
                    }
                }
                return readerQueue;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
                Logger.Log(new HCException("Error in database [" +
                    strFileName +
                    "]"));
            }
            return null;
        }

        private static void ExecuteRead(
            SqLiteReadJob sqLiteReadJob)
        {
            string strSql = string.Empty;
            ReaderWriterLock disposeLock = null;
            try
            {
                ISqLiteCacheBase cacheBase = sqLiteReadJob.SqLiteCacheBase;
                if (cacheBase == null)
                {
                    throw new HCException("Cache base already null!");
                }
                strSql = sqLiteReadJob.Query;
                var dbConn0 = sqLiteReadJob.SqLiteCacheBase;
                ISqLiteCacheBase cacheWrapper = 
                    dbConn0 == null ? null : dbConn0.GetCacheWrapperBase();
                disposeLock = cacheWrapper == null ?
                    null : cacheWrapper.DisposeLock;

                if (disposeLock != null)
                {
                    disposeLock.AcquireReaderLock(
                        SqliteConstants.DISPOSE_TIME_OUT_MILLSECS);
                }

                while (cacheWrapper == null ||
                       cacheWrapper.IsDisposed ||
                       disposeLock == null)
                {
                    if (disposeLock != null)
                    {
                        disposeLock.ReleaseReaderLock();
                        disposeLock = null;
                    }
                    //
                    // cache alread disposed, get a new connection from connection pool
                    //
                    cacheWrapper = cacheBase.GetCacheWrapperBase();
                    disposeLock = cacheWrapper.DisposeLock;
                    if (disposeLock == null ||
                        cacheWrapper.IsDisposed)
                    {
                        cacheWrapper = null;
                    }
                    else
                    {
                        disposeLock.AcquireReaderLock(
                            SqliteConstants.DISPOSE_TIME_OUT_MILLSECS);
                    }
                }


                //if (disposeLock == null)
                //{
                //    //
                //    // get a provitional lock, just to keep code running
                //    //
                //    disposeLock = new ReaderWriterLock();
                //}

                try
                {
                    if ((DateTime.Now - m_lastReadLogTime).TotalSeconds > 5)
                    {
                        IThreadedQueue<SqLiteReadJob> queue = sqLiteReadJob.ReaderQueue;
                        if (queue != null)
                        {
                            int intQueueSize = sqLiteReadJob.ReaderQueue.QueueSize;
                            if (intQueueSize > 0)
                            {
                                
                                Console.WriteLine("****Read queue size [" +
                                                  intQueueSize + "][" +
                                                  queue.Id  + "]");
                            }
                            m_lastReadLogTime = DateTime.Now;
                        }
                    }


                    SQLiteConnection dbConn = cacheWrapper.DbConn;

                    if (dbConn == null)
                    {
                        throw new HCException("Null connection");
                    }

                    if (sqLiteReadJob.ExecuteScalar)
                    {
                        using (SQLiteCommand cmd = dbConn.CreateCommand())
                        {
                            cmd.CommandTimeout = SqliteConstants.TIME_OUT;
                            cmd.CommandText = strSql;
                            object result = cmd.ExecuteScalar();
                            sqLiteReadJob.Data.Add(
                                new[]{result});
                        }
                    }
                    else
                    {
                        using (SQLiteCommand cmd = dbConn.CreateCommand())
                        {
                            cmd.CommandTimeout = SqliteConstants.TIME_OUT;
                            cmd.CommandText = strSql;
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                finally
                {
                    ReleaseReaderLock(disposeLock);
                    disposeLock = null;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(
                    new HCException("Exception in db [ " + 
                        sqLiteReadJob.FileName + "] sql [" +
                        strSql + "]"));
                Logger.Log(ex);
            }
            finally
            {
                ReleaseReaderLock(disposeLock);
            }
        }

        private static void ReleaseReaderLock(ReaderWriterLock disposeLock)
        {
            try
            {
                if (disposeLock != null)
                {
                    disposeLock.ReleaseReaderLock();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static EfficientWorkerManager<SqLiteWriteJob> GetWriterQueue(
            string strFileName)
        {
            try
            {
                string strDriveLetter = GetDriveLetter(strFileName);
                EfficientWorkerManager<SqLiteWriteJob> writerQueue;
                if (!m_writerQueueMap.TryGetValue(strDriveLetter, out writerQueue))
                {
                    lock (m_lockObj)
                    {
                        if (!m_writerQueueMap.TryGetValue(strDriveLetter, out writerQueue))
                        {
                            writerQueue = new EfficientWorkerManager<SqLiteWriteJob>(1, 20);
                            writerQueue.Id = strDriveLetter;

                            writerQueue.OnWork += item =>
                                {
                                    if (item.IsWriteTask)
                                    {
                                        ExecuteWrite(item);
                                    }
                                    else
                                    {
                                        ExecuteBulkInsert(item);
                                    }
                                };
                            m_writerQueueMap[strDriveLetter] = writerQueue;
                            string strMessage = "Loaded writer queue for drive letter: " +
                                                strDriveLetter;
                            Console.WriteLine(strMessage);
                            Logger.Log(strMessage);
                        }
                    }
                }
                return writerQueue;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
                Logger.Log(new HCException("Error in database [" +
                    strFileName +
                    "]"));
            }
            return null;
        }

        public static void EnqueueLoad(
            string strSql,
            List<object[]> data,
            string strFileName,
            ISqLiteCacheBase parentConnection,
            bool blnIsScalar)
        {
            string[] colNames;
            EnqueueLoad(
                strSql,
                data,
                strFileName,
                parentConnection,
                blnIsScalar,
                false,
                out colNames);
        }

        public static void EnqueueLoad(
            string strSql,
            List<object[]> data,
            string strFileName,
            ISqLiteCacheBase parentConnection,
            bool blnIsScalar,
            bool blnLoadColNames,
            out string[] colNames)
        {
            colNames = new string[] { };
            try
            {
                colNames = null;
                // note: do not lock anthing that comes to this methd, the lock is done when the task is consumed
                IThreadedQueue<SqLiteReadJob> readerQueue = 
                    GetReaderQueue(strFileName);
                using (var readJob = new SqLiteReadJob
                    {
                        FileName = strFileName,
                        SqLiteCacheBase = parentConnection,
                        Query = strSql,
                        ReaderQueue = readerQueue,
                        LoadColNames = blnLoadColNames
                    })
                {
                    readJob.ExecuteScalar = blnIsScalar;

                    using (TaskWrapper task = readerQueue.EnqueueTask(
                        readJob))
                    {
                        try
                        {
                            task.Wait();
                            data.AddRange(readJob.Data);
                            readJob.Data.Clear();
                            colNames = readJob.ColNames;
                            task.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(new HCException("Error in database [" +
                                                       strFileName +
                                                       "]"));
                            Logger.Log(ex);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
                Logger.Log(new HCException("Error in database [" +
                    strFileName +
                    "]"));
            }
        }

        public static TaskWrapper EnqueueWrite(
            string strTableName,
            bool blnIsBlob,
            string strFileName,
            ISqLiteCacheBase parentConnection,
            string strQuery,
            bool blnUseCompression)
        {
            return EnqueueWrite0(
                strTableName,
                null,
                blnIsBlob,
                strFileName,
                parentConnection,
                strQuery,
                true,
                blnUseCompression);
        }

        public static TaskWrapper EnqueueImport(
            string strTableName,
            List<KeyValuePair<string, object>> importArr,
            bool blnIsBlob,
            string strFileName,
            ISqLiteCacheBase parentConnection,
            bool blnUseCompression)
        {
            return EnqueueWrite0(
                strTableName,
                importArr,
                blnIsBlob,
                strFileName,
                parentConnection,
                string.Empty,
                false,
                blnUseCompression);
        }

        private static TaskWrapper EnqueueWrite0(
            string strTableName,
            List<KeyValuePair<string, object>> importArr,
            bool blnIsBlob,
            string strFileName,
            ISqLiteCacheBase parentConnection,
            string strQuery,
            bool blnIsWriteTask,
            bool blnUseCompression)
        {
            try
            {
                EfficientWorkerManager<SqLiteWriteJob> writerQueue = GetWriterQueue(strFileName);

                string strKey = "InsertKey" + strFileName;
                lock (LockObjectHelper.GetLockObject(strKey)) // only enqueue one at a time
                {
                    SqLiteWriteJob writeJob;
                    if (!blnIsWriteTask)
                    {
                        if (writerQueue.TryGetValue(strKey, out writeJob))
                        {
                            //
                            // merge import
                            //
                            lock (writeJob.LockObj)
                            {
                                TaskWrapper parentTask = null;
                                while (parentTask == null)
                                {
                                    parentTask = writeJob.TaskWrapper;
                                }
                                if (!writeJob.IsConsumed)
                                {
                                    writeJob.ImportList.AddRange(importArr);
                                    string strMessage = "=> import merged [" +
                                                        writeJob.ImportList.Count + "][" +
                                                        new FileInfo(strFileName).Name + "]...";
                                    Verboser.Talk(strMessage);
                                    var tf = new TaskFactory(
                                        TaskCreationOptions.AttachedToParent,
                                        TaskContinuationOptions.AttachedToParent);
                                    var currTask =
                                        new TaskWrapper(
                                            tf.ContinueWhenAll(new[]
                                                                   {
                                                                       parentTask.Task
                                                                   }, 
                                                                   dummyTask => Verboser.WriteLine(
                                                                                            "Data import task merged [" + new FileInfo(strFileName).Name + "]")),
                                            null);

                                    return currTask;
                                }
                                // else LockObj came too late and the item has been already consumed
                            }
                        }

                        if (writerQueue.TryGetValue(strKey, out writeJob))
                        {
                            throw new HCException("Item [" + strKey + "] already in the queue");
                        }
                    }

                    writeJob = new SqLiteWriteJob
                    {
                        DbConn = parentConnection,
                        //ImportList = importArr == null ? importArr.ToList(),
                        TableName = strTableName,
                        FileName = strFileName,
                        WriterQueue = writerQueue,
                        LockObj = new object(),
                        IsBLob = blnIsBlob,
                        Query = strQuery,
                        IsWriteTask = blnIsWriteTask,
                        UseCompression = blnUseCompression
                    };

                    if (importArr != null)
                    {
                        writeJob.ImportList.AddRange(importArr);
                    }

                    TaskWrapper newTask = writerQueue.AddItem(
                        strKey,
                        writeJob);

                    writeJob.TaskWrapper = newTask;

                    return newTask;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(new HCException("Exception in database [" +
                    strFileName +
                    "]"));
                Logger.Log(ex);
            }
            finally
            {
                try
                {
                    if (importArr != null)
                    {
                        importArr.Clear();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            return null;
        }

        #endregion

        #region Private

        private static string GetDriveLetter(string strFileName)
        {
            string strDriveLetter;
            if (!m_mapFileNameToDriveLetter.TryGetValue(strFileName, out strDriveLetter))
            {
                lock (m_lockObj)
                {
                    if (!m_mapFileNameToDriveLetter.TryGetValue(strFileName, out strDriveLetter))
                    {
                        strDriveLetter = FileHelper.GetDriveLetter(strFileName).ToLower();
                        m_mapFileNameToDriveLetter[strFileName] = strDriveLetter;
                    }
                }
            }
            return strDriveLetter;
        }

        private static void ExecuteWrite(
            SqLiteWriteJob sqLiteWriteJob)
        {
            ExecuteWrite0(
                sqLiteWriteJob,
                false);
        }

        private static void ExecuteBulkInsert(
            SqLiteWriteJob sqLiteWriteJob)
        {
            ExecuteWrite0(
                sqLiteWriteJob,
                true);
        }

        private static void ExecuteWrite0(
            SqLiteWriteJob sqLiteWriteJob,
            bool blnIsBulkInsert)
        {
            try
            {
                var logTime = DateTime.Now;
                lock (sqLiteWriteJob.LockObj) // posible merge of imports
                {
                    string strMessage;
                    sqLiteWriteJob.IsConsumed = true;
                    if ((DateTime.Now - m_lastWriteLogTime).TotalSeconds > 5)
                    {
                        int intQueueSize = sqLiteWriteJob.WriterQueue.QueueSize;
                        if (intQueueSize > 0)
                        {
                            strMessage = "****Write queue size [" + intQueueSize +"][" +
                                sqLiteWriteJob.WriterQueue.Id + "]" ;
                            Console.WriteLine(strMessage);
                            Logger.Log(strMessage);
                        }
                        m_lastWriteLogTime = DateTime.Now;
                    }


                    ISqLiteCacheBase cacheWrapper = sqLiteWriteJob.DbConn.GetCacheWrapperBase();

                    ReaderWriterLock disposeLock = cacheWrapper.DisposeLock;

                    if(disposeLock == null)
                    {
                        //
                        // get a provitional lock, just to keep code running
                        //
                        disposeLock = new ReaderWriterLock();
                    }

                    disposeLock.AcquireReaderLock(SqliteConstants.DISPOSE_TIME_OUT_MILLSECS);
                    try
                    {
                        while (cacheWrapper.IsDisposed ||
                            disposeLock == null)
                        {
                            if (disposeLock != null)
                            {
                                disposeLock.ReleaseReaderLock();
                            }
                            //
                            // cache alread disposed, get a new connection from connection pool
                            //
                            cacheWrapper = sqLiteWriteJob.DbConn.GetCacheWrapperBase();
                            disposeLock = cacheWrapper.DisposeLock;
                            if (disposeLock != null)
                            {
                                disposeLock.AcquireReaderLock(SqliteConstants.DISPOSE_TIME_OUT_MILLSECS);
                            }
                        }

                        lock (LockObjectHelper.GetLockObject(sqLiteWriteJob.FileName + "_BulkInsert"))
                        {
                            if (blnIsBulkInsert)
                            {
                                cacheWrapper.GetColNameToTypeArr(sqLiteWriteJob.TableName);
                                //
                                // get impot array
                                //
                                List<KeyValuePair<string, object>> importList0 = sqLiteWriteJob.ImportList;

                                if (importList0 != null &&
                                    importList0.Count > 0)
                                {
                                    object[][] importList;
                                    if (sqLiteWriteJob.IsBLob)
                                    {
                                        importList = cacheWrapper.GetImportArrBLob(
                                            importList0,
                                            sqLiteWriteJob.UseCompression);
                                    }
                                    else
                                    {
                                        importList = cacheWrapper.GetImportArr(
                                            importList0,
                                            sqLiteWriteJob.UseCompression);
                                    }
                                    importList0.Clear();
                                    sqLiteWriteJob.ImportList = null;


                                    using (var bulkInsert =
                                        new SqLiteBulkInsert(cacheWrapper.DbConn,
                                            sqLiteWriteJob.TableName,
                                            sqLiteWriteJob.FileName))
                                    {
                                        //
                                        // add parameters
                                        //
                                        bulkInsert.AddParameter(SqliteConstants.KEY_COL_NAME, DbType.String);

                                        if (sqLiteWriteJob.IsBLob)
                                        {
                                            bulkInsert.AddParameter("Obj", DbType.Binary);
                                        }
                                        else
                                        {
                                            KeyValuePair<string, DbType>[] colNameToTypeArr =
                                                cacheWrapper.ColNameToTypeArr;

                                            foreach (KeyValuePair<string, DbType> keyValuePair in
                                                colNameToTypeArr)
                                            {
                                                bulkInsert.AddParameter(keyValuePair.Key, keyValuePair.Value);
                                            }
                                        }

                                        //
                                        // insert data
                                        //
                                        for (int i = 0; i < importList.Length; i++)
                                        {
                                            bulkInsert.Insert(
                                                importList[i],
                                                sqLiteWriteJob.FileName);
                                            importList[i] = null;
                                        }

                                        strMessage = "Inserted [" + importList.Length +
                                                     "] rows as blob. File: " +
                                                     sqLiteWriteJob.FileName + " Time = " +
                                                     (DateTime.Now - logTime).TotalSeconds + " secs";
                                        m_verboser.DoTalk(strMessage);
                                        Logger.Log(strMessage);
                                    }
                                }
                            }
                            else
                            {
                                using (SQLiteCommand cmd = cacheWrapper.DbConn.CreateCommand())
                                {
                                    cmd.CommandTimeout = SqliteConstants.TIME_OUT;
                                    cmd.CommandText = sqLiteWriteJob.Query;
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                    finally
                    {
                        ReleaseWriteLock(disposeLock);
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Log(new HCException("Error in database [" +
                    sqLiteWriteJob.FileName +
                    "]"));
                Logger.Log(ex);
            }
            finally
            {
                DisposeWriteJob(sqLiteWriteJob);
            }
        }

        private static void DisposeWriteJob(SqLiteWriteJob sqLiteWriteJob)
        {
            try
            {
                sqLiteWriteJob.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void ReleaseWriteLock(ReaderWriterLock disposeLock)
        {
            try
            {
                if (disposeLock != null)
                {
                    disposeLock.ReleaseReaderLock();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static void ExecuteDataLoad(SqLiteReadJob sqLiteReadJob)
        {
            ReaderWriterLock disposeLock = null;
            try
            {
                string strSql = sqLiteReadJob.Query;
                ISqLiteCacheBase sqLiteCacheBase = sqLiteReadJob.SqLiteCacheBase;
                ISqLiteCacheBase cacheWrapper = sqLiteCacheBase == null ? null : sqLiteCacheBase.GetCacheWrapperBase();
                disposeLock = cacheWrapper == null ? 
                    null : cacheWrapper.DisposeLock;

                if (disposeLock == null)
                {
                    //
                    // get a provitional lock, just to keep code running
                    //
                    disposeLock = new ReaderWriterLock();
                }

                disposeLock.AcquireReaderLock(SqliteConstants.DISPOSE_TIME_OUT_MILLSECS);
                try
                {
                    if ((DateTime.Now - m_lastReadLogTime).TotalSeconds > 5)
                    {
                        int intQueueSize = sqLiteReadJob.ReaderQueue.QueueSize;
                        if (intQueueSize > 0)
                        {
                            Console.WriteLine("****Read queue size [" + intQueueSize + "][" +
                                                  sqLiteReadJob.ReaderQueue.Id + "]");
                        }
                        m_lastReadLogTime = DateTime.Now;
                    }

                    while (cacheWrapper == null || 
                        disposeLock == null ||
                           cacheWrapper.IsDisposed)
                    {
                        if (disposeLock != null)
                        {
                            disposeLock.ReleaseReaderLock();
                        }
                        //
                        // cache alread disposed, get a new connection from connection pool
                        //
                        cacheWrapper = sqLiteReadJob.SqLiteCacheBase.GetCacheWrapperBase();
                        disposeLock = cacheWrapper.DisposeLock;
                        if (disposeLock != null)
                        {
                            disposeLock.AcquireReaderLock(SqliteConstants.DISPOSE_TIME_OUT_MILLSECS);
                        }
                    }


                    SQLiteConnection dbConn = cacheWrapper.DbConn;

                    if (dbConn == null)
                    {
                        throw new HCException("Null connection");
                    }
                    var objs = new Dictionary<object, object>();
                    using (SQLiteCommand cmd = dbConn.CreateCommand())
                    {
                        cmd.CommandText = strSql;
                        cmd.CommandTimeout = SqliteConstants.TIME_OUT;
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            int intFields = reader.FieldCount;
                            while (reader.Read())
                            {
                                var currRow = new object[intFields];
                                string[] colNames = null;

                                if (sqLiteReadJob.LoadColNames)
                                {
                                    colNames = new string[intFields];
                                }

                                for (int i = 0; i < intFields; i++)
                                {
                                    object obj = reader.GetValue(i);

                                    currRow[i] = obj;

                                    if(sqLiteReadJob.LoadColNames)
                                    {
                                        if (colNames != null)
                                        {
                                            colNames[i] = reader.GetName(i);
                                        }
                                    }
                                }
                                if (sqLiteReadJob.LoadColNames)
                                {
                                    sqLiteReadJob.LoadColNames = false;
                                    sqLiteReadJob.ColNames = colNames;
                                }

                                sqLiteReadJob.Data.Add(currRow);
                            }
                            reader.Close();
                            reader.Dispose();
                        }
                        cmd.Dispose();
                    }
                    objs.Clear();
                }
                finally
                {
                    ReleaseReaderLock(disposeLock);
                    disposeLock = null;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(
                    new HCException("Exception in db[ " + sqLiteReadJob.FileName + "]"));
                if (!string.IsNullOrEmpty(sqLiteReadJob.Query))
                {
                    Logger.Log(new HCException(sqLiteReadJob.Query));
                }
                Logger.Log(ex);
            }
            finally
            {
                ReleaseReaderLock(disposeLock);
            }
        }

        #endregion
    }
}