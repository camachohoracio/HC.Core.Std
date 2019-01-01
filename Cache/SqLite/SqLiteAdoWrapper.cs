#region

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HC.Core.Exceptions;
using HC.Core.Io;
using HC.Core.Io.Serialization;
using HC.Core.Io.Serialization.Interfaces;
using HC.Core.Io.Serialization.Writers;
using HC.Core.Logging;
using HC.Core.Reflection;
using HC.Core.Threading;
using HC.Core.Threading.ProducerConsumerQueues.Support;
using HC.Core.Zip;

#endregion

namespace HC.Core.Cache.SqLite
{
    public class SqLiteAdoWrapper<T> : ISqLiteCacheBase
    {
        #region Properties

        public bool UseCompression
        {
            get { throw new HCException("Invalid call"); }
            set { throw new HCException("Invalid call"); }
        }

        public string FileName { get; private set; }
        public SQLiteConnection DbConn { get; private set; }
        public bool IsDisposed { get; private set; }
        public ReaderWriterLock DisposeLock { get; private set; }

        #endregion

        #region Members

        public KeyValuePair<string, DbType>[] ColNameToTypeArr { get; set; }
        private object m_prpertyMapLockObj = new object();
        private IReflector m_reflector;
        private readonly static object m_connectionCounterLock = new object();
        private static int m_intConnectionCounter;

        #endregion

        #region Public

