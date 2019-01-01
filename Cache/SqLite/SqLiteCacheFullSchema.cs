#region

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using HC.Core.Exceptions;
using HC.Core.Io.Serialization;
using HC.Core.Io.Serialization.Readers;
using HC.Core.Logging;
using HC.Core.Threading.ProducerConsumerQueues.Support;
using HC.Core.Zip;

#endregion

namespace HC.Core.Cache.SqLite
{
    public class SqliteCacheFullSchema<T> : ASqliteCache<T>
    {
        #region Constructors
        
        private readonly Verboser m_verboser = new Verboser();

        public SqliteCacheFullSchema(
            string strFileName)
            : this(strFileName, typeof(T).Name, null, SqliteConstants.KEY_COL_NAME) { }

        public SqliteCacheFullSchema(
            string strFileName,
            string strTableName) : this(strFileName,strTableName,null,SqliteConstants.KEY_COL_NAME){}

        public SqliteCacheFullSchema(
            string strFileName,
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

        public override object[][] GetImportArrBLob(List<KeyValuePair<string, object>> importList,
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

        public override List<TaskWrapper> Insert(Dictionary<string, T> objs)
        {
            var map = objs.ToDictionary(t => t.Key, t => new List<T>(new[] {t.Value}));
            return Insert(map);
        }

        public override List<TaskWrapper> Insert(Dictionary<string, List<T>> objs)
        {
            try
            {
                if(objs.Count == 0)
                {
                    return new List<TaskWrapper>();
                }

                var keyValuePairs = new List<KeyValuePair<string, object>>();
                foreach (KeyValuePair<string, List<T>> keyValuePair in objs)
                {
                    foreach (T tObj in keyValuePair.Value)
                    {
                        keyValuePairs.Add(new KeyValuePair<string, object>(
                            keyValuePair.Key.Replace("'", string.Empty),
                            tObj));
                    }
                    keyValuePair.Value.Clear();
                }

                var tasks = new List<TaskWrapper>
                                {
                                    SqLiteAdoWrapper<T>.BulkInsert(
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
                var intObjCounter = objs.Count;
                strKey = strKey.Replace("'", string.Empty);
                var keyValuePairs = new List<KeyValuePair<string, object>>();
                for (int i = 0; i < objs.Count; i++)
                {
                    keyValuePairs.Add(
                        new KeyValuePair<string, object>(
                        strKey,objs[i]));
                }
                TaskWrapper task = SqLiteAdoWrapper<T>.BulkInsert(
                    m_strTableName,
                    keyValuePairs,
                    FileName,
                    this,
                    UseCompression);
                string strMessage = "Inserted [" + intObjCounter + "] rows. File: " +
                                    new FileInfo(FileName).Name + " Time [" +
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
                var sb = new StringBuilder();
                var blnIsTitleCol = true;

                //
                // add default index as a column
                //
                if (!string.IsNullOrEmpty(m_strDefaultIndex))
                {
                    blnIsTitleCol = false;
                    sb.Append(m_strDefaultIndex + " varchar(100)");
                }


                for (int i = 0; i < m_readWritePropertyNames.Count; i++)
                {
                    string strPropertyName = m_readWritePropertyNames[i];
                    Type propertyType = 
                        m_reflector.GetPropertyType(strPropertyName);
                    
                    if (!blnIsTitleCol)
                    {
                        sb.Append(",");
                    }

                    if (propertyType == typeof(string))
                    {
                        sb.Append(strPropertyName + " varchar(100)");
                    }
                    else if (propertyType == typeof(int))
                    {
                        sb.Append(strPropertyName + " int");
                    }
                    else if (propertyType == typeof(long))
                    {
                        sb.Append(strPropertyName + " DOUBLE");
                    }
                    else if (propertyType == typeof(double))
                    {
                        sb.Append(strPropertyName + " DOUBLE");
                    }
                    else if (propertyType == typeof(bool))
                    {
                        sb.Append(strPropertyName + " BOOLEAN");
                    }
                    else if (propertyType == typeof(DateTime))
                    {
                        sb.Append(strPropertyName + " REAL");
                    }
                    else if (propertyType.IsEnum)
                    {
                        sb.Append(strPropertyName + " varchar(100)");
                    }
                    else
                    {
                        sb.Append(strPropertyName + " BLOB");
                    }
                    blnIsTitleCol = false;
                }
                string strColumnDef = sb.ToString();
                string strQuery = "create table if not exists [" +
                                     m_strTableName + "] (" +
                                     strColumnDef + ")";
                
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
            }
        }

        public override List<T> LoadDataFromQuery(string strQuery)
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
                    false);

                //
                // build objects using reflection
                //
                var dataArray = new List<T>(data.Count);
                //
                // not worth paraell.For
                //
                for (int i = 0; i < data.Count; i++)
                {
                    T tobj = GetRow(data, i);
                    dataArray.Add(tobj);
                }
                return dataArray;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            finally
            {
                if(data != null)
                {
                    data.Clear();
                }
            }
            return new List<T>();
        }

        protected override T GetRow(
            List<object[]> data, 
            int i)
        {
            try
            {
                object[] objects = data[i];
                data[i] = null;
                var tobj = (T) Activator.CreateInstance(typeof (T));
                for (var j = 0; j < m_readWritePropertyNames.Count; j++)
                {
                    object obj = objects[j+1]; // the zero item is the key
                    obj = ParseObject(obj, j);
                    objects[j + 1] = null;
                    if (obj != null && !obj.Equals(DBNull.Value))
                    {
                        m_reflector.SetReadWritePropertyValue(
                            tobj,
                            j,
                            obj);
                    }
                }
                objects[0] = null;
                return tobj;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return default(T);
        }

        private object ParseObject(
            object obj, 
            int intCol)
        {
            try
            {
                if (obj is DBNull)
                {
                    obj = string.Empty;
                }

                Type propertyType = m_reflector.GetReadWritePropertyType(intCol);
                if (propertyType.IsEnum)
                {
                    obj = Enum.Parse(propertyType, (string) obj, true);
                }
                if (propertyType == typeof (long) ||
                    propertyType == typeof (Int64))
                {
                    obj = Convert.ToInt64(obj);
                }
                else if (propertyType == typeof (DateTime))
                {
                    if (obj is double)
                    {
                        obj = DateTime.FromOADate((double)obj);
                    }
                    else
                    {
                        obj = new DateTime();
                    }
                }
                else if (propertyType == typeof (double) &&
                         obj.GetType() != typeof (double))
                {
                    if (obj is string)
                    {
                        var strObj = obj.ToString();
                        if (string.IsNullOrEmpty(strObj))
                        {
                            obj = double.NaN;
                        }
                        else
                        {
                            obj = Convert.ToDouble(obj);
                        }
                    }
                }
                else if (obj is byte[])
                {
                    //
                    // by default. This is a blob value
                    //
                    var bytes = (byte[])obj;
                    if (bytes.Length > 0)
                    {
                        if (UseCompression)
                        {
                            bytes = (byte[]) MemoryZipper.UnZipMemory(new MemoryStream(bytes));
                        }
                        obj = SerializerCache.GetSerializer(typeof (object)).Deserialize(
                            new SerializerReader(bytes));
                    }
                    else
                    {
                        obj = null;
                    }
                }
                else if(propertyType != typeof(string) && obj is string)
                {
                    var strObj = obj.ToString();
                    if (string.IsNullOrEmpty(strObj))
                    {
                        obj = null;
                    }
                }
                return obj;
            }
            catch(Exception ex)
            {
                Logger.Log(new HCException("Sqlite error with file [" +
                    FileName + "]"));
                Logger.Log(ex);
            }
            return null;
        }

        #endregion

        public string getKeyColName()
        {
            return m_strDefaultIndex;
        }
    }
}



