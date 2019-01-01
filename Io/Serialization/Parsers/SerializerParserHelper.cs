#region

using System;
using System.Collections.Generic;
using HC.Core.DynamicCompilation;
using HC.Core.Logging;
using HC.Core.Reflection;

#endregion

namespace HC.Core.Io.Serialization.Parsers
{
    public static class SerializerParserHelper
    {
        #region Members

        public static Dictionary<Type, string> ReadMethods { get; private set; }

        #endregion

        #region Constructors

        static SerializerParserHelper()
        {
            ReadMethods = new Dictionary<Type, string>();
            ReadMethods[typeof(int)] = "ReadInt32";
            ReadMethods[typeof(long)] = "ReadInt64";
            ReadMethods[typeof(double)] = "ReadDouble";
            ReadMethods[typeof(bool)] = "ReadBoolean";
            ReadMethods[typeof(byte)] = "ReadByte";
            ReadMethods[typeof(char)] = "ReadChar";
            ReadMethods[typeof(DateTime)] = "ReadDateTime";
            ReadMethods[typeof(string)] = "ReadString";
            ReadMethods[typeof(TimeSpan)] = "ReadTimeSpan";
        }

        #endregion

        #region Public

        public static void Parse(
            Type objType,
            SelfDescribingClassFactory classFactory,
            Type readerType)
        {
            try
            {
            WriterParser.AddWriterMethod(
                objType,
                classFactory);
            ReaderParser.AddReaderMethod(
                readerType,
                objType,
                classFactory);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public static string GetUniqueTypeName()
        {
            try
            {
            return "a" + Guid.NewGuid().ToString()
                .Replace("-",string.Empty)
                .Replace(".", string.Empty)
                .Replace("/", string.Empty);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return string.Empty;
        }

        public static void GetPropertyNameTypes(
            Type reflectedType,
            string strObjName,
            out List<Type> propertyTypes,
            out List<string> propertyNames)
        {
            propertyTypes = null;
            propertyNames = null;
            try
            {
            var reflector = ReflectorCache.GetReflector(reflectedType);
            propertyNames = new List<string>();
            propertyTypes = new List<Type>();
            foreach (string strPropertyName in reflector.GetPropertyNames())
            {
                if (!reflector.CanWriteProperty(strPropertyName))
                {
                    continue;
                }
                propertyTypes.Add(reflector.GetPropertyType(strPropertyName));
                string strObjProperty = strObjName + "." + strPropertyName;
                propertyNames.Add(strObjProperty);
            }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        #endregion
    }
}
