#region

using System;
using System.Collections.Generic;
using HC.Core.Io.Serialization.Types;
using HC.Core.Reflection;

#endregion

namespace HC.Core.Helpers
{
    public static class ReflectionHelper
    {
        #region Members

        private static readonly Type m_dblType = typeof (double);
        private static readonly Type m_intType = typeof (int);
        private static readonly Type m_lngType = typeof (long);
        private static readonly Type m_blnType = typeof (bool);
        private static readonly Type m_dateType = typeof (DateTime);

        #endregion

        public static object GetDefaltValue(Type type)
        {
            if (type == typeof(string))
            {
                return string.Empty;
            }
            if (type == typeof(double))
            {
                return double.NaN;
            }
            if (type == typeof(int))
            {
                return 0;
            }
            if (type == typeof(long))
            {
                return 0L;
            }
            if (type == typeof(DateTime))
            {
                return new DateTime();
            }
            if (type == typeof(char))
            {
                return Char.MinValue;
            }
            return null;
        }

        public static string GetTypeNameRecursive(Type type)
        {
            if (type.IsArray)
            {
                Type elementType = type.GetElementType();
                if(elementType.IsArray)
                {
                    return "Array_" + GetTypeNameRecursive(elementType) + "_" +
                        type.GetArrayRank();
                }
                return "Array_" + ComplexTypeParser.ToStringType(elementType) + "_" +
                    type.GetArrayRank();
            }
            //if (typeof(IList).IsAssignableFrom(type))
            //{
            //    var entryType = type.GetGenericArguments()[0];
            //    return "List_" + GetTypeNameRecursive(entryType);
            //}
            //if (typeof(IDictionary).IsAssignableFrom(type))
            //{
            //    Type[] generics = type.GetGenericArguments();
            //    var keyType = generics[0];
            //    var valueType = generics[1];
                
            //    return "Dictionary_" + GetTypeNameRecursive(keyType) + "_" +
            //        GetTypeNameRecursive(valueType);
            //}
            string strTypeName = ComplexTypeParser.ToStringType(type)
                .Replace("<", "_")
                .Replace(">", "_")
                .Replace(".", "_")
                .Replace(",", "_")
                .Replace("[", "_")
                .Replace("]", "_");
            return strTypeName;
        }

        public static object GetDefaultValueType(Type type)
        {
            if (type == m_dblType)
            {
                return 0.0;
            }
            if (type == m_intType)
            {
                return 0;
            }
            if (type == m_lngType)
            {
                return 0L;
            }
            if (type == m_blnType)
            {
                return false;
            }
            if (type == m_dateType)
            {
                return new DateTime();
            }
            return null;
        }


        public static void GetPropertyValues(
            string strPrefix,
            object obj,
            IDictionary<string, string> dictionary)
        {
            var binder = ReflectorCache.GetReflector(obj.GetType());
            foreach (string strPropertyName in binder.GetPropertyNames())
            {
                object currObj = binder.GetPropertyValue(obj, strPropertyName);
                dictionary[strPrefix + "_" + strPropertyName] =
                    (currObj == null ? string.Empty : currObj.ToString());
            }
        }
    }
}


