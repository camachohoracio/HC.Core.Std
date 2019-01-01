#region

using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

#endregion

namespace HC.Core.Reflection
{
    public interface IReflector
    {
        object GetPropertyValue(object item, string strPropertyName);
        object GetPropertyValue(object item, int i);
        object GetReadWritePropertyValue(object item, int i);
        
        List<string> GetPropertyNames();
        List<string> GetReadWritePropertyNames();
        
        PropertyInfo GetPropertyInfo(string strPropertyName);
        PropertyInfo GetPropertyInfo(int intIndex);
        PropertyInfo GetReadWritePropertyInfo(int intIndex);
        
        void SetPropertyValue(object obj, string strPropertyName, object propetyValue);
        void SetPropertyValue(object obj, int intIndex, object propetyValue);
        void SetReadWritePropertyValue(object obj, int intIndex, object propetyValue);
       
        Type GetPropertyType(string strPropertyName);
        Type GetPropertyType(int intIndex);
        Type GetReadWritePropertyType(int intIndex);

        List<Type> GetPropertyTypes();
        List<Type> GetReadWritePropertyTypes();
        
        bool TryGetPropertyType(
            string strPropertyName, 
            out Type type);
        T CreateInstanceFromDr<T>(DataRow dr);
        bool CanWriteProperty(string strPropertyName);
        object CreateInstance();
        bool IsPropertyValueType(string strPropertyName);
        bool ContainsProperty(string strPropertyName);

        string ToStringObject(Object obj, string strDelim);
    }
}