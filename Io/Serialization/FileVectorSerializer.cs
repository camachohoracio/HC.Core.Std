#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HC.Core.DynamicCompilation;
using HC.Core.Io.Serialization.Interfaces;
using HC.Core.Logging;
using HC.Core.Reflection;

#endregion

namespace HC.Core.Io.Serialization
{
    public static class FileVectorSerializer
    {
        public static void Serialize(
            string strFileName,
            IList objList)
        {
            try
            {
                var fi = new FileInfo(strFileName);
                if(!DirectoryHelper.Exists(fi.DirectoryName))
                {
                    DirectoryHelper.CreateDirectory(fi.DirectoryName);
                }
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
                    foreach (object currObj in objList)
                    {
                        var serializationWriter = Serializer.GetWriter();
                        Type objType = currObj.GetType();
                        if (objType.Name.Equals("RuntimeType"))
                        {
                            objType = typeof (SelfDescribingClass);
                        }
                        serializationWriter.Write(objType);
                        serializationWriter.Write(((ISerializable) currObj).GetByteArr());
                        var bytes = serializationWriter.GetBytes();
                        f.Write(bytes, 0, bytes.Length);
                    }
                }
                File.Move(strTmpFileName, strFileName);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public static List<T> Deserialize<T>(
            string strFileName)
        {
            try
            {
                var fi = new FileInfo(strFileName);
                var buffLenght = fi.Length;
                var bytes = new byte[buffLenght];
                using (var f = new FileStream(
                    strFileName,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    10 * 1024 * 1024))
                {
                    f.Read(bytes, 0, (int)buffLenght);
                }
                var serializationReader = Serializer.GetReader(bytes);
                var resultList = new List<T>();
                while (serializationReader.Position < buffLenght)
                {
                    var objType = serializationReader.ReadType();
                    var instance = (ISerializable) ReflectorCache.GetReflector(objType).CreateInstance();
                    var obj = instance.Deserialize(serializationReader.ReadByteArray());
                    resultList.Add((T)obj);
                }
                return resultList;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }
    }
}



