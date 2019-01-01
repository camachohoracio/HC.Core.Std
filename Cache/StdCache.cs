#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using HC.Core.Helpers;
using HC.Core.Io;
using HC.Core.Io.Serialization;
using HC.Core.Resources;
using HC.Core.Threading;
using HC.Core.Zip;

#endregion

namespace HC.Core.Cache
{
    public class StdCache : ICache
    {
        #region Properties

        public bool CompressItems { get; set; }
        public IDataRequest DataRequest { get; set; }
        public object Owner { get; set; }
        public DateTime TimeUsed { get; set; }
        public bool HasChanged { get; set; }

        public int Count
        {
            get { return -1; }
        }

        #endregion

        #region Members

        private readonly string m_strDbPath;

        #endregion

        #region Constructors

        /// <summary>
        ///   Default constructor
        /// </summary>
        public StdCache() :
            this(Config.GetDefaultCacheDataPath(),
                 false)
        {
        }

        public StdCache(
            string strDbPath,
            bool blnCompressItems)
        {
            var dbDataRequest =
                new DbDataRequest(strDbPath);
            DataRequest = dbDataRequest;
            m_strDbPath = strDbPath;
            CompressItems = blnCompressItems;
            if (!DirectoryHelper.Exists(
                strDbPath,
                false))
            {
                DirectoryHelper.CreateDirectory(strDbPath);
            }
            InitializeCache();
        }

        #endregion

        #region Public

        public IEnumerator<KeyValuePair<object, object>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            Dispose();
        }

        public void Delete(object oOKey)
        {
            Delete(oOKey as string);
        }

        public void Add(
            object oKey,
            object oValue)
        {
            Insert(
                oKey as string,
                oValue);
        }

        public bool ContainsKey(object oKey)
        {
            return Contains(
                oKey as string);
        }

        public void Update(
            object oKey,
            object oValue)
        {
            Update(
                oKey as string,
                oValue);
        }

        public void Clear()
        {
            //
            // remove all elements in the cache
            //
            DirectoryHelper.Delete(
                m_strDbPath,
                false);
        }

        public object Get(object oKey)
        {
            return GetItem(oKey as string);
        }

        public void Insert(
            string strKey,
            object oValue)
        {
            try
            {
                var strFileName = GetFileName(strKey);
                //
                // save first into a temp location
                // this will avoid partial serialization
                //
                var strTempFileName =
                    strFileName + "_tmp";

                var serializerLockObject =
                    LockObjectHelper.GetLockObject(strFileName);

                if (CompressItems)
                {
                    lock (serializerLockObject)
                    {
                        Serializer.Serialize(strTempFileName, MemoryZipper.ZipInMemory(oValue));
                        File.Move(strTempFileName, strFileName);
                    }
                }
                else
                {
                    var str = new MemoryStream();
                    var formatter = new BinaryFormatter();
                    formatter.Serialize(str, oValue);
                    Byte[] bytes;
                    bytes = str.GetBuffer();
                    str.Close();
                    lock (serializerLockObject)
                    {
                        Stream stream = File.OpenWrite(strTempFileName);
                        var binaryFormatter = new BinaryFormatter();
                        binaryFormatter.Serialize(stream, bytes);
                        stream.Close();
                        File.Move(strTempFileName, strFileName);
                    }
                }
            }
            catch (Exception e)
            {
                PrintToScreen.WriteLine(Thread.CurrentThread.Name +
                                        " : caucht exception: " + e);
                PrintToScreen.WriteLine(e.StackTrace);
            }
        }

        public void Update(
            string strKey,
            object oValue)
        {
            if (Contains(strKey))
            {
                Delete(strKey);
            }
            Insert(strKey, oValue);
        }

        public bool Contains(string strKey)
        {
            var strFileName = GetFileName(strKey);

            var serializerLockObject = LockObjectHelper.GetLockObject(strFileName);

            bool blFileExists;
            lock (serializerLockObject)
            {
                blFileExists = FileHelper.Exists(strFileName);
            }

            return blFileExists;
        }

        public void Delete(string strKey)
        {
            if (Contains(strKey))
            {
                var strFileName = GetFileName(strKey);
                FileHelper.Delete(strFileName);
            }
            PrintToScreen.WriteLine("Finish deleting item from local cache");
        }

        public object GetItem(string strKey)
        {
            Stream str = null;
            try
            {
                var strFileName = GetFileName(strKey);
                //
                // make process thread-safe
                //
                var serializerLockObject = LockObjectHelper.GetLockObject(strFileName);
                object obj;
                lock (serializerLockObject)
                {
                    byte[] bytes;
                    if (!CompressItems)
                    {
                        str = File.OpenRead(strFileName);
                        var formatter = new BinaryFormatter();
                        bytes = (byte[]) formatter.Deserialize(str);
                    }
                    else
                    {
                        str = Serializer.DeserializeFile<Stream>(strFileName);
                        return (MemoryZipper.UnZipMemory(str));
                    }
                    //
                    // convert bytes into object
                    //
                    BinaryFormatter formatter2 = new BinaryFormatter();
                    var stream = new MemoryStream(bytes);
                    obj = formatter2.Deserialize(stream);
                    str.Close();
                }
                return obj;
            }
            catch (Exception e)
            {
                PrintToScreen.WriteLine(e.Message);
                PrintToScreen.WriteLine(e.StackTrace);
                throw;
            }
            finally
            {
                if (str != null)
                {
                    str.Close();
                }
            }
        }

        //public static LockObjectHelper GetLockObject(string strFileName)
        //{
        //    var serializerLockObject =
        //        new LockObjectHelper
        //            {
        //                Name = strFileName
        //            };
        //    lock (m_lockObjects)
        //    {
        //        if (!m_lockObjects.ContainsKey(serializerLockObject))
        //        {
        //            m_lockObjects.Add(
        //                serializerLockObject,
        //                serializerLockObject);
        //        }
        //        else
        //        {
        //            serializerLockObject = m_lockObjects[serializerLockObject];
        //        }
        //    }
        //    return serializerLockObject;
        //}

        #endregion

        #region Private

        private void InitializeCache()
        {
            Init();
        }

        private void Init()
        {
            try
            {
                Open();
            }
            catch (Exception e)
            {
                PrintToScreen.WriteLine("Caught exception:  {0}", e.Message);
                PrintToScreen.WriteLine(e.StackTrace);
            }
        }

        private void Open()
        {
        }

        private string GetFileName(string strKey)
        {
            strKey = strKey.Replace(" ", "")
                .Replace(@"/", "_")
                .Replace(@"\", "_")
                .Replace(@"|", "%");

            return Path.Combine(m_strDbPath, strKey);
        }

        #endregion

        #region Disposable Methods

        public void Dispose()
        {
            EventHandlerHelper.RemoveAllEventHandlers(this);
        }

        ~StdCache()
        {
            Dispose();
        }

        #endregion
    }
}



