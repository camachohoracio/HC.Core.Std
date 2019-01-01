#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using System.Xml.Serialization;
using HC.Core.Comunication;
using HC.Core.ConfigClasses;
using HC.Core.DataStructures;
using HC.Core.Events;
using HC.Core.Exceptions;
using HC.Core.Helpers;
using HC.Core.Io.Serialization.Interfaces;
using HC.Core.Io.Serialization.Readers;
using HC.Core.Io.Serialization.Writers;
using HC.Core.Logging;
using HC.Core.Reflection;
using HC.Core.Threading;
using LZ4;

#endregion

namespace HC.Core.Io.Serialization
{
    public static class Serializer
    {
        public const int BYTES_LIMIT = (int) (500*1024f*1024f);
        public static readonly double m_dblZipLimit = 0.1 * 1024f * 1024f;
        private static readonly ConcurrentHashSet<string> m_directoriesCheecked =
            new ConcurrentHashSet<string>();

        //private static readonly object m_readLock = new object();

        public static List<byte[]> GetByteArrList(byte[] bytes)
        {
            int intBytesLength = bytes.Length;
            int intIndex = 0;
            var byteList = new List<byte[]>(2 + (intBytesLength / BYTES_LIMIT));
            while(intIndex < intBytesLength)
            {
                int intArraySize = Math.Min(
                    BYTES_LIMIT, 
                    intBytesLength - intIndex);
                var currArr = new byte[intArraySize];
                Array.Copy(bytes, intIndex, currArr, 0, intArraySize);
                byteList.Add(currArr);
                intIndex += intArraySize;
            }
            return byteList;
        }

        public static void SerializeFastLocal(
            Object obj,
            string strFileName)
        {
            SerializeFastLocal(
                obj,
                strFileName,
                false);
        }

