#region

using System;
using System.Collections.Generic;
using System.IO;
using HC.Core.Io;
using HC.Core.Io.Serialization;
using HC.Core.Logging;
using HC.Core.Reflection;

#endregion

namespace HC.Core.DynamicCompilation
{
    public static class SelfDescribingClassHelper
    {
        #region Members

        private static string m_strClassSchemaPath;

        #endregion

        #region Public

        public static SelfDescribingClassFactory GetSerializedClassFactory(
            string strClassName)
        {
            var strFileName = GetXmlFileName(strClassName);
            Logger.Log("Attemp to load dynamic class schema = " + strFileName);
            if (!FileHelper.Exists(
                strFileName,
                false))
            {
                // no file available
                return null;
            }
            return Serializer.DeserializeXml<SelfDescribingClassFactory>(
                strFileName,
                null);
        }

        public static void SaveSchema(
            string strClassName,
            SelfDescribingClassFactory selfDescribingClassFactory)
        {
            string strFileName = GetXmlFileName(
                strClassName);

            if (FileHelper.Exists(
                strFileName,
                false))
            {
                var fi = new FileInfo(strFileName);
                string strOldConfigsPath =
                    Path.Combine(
                        fi.DirectoryName,
                        "OldConfigs");
                if(!DirectoryHelper.Exists(
                    strOldConfigsPath,
                    false))
                {
                    DirectoryHelper.CreateDirectory(strOldConfigsPath);
                }
                string strOldConfigFileName = Path.Combine(
                    strOldConfigsPath,
                    strFileName + "_" +
                    Guid.NewGuid());
                //
                // rename file name
                //
                File.Move(
                    strFileName,
                    strOldConfigFileName);
            }
            Serializer.SerializeToXml(
                strFileName,
                selfDescribingClassFactory,
                null);
        }

        #endregion

        #region Private

        private static string GetXmlFileName(
            string strClassName)
        {
            if (string.IsNullOrEmpty(m_strClassSchemaPath))
            {
                m_strClassSchemaPath =
                    Config.GetSelfDescribingClassSchemaDir();
            }
            string strFileName = Path.Combine(
                m_strClassSchemaPath,
                strClassName + ".xml");
            return strFileName;
        }

        #endregion

        public static List<SelfDescribingClass> ConvertToSelfDescribe<T>(
            List<T[]> item,
            List<string> titles,
            string strName)
        {
            try
            {
                List<SelfDescribingClass> list =
                    new List<SelfDescribingClass>();
                foreach (var kvp in item)
                {
                    var selfDescribingClass = new SelfDescribingClass();
                    selfDescribingClass.SetClassName(
                        strName);
                    for (int i = 0; i < titles.Count; i++)
                    {
                        selfDescribingClass.SetValueToDictByType(
                            titles[i],
                            kvp[i]);
                    }
                    list.Add(selfDescribingClass);
                }
                return list;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public static SelfDescribingClass ConvertToSelfDescribe<T>(
            Dictionary<string,T> item,
            string strName)
        {
            try
            {
                var selfDescribingClass = new SelfDescribingClass();
                selfDescribingClass.SetClassName(
                    strName);
                foreach (var kvp in item)
                {
                    selfDescribingClass.SetValueToDictByType(
                        kvp.Key,
                        kvp.Value);
                }
                return selfDescribingClass;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public static SelfDescribingClass ConvertToSelfDescribing(object item)
        {
            try
            {
                var selfDescribingClass = new SelfDescribingClass();
                selfDescribingClass.SetClassName(item.GetType().Name + "_selfDescr");
                IReflector reflector = ReflectorCache.GetReflector(item.GetType());
                var names = reflector.GetPropertyNames();
                foreach (string strName in names)
                {
                    var val = reflector.GetPropertyValue(item, strName);
                    if(val == null)
                    {
                        continue;
                    }
                    selfDescribingClass.SetValueToDictByType(
                        strName,
                        val);
                }
                return selfDescribingClass;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }
    }
}