        public SqLiteAdoWrapper(string strFileName)
        {
            try
            {
                DisposeLock = new ReaderWriterLock();
                int intMalFormed = 0;
                string strBaseName = strFileName;

                while (!OpenDb(strFileName))
                {
                    strFileName = strBaseName + 
                        "_malformedHoracioHere_" + 
                        intMalFormed++;

                    if (intMalFormed > 10)
                    {
                        // too many trials
                        break;
                    }
                    Thread.Sleep(5000);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public void TryRepairDb()
        {
            using (SQLiteCommand cmd = DbConn.CreateCommand())
            {
                cmd.CommandTimeout = SqliteConstants.TIME_OUT;
                cmd.CommandText = "mode insert";
                cmd.ExecuteNonQuery();
            }

            using (SQLiteCommand cmd = DbConn.CreateCommand())
            {
                cmd.CommandTimeout = SqliteConstants.TIME_OUT;
                cmd.CommandText = "output mydb_export.sql";
                cmd.ExecuteNonQuery();
            }
            using (SQLiteCommand cmd = DbConn.CreateCommand())
            {
                cmd.CommandTimeout = SqliteConstants.TIME_OUT;
                cmd.CommandText = "dump";
                cmd.ExecuteNonQuery();
            }
            using (SQLiteCommand cmd = DbConn.CreateCommand())
            {
                cmd.CommandTimeout = SqliteConstants.TIME_OUT;
                cmd.CommandText = "exit";
                cmd.ExecuteNonQuery();
            }
           
            
        }

        private bool OpenDb(string strFileName)
        {
            try
            {
                lock (LockObjectHelper.GetLockObject(strFileName + "lock"))
                {
                    if (strFileName.StartsWith(@"\\"))
                    {
                        strFileName = @"\\" + strFileName;
                    }
                    strFileName = FileHelper.CleanFileName(strFileName);
                    FileName = strFileName;
                    m_reflector = ReflectorCache.GetReflector(typeof (T));
                    string strConnectionString = @"data source=" +
                                                 strFileName + ";datetimeformat=Ticks";
                    DbConn = new SQLiteConnection(strConnectionString);
                    DbConn.Open();

                    using (SQLiteCommand cmd = DbConn.CreateCommand())
                    {
                        cmd.CommandTimeout = SqliteConstants.TIME_OUT;
                        cmd.CommandText = "PRAGMA page_size = SQLITE_MAX_PAGE_SIZE";
                        cmd.ExecuteNonQuery();
                    }
                    try
                    {
                        using (SQLiteCommand cmd = DbConn.CreateCommand())
                        {
                            cmd.CommandTimeout = SqliteConstants.TIME_OUT;
                            cmd.CommandText = "PRAGMA cache_size = 1000000";
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                    }
                    try
                    { 
                    using (SQLiteCommand cmd = DbConn.CreateCommand())
                    {
                        cmd.CommandTimeout = SqliteConstants.TIME_OUT;
                        cmd.CommandText = " PRAGMA synchronous = OFF";
                        cmd.ExecuteNonQuery();
                    }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                    }
                    //using (SQLiteCommand cmd = DbConn.CreateCommand())
                    //{
                    //    cmd.CommandTimeout = SqliteConstants.TIME_OUT;
                    //    cmd.CommandText = " PRAGMA temp_store = MEMORY";
                    //    cmd.ExecuteNonQuery();
                    //}
                    try
                    { 
                
                    using (SQLiteCommand cmd = DbConn.CreateCommand())
                    {
                        cmd.CommandTimeout = SqliteConstants.TIME_OUT;
                        cmd.CommandText = " PRAGMA journal_mode = OFF";
                        cmd.ExecuteNonQuery();
                    }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                    }

                    //Execute("PRAGMA synchronous=OFF");

                    //Execute("PRAGMA count_changes=OFF");
                    //Execute("PRAGMA journal_mode=MEMORY");
                    //Execute("PRAGMA temp_store=MEMORY");
                    lock (m_connectionCounterLock)
                    {
                        m_intConnectionCounter++;
                    }
                    //string strMessage = "|||------------Total sqlite connections [" + m_intConnectionCounter + "]";
                    //Console.WriteLine(strMessage);
                    //Logger.Log(strMessage);
                    //Logger.Log("Loaded connection. File = " + strFileName);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                Logger.Log(new HCException(
                    "Exception on database: " +
                    strFileName));
                return false;
            }
            return true;
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            try
            {
                ReaderWriterLock disposeLock = DisposeLock;
                if (disposeLock != null)
                {
                    IsDisposed = true;
                    disposeLock.AcquireWriterLock(
                        SqliteConstants.DISPOSE_TIME_OUT_MILLSECS);
                    DisposeLock = null;
                }
                else if (IsDisposed)
                {
                    return;
                }

                try
                {
                    if (IsDisposed)
                    {
                        return;
                    }
                    IsDisposed = true;
                    DisposeLock = null;
                    //string strMessage = "Disposing " +
                    //                    GetType().Name + " [" +
                    //                    strFileName + "]";
                    //Logger.Log(strMessage);
                    //Console.WriteLine(strMessage);

                    EventHandlerHelper.RemoveAllEventHandlers(this);
                    if (DbConn != null)
                    {
                        DbConn.Close();
                        DbConn.Dispose();
                        DbConn = null;
                    }
                    ColNameToTypeArr = null;
                    m_prpertyMapLockObj = null;
                    m_reflector = null;
                    FileName = null;
                    lock (m_connectionCounterLock)
                    {
                        m_intConnectionCounter--;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
                finally
                {
                    ReleaseWriteLock(disposeLock);
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            finally
            {
                DisposeLock = null;
                IsDisposed = true;
            }
        }

        private static void ReleaseWriteLock(
            ReaderWriterLock disposeLock)
        {
            try
            {
                if (disposeLock != null)
                {
                    disposeLock.ReleaseWriterLock();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }

        public static TaskWrapper BulkInsert(
            string strTableName,
            List<KeyValuePair<string, object>> keyValuePairs,
            string strFileName,
            ISqLiteCacheBase parentConnection,
            bool blnUseCompression)
        {
            try
            {
                return SqLiteTaskQueues.EnqueueImport(
                    strTableName, 
                    keyValuePairs,
                    false,
                    strFileName,
                    parentConnection,
                    blnUseCompression);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public object[][] GetImportArr(
            List<KeyValuePair<string, object>> keyValuePairs,
            bool blnUseCompression)
        {
            try
            {
                var importArr = new object[keyValuePairs.Count()][];
                for (int i = 0; i < keyValuePairs.Count; i++)
                {
                    importArr[i] = ImportRow(
                        keyValuePairs[i],
                        blnUseCompression);
                }
                return importArr;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return new object[0][];
        }

        private object[] ImportRow(
            KeyValuePair<string, object> keyValuePair,
            bool blnUseCompression)
        {
            try
            {
                var dataObj = keyValuePair.Value;
                var objects = new object[ColNameToTypeArr.Length + 1];
                objects[0] = keyValuePair.Key.Replace("'", string.Empty);

                for (int i = 0; i < ColNameToTypeArr.Length; i++)
                {
                    object objValue = m_reflector.GetReadWritePropertyValue(
                        dataObj, 
                        i);
                    
                    if (objValue == null)
                    {
                        Type type  = m_reflector.GetPropertyType(i);
                        //
                        // get default value
                        //
                        if (type == typeof(DateTime))
                        {
                            objValue = new DateTime().ToOADate();
                        }
                        else if (type.IsEnum)
                        {
                            objValue = string.Empty;
                        }
                        else if (type == typeof(string))
                        {
                            objValue = string.Empty;
                        }
                        else
                        {
                            objValue = new byte[0];
                        }
                    }
                    else if (objValue is DateTime)
                    {
                        var dateVal = (DateTime) objValue;
                        objValue = dateVal.ToOADate();
                    }
                    else if (objValue.GetType().IsEnum)
                    {
                        objValue = objValue.ToString();
                    }
                    else if (ColNameToTypeArr[i].Value == DbType.Binary)
                    {
                        ISerializerWriter writer = new SerializerWriter();
                        SerializerCache.GetSerializer(typeof (object)).Serialize(
                            objValue,
                            writer);
                        byte[] bytes = writer.GetBytes();
                        if (blnUseCompression)
                        {
                            bytes = MemoryZipper.ZipInMemory(bytes).GetBuffer();
                        }
                        objValue = bytes;
                    }
                    objects[i+1] = objValue;
                }
                return objects;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public void GetColNameToTypeArr(string strTableName)
        {
            if (ColNameToTypeArr == null)
            {
                if (m_prpertyMapLockObj == null)
                {
                    m_prpertyMapLockObj = new object();
                }
                lock (m_prpertyMapLockObj)
                {
                    if (ColNameToTypeArr == null)
                    {
                        //List<string> columnNames;
                        //List<Type> types;
                        //GetTableSchema(strTableName, out types, out columnNames);
                        List<string> readWritePropertyNames = m_reflector.GetReadWritePropertyNames();
                        List<Type> types = m_reflector.GetReadWritePropertyTypes(); 
                        ColNameToTypeArr = new KeyValuePair<string, DbType>[readWritePropertyNames.Count];
                        for ( int i = 0; i < readWritePropertyNames.Count; i++)
                        {
                            string strColName = readWritePropertyNames[i];
                            //if (columnNames.Contains(strColName))
                            {
                                //int intIndex = columnNames.IndexOf(strColName);
                                Type currType = types[i];
                                DbType dbType;
                                if (currType == typeof(string))
                                {
                                    dbType = DbType.String;
                                }
                                else if (currType == typeof(bool))
                                {
                                    dbType = DbType.Boolean;
                                }
                                else if (currType == typeof(double))
                                {
                                    dbType = DbType.Double;
                                }
                                else if (currType == typeof(int))
                                {
                                    dbType = DbType.Int32;
                                }
                                else if (currType == typeof(long))
                                {
                                    dbType = DbType.UInt64;
                                }
                                else if (currType == typeof(DateTime))
                                {
                                    dbType = DbType.Double;
                                }
                                else if (currType.IsEnum)
                                {
                                    dbType = DbType.String;
                                }
                                else
                                {
                                    dbType = DbType.Binary;
                                }
                                ColNameToTypeArr[i] = new KeyValuePair<string, DbType>(
                                    strColName,
                                    dbType);
                            }
                        }
                    }
                }
            }
        }

        public static TaskWrapper BulkInsertBLob(
            string strTableName,
            List<KeyValuePair<string, object>> data,
            string strFileName,
            ISqLiteCacheBase parentConnection,
            bool blnUseCompression)
        {
            try
            {
                return SqLiteTaskQueues.EnqueueImport(
                    strTableName,
                    data,
                    true,
                    strFileName,
                    parentConnection,
                    blnUseCompression);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public object[][] GetImportArrBLob(
            List<KeyValuePair<string, object>> data,
            bool blnUseCompression)
        {
            try
            {
                var importArr = new object[data.Count()][];
                for (int i = 0; i < data.Count(); i++)
                {
                    byte[] bytes = ((ISerializable) data[i].Value).GetByteArr();

                    if (blnUseCompression)
                    {
                        bytes = MemoryZipper.ZipInMemory(bytes).GetBuffer();
                    }

                    var objArr = new object[]
                                     {
                                         data[i].Key.Replace("'", string.Empty),
                                         bytes
                                     };
                    importArr[i] = objArr;
                }
                return importArr;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return new object[0][];
        }

        public static List<T> LoadBLob(
            string strSql,
            string strFileName,
            ISqLiteCacheBase parentConnection)
        {
            try
            {
                var data = new List<object[]>();
                SqLiteTaskQueues.EnqueueLoad(
                    strSql,
                    data,
                    strFileName,
                    parentConnection,
                    false);

                const int intIndeOfObj = 1;

                var results = new T[data.Count];
                var reflector = ReflectorCache.GetReflector(typeof (T));
                Parallel.For(
                    0, results.Length, delegate(int i)
                                           {
                                               try
                                               {
                                                   var instance =
                                                       (ISerializable)
                                                       reflector.CreateInstance();
                                                   var bytes = (byte[]) data[i][intIndeOfObj];
                                                   if (parentConnection.UseCompression)
                                                   {
                                                       bytes =
                                                           (byte[]) MemoryZipper.UnZipMemory(new MemoryStream(bytes));
                                                   }
                                                   var obj = (T) instance.Deserialize(bytes);
                                                   data[i][0] = null;
                                                   data[i][intIndeOfObj] = null;
                                                   results[i] = obj;
                                               }
                                               catch(Exception ex)
                                               {
                                                   Logger.Log(ex);
                                               }
                                           });

                return results.ToList();
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return new List<T>();
        }

        public ISqLiteCacheBase GetCacheWrapperBase()
        {
            throw new HCException("NotImplementedException");
        }

        #endregion

        #region Private

        //public bool ContainsKey(
        //    string strSql)
        //{
        //    return ExecuteScalar<long>(strSql) == 1L;
        //}

        //public K ExecuteScalar<K>(
        //    string strSql)
        //{
        //    try
        //    {
        //        using (var cmd = SqLiteCacheBase.CreateCommand())
        //        {
        //            cmd.CommandText = strSql;
        //            return (K) cmd.ExecuteScalar();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Log("Exception in db name: " + FileName);
        //        Logger.Log(ex);
        //    }
        //    return default(K);
        //}


        //public void Execute(string strSql)
        //{
        //    try
        //    {
        //        if (SqLiteCacheBase == null)
        //        {
        //            throw new HCException("Null connection");
        //        }
        //        using (var cmd = SqLiteCacheBase.CreateCommand())
        //        {
        //            cmd.CommandText = strSql;
        //            cmd.ExecuteNonQuery();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Log(ex);
        //    }
        //}

        #endregion
    }
}