        public static void SerializeFastLocal(
            Object obj,
            string strFileName,
            bool blnAreBytes)
        {
            try
            {
                if (NetworkHelper.IsADistWorkerConnected)
                {
                    Logger.Log(new HCException("Service should not access disk " +
                        Environment.StackTrace));
                }
                strFileName = strFileName.Replace(
                    "AUX.",
                    "AUX_.");

                byte[] bytes;
                if (!blnAreBytes)
                {
                    bytes = GetBytesFast(obj); // get bytes outside of lock, to avoid too much time in locking
                }
                else
                {
                    bytes = (byte[])obj;
                }

                lock (GetLockObject(strFileName)) // avoid hard disk starvation
                {
                    string strDirName = Path.GetDirectoryName(strFileName);
                    if (!m_directoriesCheecked.Contains(strDirName))
                    {
                        if(!DirectoryHelper.Exists(strDirName))
                        {
                            DirectoryHelper.CreateDirectory(strDirName);
                        }
                        m_directoriesCheecked.Add(strDirName);
                    }


                    //var fi = new FileInfo(strFileName);
                    //string strDir = Path.GetDirectoryName(strFileName);
                    //if (string.IsNullOrEmpty(strDir))
                    //{
                    //    throw new HCException("Empty dir");
                    //}
                    string strTmpFileName = strFileName + "_tmp";
                    if (FileHelper.Exists(strTmpFileName))
                    {
                        FileHelper.Delete(strTmpFileName);
                    }
                    if (FileHelper.Exists(strFileName))
                    {
                        FileHelper.Delete(strFileName);
                    }
                    using (var f = new FileStream(strTmpFileName,
                                                  FileMode.Create,
                                                  FileAccess.Write,
                                                  FileShare.Write,
                                                  10*1024*1024))
                    {
                        f.Write(bytes, 0, bytes.Length);
                    }
                    File.Move(strTmpFileName, strFileName);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                Logger.Log(new HCException("Failed to serialize file[" + 
                    strFileName + "]"));
            }
        }

        public static byte[] GetBytesFast(object obj)
        {
            try
            {
                Object obj2 = obj;
                if(obj2==null)
                {
                    return null;
                }
                IDynamicSerializable serializer = SerializerCache.GetSerializer(obj.GetType());
                var writer = GetWriter();
                serializer.Serialize(obj, writer);
                var bytes = writer.GetBytes();
                return bytes;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public static T DeserializeFast<T>(
            string strCacheFileName, 
            bool blnUseService,
            bool blnUseCompression = true)
        {
            try
            {
                object obj;
                if (blnUseService)
                {
                    obj = ProviderEvents.InvokeOnRunMethodDistributedViaService(
                        typeof (Serializer),
                        "DeserializeFastLocal",
                        new List<object>
                            {
                                strCacheFileName,
                                typeof (T),
                                true,
                                blnUseCompression
                            });
                }
                else
                {
                    obj = DeserializeFastLocal(
                        strCacheFileName,
                        typeof (T),
                        false, // do not return bytes
                        false); //  use compression only when using service, otherwise no point compressing
                }
                if (obj == null)
                {
                    return default(T);
                }
                var byteArr = obj as byte[];

                if (byteArr != null)
                {
                    if (blnUseCompression)
                    {
                        ISerializerReader reader = GetReader(byteArr);
                        int intBytesSize = reader.ReadInt32();
                        byteArr = reader.ReadByteArray();
                        byteArr = LZ4Codec.Decode(
                            byteArr,
                            0,
                            byteArr.Length,
                            intBytesSize); //(byte[])MemoryZipper.UnZipMemory(new MemoryStream(bytes));
                    }

                    object result =
                        DeserializeFastFromBytesGeneric(
                            byteArr,
                            typeof (T));
                    return (T) result;
                }
                return (T) obj;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return default(T);
        }

        public static Object DeserializeFastLocal(
            string strFileName,
            Type type,
            bool blnReturnBytes,
            bool blnUseCompression)
        {
            try
            {
                if (NetworkHelper.IsADistWorkerConnected)
                {
                    LiveGuiPublisherEvent.PublishGrid(
                        "Admin",
                        "CloudWorkers",
                        "Serializer_" + HCConfig.ClientUniqueName,
                        Environment.StackTrace.GetHashCode().ToString(),
                        Environment.StackTrace,
                        0,
                        false);
                    Logger.Log(new HCException("Service should not access disk " +
                        Environment.StackTrace));
                }
                strFileName = strFileName.Replace(
                    "AUX.",
                    "AUX_.");

                byte[] bytes;
                lock(GetLockObject(strFileName)) // avoid hard disk starvation
                {
                    if (!FileHelper.Exists(strFileName))
                    {
                        return ReflectorCache.GetReflector(type).CreateInstance();
                    }
                    var fileInfo = new FileInfo(strFileName);
                    var buffLenght = fileInfo.Length;
                    bytes = new byte[buffLenght];
                    using (var f = new FileStream(
                        strFileName,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read,
                        10*1024*1024))
                    {
                        f.Read(bytes, 0, (int) buffLenght);
                    }
                }

                if(blnUseCompression)
                {
                    ISerializerWriter serializer = GetWriter();
                    var intBytesSize = bytes.Length;
                    bytes = LZ4Codec.Encode(
                        bytes,
                        0,
                        bytes.Length);  //MemoryZipper.ZipInMemory(requestBytes).GetBuffer();
                    serializer.Write(intBytesSize);
                    serializer.Write(bytes);
                    return serializer.GetBytes();
                }

                if (blnReturnBytes)
                {
                    return bytes;
                }

                // deserialize outside of lock, to void too much time in locking
                object result = 
                    DeserializeFastFromBytesGeneric(
                    bytes, 
                    type);
                return result;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public static object GetLockObject(string strFileName)
        {
            return LockObjectHelper.GetLockObject(FileHelper.GetDriveLetter(
                "serializer_" + strFileName));
        }

        public static T DeserializeFastFromBytes<T>(byte[] bytes)
        {
            return(T) DeserializeFastFromBytesGeneric(bytes, typeof (T));
        }

        public static object DeserializeFastFromBytesGeneric(byte[] bytes, Type type)
        {
            try
            {
                var reader = GetReader(bytes);
                var serializer = SerializerCache.GetSerializer(type);
                var result = serializer.Deserialize(reader);
                return result;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public static ISerializerReader GetReader(
            byte[] buffer)
        {
            try
            {
                return new SerializerReader(buffer);
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public static ISerializerWriter GetWriter()
        {
            try
            {
                return new SerializerWriter();
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public static string SerializeToXml(
            object objectToSerialize,
            Type[] extraTypes)
        {
            var typeObj = objectToSerialize.GetType();

            using (var sw = new StringWriter())
            {
                var serializer =
                    new XmlSerializer(typeObj,
                                      extraTypes);

                serializer.Serialize(
                    sw,
                    objectToSerialize);
                return sw.ToString();
            }
        }

        public static void SerializeToXml(
            string strFileName,
            object objectToSerialize,
            Type[] extraTypes)
        {

            SerializeFast(objectToSerialize, strFileName, false);



            var typeObj = objectToSerialize.GetType();
            var strTmpFileName = strFileName + "_tmp";
            ClonerHelper.Clone(objectToSerialize);
            using (TextWriter sw = new StreamWriter(
                strTmpFileName))
            {
                var serializer =
                    new XmlSerializer(typeObj,
                                      extraTypes);

                serializer.Serialize(
                    sw,
                    objectToSerialize);
            }
            if (FileHelper.Exists(strFileName))
            {
                FileHelper.Delete(strFileName);
            }
            File.Move(strTmpFileName, strFileName);
        }

        public static T DeserializeXmlStr<T>(
            string strXml,
            Type[] extraTypes)
        {
            T obj;
            using (var sr = new StringReader(
                strXml))
            {
                var serializer =
                    new XmlSerializer(
                        typeof(T),
                        extraTypes);

                obj = (T)serializer.Deserialize(
                    sr);
            }
            return obj;
        }

        public static T DeserializeXml<T>(
            string strFileName,
            Type[] extraTypes)
        {
            T obj;
            using (TextReader sr = new StreamReader(
                strFileName))
            {
                var serializer =
                    new XmlSerializer(
                        typeof(T),
                        extraTypes);

                obj = (T)serializer.Deserialize(
                    sr);
            }
            return obj;
        }

        public static T DeserializeXml<T>(
            XmlReader xmlReader,
            Type[] extraTypes)
        {
            var serializer =
                new XmlSerializer(
                    typeof(T),
                    extraTypes);

            var obj = (T)serializer.Deserialize(
                xmlReader);
            return obj;
        }

        public static void Serialize(
            string strFileName,
            object objectToSerialize)
        {
            string strTmpFileName = strFileName + "tmp";
            var fi = new FileInfo(strTmpFileName);
            string strDir = fi.DirectoryName;
            if (String.IsNullOrEmpty(strDir))
            {
                throw new HCException("Empty dir");
            }
            if (!DirectoryHelper.Exists(strDir))
            {
                DirectoryHelper.CreateDirectory(strDir);
            }

            if (FileHelper.Exists(strTmpFileName))
            {
                FileHelper.Delete(strTmpFileName);
            }
            if (FileHelper.Exists(strFileName))
            {
                FileHelper.Delete(strFileName);
            }

            PrintToScreen.WriteLine("Serializing object: " +
                                    strTmpFileName + ". Please wait...");
            Stream str = File.OpenWrite(strTmpFileName);
            var formatter = new BinaryFormatter();
            formatter.Serialize(str, objectToSerialize);
            str.Close();
            PrintToScreen.WriteLine("Finished serializing object.");
            if (FileHelper.Exists(strTmpFileName))
            {
                File.Move(strTmpFileName, strFileName);
            }
        }

        public static byte[] SerializeSlow(
            object objectToSerialize)
        {
            try
            {
                if (objectToSerialize == null)
                {
                    return new byte[0];
                }
                var bf = new BinaryFormatter();
                var fs = new MemoryStream();
                bf.Serialize(fs, objectToSerialize);
                return fs.GetBuffer();
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return new byte[0];
        }

        public static T DeserializeSlow<T>(byte[] bytes)
        {
            try
            {
                if (bytes == null)
                {
                    return default(T);
                }
                Stream str = new MemoryStream(bytes);
                var formatter = new BinaryFormatter();
                var deserializedObject = (T) formatter.Deserialize(str);
                str.Close();
                return deserializedObject;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return default(T);
        }


        public static T DeserializeFile<T>(string strFileName)
        {
            var strMessage = "Deserializing file: " +
                             strFileName + ". Please wait...";
            Logger.Log(strMessage);
            Stream str = File.OpenRead(strFileName);
            var formatter = new BinaryFormatter();
            var deserializedObject = (T)formatter.Deserialize(str);
            str.Close();

            strMessage = "Finish deserializing file: " +
                         strFileName + ". Please wait...";
            Logger.Log(strMessage);
            return deserializedObject;
        }

        public static byte[] MergeBytes(List<byte[]> byteArrList)
        {
            var byteList = new List<byte>(byteArrList.Count * BYTES_LIMIT + 2);
            for (int i = 0; i < byteArrList.Count; i++)
            {
                byteList.AddRange(byteArrList[i]);
            }
            return byteList.ToArray();
        }

        public static void SerializeFast(
            object obj, 
            string strCacheFileName, 
            bool blnUseService)
        {
            try
            {
                if (blnUseService)
                {
                    var bytes = GetBytesFast(obj);

                    ProviderEvents.InvokeOnRunMethodDistributedViaService(
                        typeof (Serializer),
                        "SerializeFastLocal",
                        new List<object>
                            {
                                bytes,
                                strCacheFileName,
                                true
                            });
                }
                else
                {
                    SerializeFastLocal(
                        obj,
                        strCacheFileName,
                        false);
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }
    }
}


