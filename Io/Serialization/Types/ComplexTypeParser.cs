#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using HC.Core.DataStructures;
using HC.Core.Logging;

#endregion

namespace HC.Core.Io.Serialization.Types
{
    public static class ComplexTypeParser
    {
        public static string ToStringType(Type currType)
        {
            try
            {
                if (currType == null)
                {
                    return string.Empty;
                }
                if (typeof (Array).IsAssignableFrom(currType))
                {
                    string strListType;
                    return ToStringArray(currType, out strListType);
                }
                if (typeof (IList).IsAssignableFrom(currType))
                {
                    string strListType;
                    return ToStringList(currType, out strListType);
                }
                if (typeof (IDictionary).IsAssignableFrom(currType))
                {
                    string strKeyType;
                    string strValueType;
                    return ToStringMap(currType, out strKeyType, out strValueType);
                }
                return ParseGenerics(currType);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return string.Empty;
        }

        private static string ParseGenerics(Type currType)
        {
            var sb = new StringBuilder();
            ParseGenericTypes(sb,currType);
            return sb.ToString();
        }

        private static void ParseGenericTypes(
            StringBuilder sb,
            Type currType)
        {
            Type[] generics = currType.GetGenericArguments();
            if (generics.Length > 0)
            {
                sb.Append(currType.Name.Split('`')[0])
                  .Append("<");
                ParseGenericTypes(sb, generics[0]);
                for (int i = 1; i < generics.Length; i++)
                {
                    sb.Append(",");
                    ParseGenericTypes(sb, generics[i]);
                }
                sb.Append(">");
            }
            else
            {
                sb.Append(currType.Name);
            }
        }

        public static string ToStringArray(
            Type currType,
            out string strElementType)
        {
            strElementType = null;
            try
            {
                Type elementType = currType.GetElementType();
                strElementType = ToStringType(elementType);
                string commas = string.Empty;
                if (currType.GetArrayRank() > 1)
                {
                    for (int i = 1; i < currType.GetArrayRank(); i++)
                    {
                        commas += ",";
                    }
                }
                return strElementType + "[" + commas + "]";
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return "";
        }

        public static string ToStringArraysType(
            Type currType,
            out string strElementType)
        {
            Type elementType = currType.GetElementType();
            strElementType = ToStringType(elementType);
            return strElementType;
        }

        public static string ToStringList(
            Type currType,
            out string strElementType)
        {
            Type[] generics = currType.GetGenericArguments();
            //var instance = (IList)ReflectorCache.GetReflector(currType).CreateInstance();
            Type elementType = generics[0];
            strElementType = ToStringType(elementType);
            return "List<" + strElementType + ">";
        }


        public static string ToStringMap(
            Type currType,
            out string strKeyType,
            out string strValueType)
        {
            string strTypeDef;
            if (currType.GetGenericTypeDefinition() == typeof (SortedDictionary<,>))
            {
                strTypeDef = "SortedDictionary";
            }
            else if (currType.GetGenericTypeDefinition() == typeof(SerializableDictionary<,>))
            {
                strTypeDef = "SerializableDictionary";
            }
            else
            {
                strTypeDef = "Dictionary";
            }
            Type[] genericTypes = currType.GetGenericArguments();
            var keyType = genericTypes[0];
            var valueType = genericTypes[1];
            strKeyType = ToStringType(keyType);
            strValueType = ToStringType(valueType);
            return strTypeDef + "<" + strKeyType + "," + strValueType  + ">";
        }
    }
}



