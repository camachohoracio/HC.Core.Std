#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HC.Core.Exceptions;
using HC.Core.Helpers;
using HC.Core.Io.Serialization;
using HC.Core.Io.Serialization.Interfaces;
using HC.Core.Io.Serialization.Readers;
using HC.Core.Io.Serialization.Types;
using HC.Core.Io.Serialization.Writers;
using HC.Core.Logging;
using HC.Core.Reflection;
using HC.Core.Threading;
using HC.Core.Time;

#endregion

namespace HC.Core.DynamicCompilation
{
    [Serializable]
    public abstract class ASelfDescribingClass : ISerializable, IDisposable

    {
        #region Members

        protected readonly Dictionary<string, bool> m_blnValues;
        protected readonly Dictionary<string, DateTime> m_dateValues;
        protected readonly Dictionary<string, double> m_dblValues;
        protected readonly Dictionary<string, int> m_intValues;
        protected readonly Dictionary<string, long> m_lngValues;
        private readonly Dictionary<string, object> m_objValues;
        protected readonly Dictionary<string, string> m_strValues;

        [NonSerialized]
        protected IReflector m_reflector;

        protected string m_strClassName;

        #endregion

        #region Constructors

        public ASelfDescribingClass()
            : this(string.Empty)
        {
        }

        public ASelfDescribingClass(
            string strClassName)
        {
            //
            // initialize lookup caches
            //
            m_strClassName = strClassName;
            m_intValues = new Dictionary<string, int>();
            m_strValues = new Dictionary<string, string>();
            m_blnValues = new Dictionary<string, bool>();
            m_lngValues = new Dictionary<string, long>();
            m_dblValues = new Dictionary<string, double>();
            m_dateValues = new Dictionary<string, DateTime>();
            m_objValues = new Dictionary<string, object>();
        }

        #endregion

        #region Public

        public void SaveToXml(string strXmlFileName)
        {
            try
            {
                lock (LockObjectHelper.GetLockObject(strXmlFileName))
                {
                    var sb =
                        new StringBuilder("<?xml version=" + '"' + "1.0" + '"' + " encoding=" + '"' + "utf-8" + '"' +
                                          "?>")
                            .AppendLine();
                    sb.AppendLine("<constants>");
                    foreach (string strPropertyName in GetHardPropertyNames())
                    {
                        var value = GetHardPropertyValue(strPropertyName);
                        sb.AppendLine("<" + strPropertyName + ">");
                        sb.AppendLine((value == null ? string.Empty : value.ToString().Trim()) + " ");
                        sb.AppendLine("</" + strPropertyName + ">");
                    }

                    foreach (KeyValuePair<string, bool> kvp in m_blnValues)
                    {
                        string strPropertyName = kvp.Key.Trim();
                        string strPropertyValue = kvp.Value.ToString().Trim();
                        sb.AppendLine("<" + strPropertyName + ">");
                        sb.AppendLine(strPropertyValue);
                        sb.AppendLine("</" + strPropertyName + ">");
                    }
                    foreach (KeyValuePair<string, double> kvp in m_dblValues)
                    {
                        string strPropertyName = kvp.Key.Trim();
                        string strPropertyValue = kvp.Value.ToString().Trim();
                        sb.AppendLine("<" + strPropertyName + ">");
                        sb.AppendLine(strPropertyValue);
                        sb.AppendLine("</" + strPropertyName + ">");
                    }
                    foreach (KeyValuePair<string, int> kvp in m_intValues)
                    {
                        string strPropertyName = kvp.Key.Trim();
                        string strPropertyValue = kvp.Value.ToString().Trim();
                        sb.AppendLine("<" + strPropertyName + ">");
                        sb.AppendLine(strPropertyValue);
                        sb.AppendLine("</" + strPropertyName + ">");
                    }
                    foreach (KeyValuePair<string, DateTime> kvp in m_dateValues)
                    {
                        string strPropertyName = kvp.Key.Trim();
                        string strPropertyValue = kvp.Value.ToString().Trim();
                        sb.AppendLine("<" + strPropertyName + ">");
                        sb.AppendLine(strPropertyValue);
                        sb.AppendLine("</" + strPropertyName + ">");
                    }
                    foreach (KeyValuePair<string, long> kvp in m_lngValues)
                    {
                        string strPropertyName = kvp.Key.Trim();
                        string strPropertyValue = kvp.Value.ToString().Trim();
                        sb.AppendLine("<" + strPropertyName + ">");
                        sb.AppendLine(strPropertyValue);
                        sb.AppendLine("</" + strPropertyName + ">");
                    }
                    foreach (KeyValuePair<string, string> kvp in m_strValues)
                    {
                        string strPropertyName = kvp.Key.Trim();
                        string strPropertyValue = kvp.Value.Trim();
                        sb.AppendLine("<" + strPropertyName + ">");
                        sb.AppendLine(strPropertyValue);
                        sb.AppendLine("</" + strPropertyName + ">");
                    }
                    foreach (KeyValuePair<string, object> kvp in m_objValues)
                    {
                        string strPropertyName = kvp.Key.Trim();
                        string strPropertyValue = kvp.Value.ToString().Trim();
                        sb.AppendLine("<" + strPropertyName + ">");
                        sb.AppendLine("<constants>");
                        sb.AppendLine(strPropertyValue);
                        sb.AppendLine("</constants>");
                        sb.AppendLine("</" + strPropertyName + ">");
                    }
                    sb.AppendLine("</constants>");
                    var strDescr = sb.ToString().Trim();
                    if (string.IsNullOrEmpty(strDescr))
                    {
                        throw new HCException("Null description");
                    }
                    using (var sw = new StreamWriter(strXmlFileName))
                    {
                        sw.WriteLine(strDescr);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public virtual object Clone()
        {
            var obj = (ASelfDescribingClass)ReflectorCache.GetReflector(GetType()).CreateInstance();
            obj.SetClassName(GetClassName());
            List<string> hardPropNames = obj.GetHardPropertyNames();
            for (int i = 0; i < hardPropNames.Count; i++)
            {
                string strProperty = hardPropNames[i];
                if (!m_reflector.CanWriteProperty(strProperty))
                {
                    continue;
                }
                var propertyValue = GetHardPropertyValue(strProperty);
                var cloneable = propertyValue as ICloneable;
                propertyValue = cloneable != null ? cloneable.Clone() : ClonerHelper.Clone(propertyValue);

                obj.SetHardPropertyValue(strProperty, propertyValue);
            }

            foreach (KeyValuePair<string, bool> kvp in m_blnValues)
            {
                obj.SetBlnValue(
                    kvp.Key,
                    kvp.Value);
            }
            foreach (KeyValuePair<string, double> kvp in m_dblValues)
            {
                obj.SetDblValue(
                    kvp.Key,
                    kvp.Value);
            }
            foreach (KeyValuePair<string, int> kvp in m_intValues)
            {
                obj.SetIntValue(
                    kvp.Key,
                    kvp.Value);
            }
            foreach (KeyValuePair<string, DateTime> kvp in m_dateValues)
            {
                obj.SetDateValue(
                    kvp.Key,
                    kvp.Value);
            }
            foreach (KeyValuePair<string, long> kvp in m_lngValues)
            {
                obj.SetLngValue(
                    kvp.Key,
                    kvp.Value);
            }
            foreach (KeyValuePair<string, string> kvp in m_strValues)
            {
                obj.SetStrValue(
                    kvp.Key,
                    kvp.Value);
            }
            foreach (KeyValuePair<string, object> kvp in m_objValues)
            {
                obj.SetObjValueToDict(
                    kvp.Key,
                    kvp.Value);
            }

            return obj;
        }

        public string GetClassName()
        {
            return m_strClassName;
        }

        public void SetClassName(Enum enumValue)
        {
            SetClassName(enumValue.ToString());
        }

        public void SetClassName(string strClassName)
        {
            m_strClassName = strClassName;
        }

        public void CopyTo(
            ASelfDescribingClass otherSelfDescribingClass)
        {
            CopyTo(
                otherSelfDescribingClass,
                string.Empty);
        }

        public void CopyTo(
            ASelfDescribingClass otherSelfDescribingClass,
            string strKeyPrefix)
        {
            strKeyPrefix = (string.IsNullOrEmpty(strKeyPrefix)
                                ? string.Empty
                                : strKeyPrefix + "_");
            //
            // note => We need to lock the dictionaries since these values can change
            // at runtime. Adding a concurrent dictionary would slow down the performance and add memmory footprint
            //
            //
            lock (m_blnValues)
            {
                foreach (KeyValuePair<string, bool> kvp in m_blnValues)
                {
                    otherSelfDescribingClass.SetBlnValue(
                        strKeyPrefix + kvp.Key, kvp.Value);
                }
            }
            lock (m_dblValues)
            {
                foreach (KeyValuePair<string, double> kvp in m_dblValues)
                {
                    otherSelfDescribingClass.SetDblValue(
                        strKeyPrefix + kvp.Key, kvp.Value);
                }
            }
            lock (m_intValues)
            {
                foreach (KeyValuePair<string, int> kvp in m_intValues)
                {
                    otherSelfDescribingClass.SetIntValue(
                        strKeyPrefix + kvp.Key, kvp.Value);
                }
            }
            lock (m_dateValues)
            {
                foreach (KeyValuePair<string, DateTime> kvp in m_dateValues)
                {
                    otherSelfDescribingClass.SetDateValue(
                        strKeyPrefix + kvp.Key, kvp.Value);
                }
            }
            lock (m_lngValues)
            {
                foreach (KeyValuePair<string, long> kvp in m_lngValues)
                {
                    otherSelfDescribingClass.SetLngValue(
                        strKeyPrefix + kvp.Key, kvp.Value);
                }
            }
            lock (m_strValues)
            {
                foreach (KeyValuePair<string, string> kvp in m_strValues)
                {
                    otherSelfDescribingClass.SetStrValue(
                        strKeyPrefix + kvp.Key, kvp.Value);
                }
            }
            lock (m_objValues)
            {
                foreach (KeyValuePair<string, object> kvp in m_objValues)
                {
                    otherSelfDescribingClass.SetObjValueToDict(
                        strKeyPrefix + kvp.Key, kvp.Value);
                }
            }
            //
            // set property values
            //
            var thisDynamicType = GetType();
            var thisClassBinderObj = ReflectorCache.GetReflector(thisDynamicType);
            var otherDynamicType = otherSelfDescribingClass.GetType();
            var otherClassBinderObj = ReflectorCache.GetReflector(otherDynamicType);
            foreach (string strPropertyName in thisClassBinderObj.GetPropertyNames())
            {
                var objValue = thisClassBinderObj.GetPropertyValue(
                    this,
                    strPropertyName);
                if (otherClassBinderObj.ContainsProperty(
                    strPropertyName) &&
                    otherClassBinderObj.CanWriteProperty(strPropertyName))
                {
                    otherClassBinderObj.SetPropertyValue(
                        otherSelfDescribingClass,
                        strPropertyName,
                        objValue);
                }
                else
                {
                    Type propertyType;
                    if(objValue == null)
                    {
                        propertyType = GetPropertyType(strPropertyName);
                        objValue = ReflectorCache.GetReflector(propertyType).CreateInstance();
                    }
                    else
                    {
                        propertyType = objValue.GetType();
                    }
                    //
                    // set value as a map
                    //
                    if (propertyType == typeof(int))
                    {
                        otherSelfDescribingClass.SetIntValue(strPropertyName,
                                                                   (int)objValue);
                    }
                    else if (propertyType == typeof(double))
                    {
                        otherSelfDescribingClass.SetDblValue(strPropertyName,
                                                                   (double)objValue);
                    }
                    else if (propertyType == typeof(string))
                    {
                        otherSelfDescribingClass.SetStrValue(strPropertyName,
                                                                   (string)objValue);
                    }
                    else if (propertyType == typeof(DateTime))
                    {
                        otherSelfDescribingClass.SetDateValue(strPropertyName,
                                                                    (DateTime)objValue);
                    }
                    else if (propertyType == typeof(bool))
                    {
                        otherSelfDescribingClass.SetBlnValue(strPropertyName,
                                                                   (bool)objValue);
                    }
                    else if (propertyType == typeof(long))
                    {
                        otherSelfDescribingClass.SetLngValue(strPropertyName,
                                                                   (long)objValue);
                    }
                    else
                    {
                        otherSelfDescribingClass.SetObjValueToDict(strPropertyName,
                                                                   objValue);
                    }
                }
            }
        }

        public bool TryGetStringArr(
            Enum enumValue,
            out string[] result)
        {
            return TryGetStringArr(
                enumValue.ToString(),
                out result);
        }

        public bool TryGetStringArr(
            string strPropertyName,
            out string[] result)
        {
            if (ContainsHardProperty(strPropertyName))
            {
                result = GetStringArr(strPropertyName);
                return true;
            }
            result = null;
            return false;
        }

        public string[] GetStringArr(Enum enumValue)
        {
            return GetStringArr(enumValue.ToString());
        }

        public T GetHardPropertyValue<T>(string strPropertyName)
        {
            ValidateReflector();
            var propValue = m_reflector.GetPropertyValue(this, strPropertyName);
            if (propValue is T)
            {
                return (T) propValue;
            }
            return default(T);
        }

        public string[] GetStringArr(string strParamName)
        {
            string strValue = GetStrValue(strParamName);
            var tokens = strValue.Split(", \n\t".ToCharArray());
            var selectedTokens = new List<string>();
            foreach (string strToken in tokens)
            {
                var strCleanToken = strToken.Trim();
                if (!string.IsNullOrEmpty(strCleanToken))
                {
                    selectedTokens.Add(strCleanToken);
                }
            }
            return selectedTokens.ToArray();
            //}
        }

        public Dictionary<string, string> GetStringValues()
        {
            return m_strValues;
        }

        public Dictionary<string, double> GetDblValues()
        {
            return m_dblValues;
        }

        public Dictionary<string, int> GetIntValues()
        {
            return m_intValues;
        }

        public Dictionary<string, bool> GetBlnValues()
        {
            return m_blnValues;
        }

        public Dictionary<string, long> GetLngValues()
        {
            return m_lngValues;
        }

        public Dictionary<string, object> GetObjValues()
        {
            return m_objValues;
        }

        public Dictionary<string, DateTime> GetDateValues()
        {
            return m_dateValues;
        }

        public string GetStrValue(
            Enum enumValue)
        {
            return GetStrValue(
                enumValue.ToString());
        }

        public string GetStrValue(
            string strPropertyName)
        {
            string strValue;
            if (!TryGetStrValue(strPropertyName, out strValue))
            {
                throw new HCException("Value not found for key [" +
                strPropertyName + "]");
            }
            return strValue;
        }

        private string GetHardStrValue(string strPropertyName)
        {
            var strValue = GetHardPropertyValue<string>(strPropertyName);
            return strValue;
        }

        public int GetIntValue(
            Enum enumValue)
        {
            return GetIntValue(enumValue.ToString());
        }

        public int GetIntValue(
            string strPropertyName)
        {
            int intValue;
            if (!TryGetIntValue(strPropertyName, out intValue))
            {
                throw new HCException("Value not found for key [" +
                strPropertyName + "]");
            }
            return intValue;
        }

        private int GetHardIntValue(string strPropertyName)
        {
            var objValue = GetHardPropertyValue<object>(strPropertyName);
            var intValue = ParserHelper.ParseObject<int>(objValue);
            return intValue;
        }

        private object GetHardObjValue(string strPropertyName)
        {
            var objValue = GetHardPropertyValue<object>(strPropertyName);
            return objValue;
        }

        public bool ContainsHardProperty(Enum enumValue)
        {
            return ContainsHardProperty(enumValue.ToString());
        }

        public bool ContainsProperty(string strPropertyName)
        {
            if (ContainsHardProperty(strPropertyName))
            {
                return true;
            }
            if (m_blnValues.ContainsKey(strPropertyName) ||
                m_dateValues.ContainsKey(strPropertyName) ||
                m_dblValues.ContainsKey(strPropertyName) ||
                m_intValues.ContainsKey(strPropertyName) ||
                m_lngValues.ContainsKey(strPropertyName) ||
                m_objValues.ContainsKey(strPropertyName) ||
                m_strValues.ContainsKey(strPropertyName))
            {
                return true;
            }
            return false;
        }

        public bool ContainsHardProperty(string strPropertyName)
        {
            ValidateReflector();
            if (m_reflector == null)
            {
                return false;
            }
            return m_reflector.ContainsProperty(strPropertyName);
        }

        public double GetDblValue(
            Enum enumName)
        {
            return GetDblValue(enumName.ToString());
        }

        public double GetDblValue(
            string strPropertyName)
        {
            double dblValue;
            if (!TryGetDblValue(strPropertyName, out dblValue))
            {
                throw new HCException("Value not found for key [" +
                strPropertyName + "]");
            }
            return dblValue;
        }

        private double GetHardDblValue(string strPropertyName)
        {
            var objValue = GetHardPropertyValue<object>(strPropertyName);
            if(objValue == null)
            {
                return double.NaN;
            }
            var dblValue = ParserHelper.ParseObject<double>(objValue);
            return dblValue;
        }

        public bool GetBlnValue(
            Enum enumValue)
        {
            return GetBlnValue(enumValue.ToString());
        }

        public bool GetBlnValue(
            string strPropertyName)
        {
            bool blnValue;
            if (!TryGetBlnValue(strPropertyName, out blnValue))
            {
                throw new HCException("Value not found for key [" +
                strPropertyName + "]");
            }
            return blnValue;
        }

        private bool GetHardBlnValue(string strPropertyName)
        {
            var objValue = GetHardPropertyValue<object>(strPropertyName);
            var blnValue = ParserHelper.ParseObject<bool>(objValue);
            return blnValue;
        }

        public long GetLngValue(
            Enum enumValue)
        {
            return GetLngValue(
                enumValue.ToString());
        }

        public long GetLngValue(
            string strPropertyName)
        {
            int intValue;
            if (!TryGetIntValue(strPropertyName, out intValue))
            {
                throw new HCException("Value not found for key [" +
                strPropertyName + "]");
            }
            return intValue;
        }

        private long GetHardLngValue(string strPropertyName)
        {
            var objValue = GetHardPropertyValue<object>(strPropertyName);
            var lngValue = ParserHelper.ParseObject<long>(objValue);
            return lngValue;
        }

        public object GetObjValue(
            Enum enumKey)
        {
            return GetObjValue(
                enumKey.ToString());
        }

        public object GetObjValue(
            string strPropertyName)
        {
            object objValue;
            if (!TryGetObjValue(strPropertyName, out objValue))
            {
                throw new HCException("Value not found for key [" +
                strPropertyName + "]");
            }
            return objValue;
        }

        public void SetIntValue(
            string strKey,
            int intValue)
        {
            lock (m_intValues)
            {
                if (ContainsHardProperty(strKey))
                {
                    SetHardPropertyValue(strKey, intValue);
                }
                else
                {
                    m_intValues[strKey] = intValue;
                }
            }
        }

        public void SetDateValue(
            Enum enumKey,
            DateTime dateValue)
        {
            SetDateValue(enumKey.ToString(),
                               dateValue);
        }

        public void SetDateValue(
            string strKey,
            DateTime dateValue)
        {
            lock (m_dateValues)
            {
                if (ContainsHardProperty(strKey))
                {
                    SetHardPropertyValue(strKey, dateValue);
                }
                else
                {
                    m_dateValues[strKey] = dateValue;
                }
            }
        }


        public void SetLngValue(
            Enum enumKey,
            long lngValue)
        {
            SetLngValue(
                enumKey.ToString(),
                lngValue);
        }

        public void SetLngValue(
            string strKey,
            long lngValue)
        {
            lock (m_lngValues)
            {
                if (ContainsHardProperty(strKey))
                {
                    SetHardPropertyValue(strKey, lngValue);
                }
                else
                {
                    m_lngValues[strKey] = lngValue;
                }
            }
        }


        public void SetIntValue(
            Enum enumKey,
            int intValue)
        {
            SetIntValue(
                enumKey.ToString(),
                intValue);
        }

        public void SetDblValue(
            string strKey,
            double dblValue)
        {
            lock (m_dblValues)
            {
                if (ContainsHardProperty(strKey))
                {
                    SetHardPropertyValue(strKey, dblValue);
                }
                else
                {
                    m_dblValues[strKey] = dblValue;
                }
            }
        }

        public void SetBlnValue(
            Enum enumKey,
            bool blnValue)
        {
            SetBlnValue(
                enumKey.ToString(),
                blnValue);
        }

        public void SetBlnValue(
            string strKey,
            bool blnValue)
        {
            if (ContainsHardProperty(strKey))
            {
                SetHardPropertyValue(strKey, blnValue);
            }
            else
            {
                m_blnValues[strKey] = blnValue;
            }
        }


        public void SetDblValue(
            Enum enumKey,
            double dblValue)
        {
            SetDblValue(
                enumKey.ToString(),
                dblValue);
        }

        public void SetStrValue(
            Enum enumKey,
            string strValue)
        {
            SetStrValue(
                enumKey.ToString(),
                strValue);
        }

        public void SetStrValue(
            string strKey,
            string strValue)
        {
            lock (m_strValues)
            {
                if (ContainsHardProperty(strKey))
                {
                    SetHardPropertyValue(strKey, strValue);
                }
                else
                {
                    m_strValues[strKey] = strValue;
                }
            }
        }

        public void SetObjValueToDict(
            Enum enumKey,
            object oValue)
        {
            SetObjValueToDict(
                enumKey.ToString(),
                oValue);
        }

        public void SetObjValueToDict(
            string strKey,
            object oValue)
        {
            lock (m_objValues)
            {
                m_objValues[strKey] = oValue;
            }
        }

        public List<string> GetAllPropertyNames()
        {
            try
            {
                List<string> properties = GetHardPropertyNames();
                var softProps = GetSoftPropertyNames();
                if (softProps != null &&
                    softProps.Count > 0)
                {
                    properties.AddRange(softProps);
                }
                return properties.Distinct().ToList();
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return new List<string>();
        }

        public List<string> GetSoftPropertyNames()
        {
            try
            {
                var properties = new List<string>();
                lock (m_intValues)
                {
                    if (m_intValues.Count > 0)
                    {
                        properties.AddRange(m_intValues.Keys);
                    }
                }
                lock (m_dblValues)
                {
                    if (m_dblValues.Count > 0)
                    {
                        properties.AddRange(m_dblValues.Keys);
                    }
                }
                lock (m_blnValues)
                {
                    if (m_blnValues.Count > 0)
                    {
                        properties.AddRange(m_blnValues.Keys);
                    }
                }
                lock (m_strValues)
                {
                    if (m_strValues.Count > 0)
                    {
                        properties.AddRange(m_strValues.Keys);
                    }
                }
                lock (m_objValues)
                {
                    if (m_objValues.Count > 0)
                    {
                        properties.AddRange(m_objValues.Keys);
                    }
                }
                lock (m_lngValues)
                {
                    if (m_lngValues.Count > 0)
                    {
                        properties.AddRange(m_lngValues.Keys);
                    }
                }
                lock (m_dateValues)
                {
                    if (m_dateValues.Count > 0)
                    {
                        properties.AddRange(m_dateValues.Keys);
                    }
                }
                return properties;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return new List<string>();
        }

        public List<string> GetHardPropertyNames()
        {
            ValidateReflector();
            if (m_reflector == null)
            {
                return new List<string>();
            }
            List<string> properties = m_reflector.GetPropertyNames();
            return properties.ToList();
        }

        public Type GetPropertyType(string strPropertyName)
        {
            ValidateReflector();
            Type type;
            if(!m_reflector.TryGetPropertyType(strPropertyName, out type))
            {
                object objRes;
                if(TryGetValueFromAnyProperty(strPropertyName, out objRes) &&
                    objRes != null)
                {
                    return objRes.GetType();
                }
            }
            return type;
        }

        public void SetHardPropertyValue(
            string strPropertyName,
            object objValue)
        {
            if (objValue == null)
            {
                return;
            }

            ValidateReflector();
            var propertyType = GetPropertyType(strPropertyName);
            if (objValue.GetType() != propertyType)
            {
                var strObjValue = objValue.ToString();
                if (propertyType == typeof(string))
                {
                    //
                    // easy fix, just set a string value
                    //
                    m_reflector.SetPropertyValue(this, strPropertyName, strObjValue);
                }
                else
                {
                    if (objValue.GetType() == propertyType)
                    {
                        m_reflector.SetPropertyValue(this, strPropertyName, objValue);
                    }
                    else
                    {
                        if (propertyType == typeof(object))
                        {
                            m_reflector.SetPropertyValue(this, strPropertyName, objValue);
                        }
                        else
                        {
                            //
                            // Try to parse a string. Note that if the parser fails, the object will be wasted
                            //
                            var obj = ParserHelper.ParseString(strObjValue,
                                                                  propertyType);
                            if (obj != null)
                            {
                                m_reflector.SetPropertyValue(this, strPropertyName, obj);
                            }
                        }
                    }
                }
            }
            else
            {
                //
                // set the property value
                //
                m_reflector.SetPropertyValue(this, strPropertyName, objValue);
            }
        }


        public object ExecuteMethod(string strMethodName)
        {
            return ExecuteMethod(strMethodName,
                    null);
        }

        public object ExecuteMethod(string strMethodName,
            object[] parameters)
        {
            try
            {
                MethodInfo methodInfo = GetType().GetMethod(strMethodName);
                return methodInfo.Invoke(
                    this,
                    parameters);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }


        public string ToCsvString()
        {
            return ToCsvString(",", false);
        }

        public string ToCsvString(
            string strDelim,
            bool blnLoadProps)
        {
            var sb = new StringBuilder();
            var blnTitleAdded = false;
            var validator =
                new Dictionary<string, object>();

            //
            // string values
            //
            foreach (KeyValuePair<string, string> keyValuePair in m_strValues)
            {
                if (blnTitleAdded)
                {
                    sb.Append(strDelim);
                }
                else
                {
                    blnTitleAdded = true;
                }
                var strColName = keyValuePair.Key;
                HCException.ThrowIfTrue(validator.ContainsKey(strColName),
                    "Column already in event " + strColName);

                validator.Add(strColName, null);

                if(blnLoadProps)
                {
                    sb.Append(keyValuePair.Key.Trim() + "=>");
                }

                sb.Append(keyValuePair.Value.Trim());
            }

            //
            // int values
            //
            foreach (KeyValuePair<string, int> keyValuePair in m_intValues)
            {
                if (blnTitleAdded)
                {
                    sb.Append(strDelim);
                }
                else
                {
                    blnTitleAdded = true;
                }
                var strColName = keyValuePair.Key;
                HCException.ThrowIfTrue(validator.ContainsKey(strColName),
                    "Column already in event " + strColName);

                validator.Add(strColName, null);
                if (blnLoadProps)
                {
                    sb.Append(keyValuePair.Key.Trim() + "=>");
                }
                
                sb.Append(keyValuePair.Value);
            }

            //
            // double values
            //
            foreach (KeyValuePair<string, double> keyValuePair in m_dblValues)
            {
                if (blnTitleAdded)
                {
                    sb.Append(strDelim);
                }
                else
                {
                    blnTitleAdded = true;
                }
                var strColName = keyValuePair.Key;
                HCException.ThrowIfTrue(validator.ContainsKey(strColName),
                                               "Column already in event " + strColName);
                validator.Add(strColName, null);
                if (blnLoadProps)
                {
                    sb.Append(keyValuePair.Key.Trim() + "=>");
                }
                sb.Append(keyValuePair.Value);
            }

            //
            // boolean values
            //
            foreach (KeyValuePair<string, bool> keyValuePair in m_blnValues)
            {
                if (blnTitleAdded)
                {
                    sb.Append(strDelim);
                }
                else
                {
                    blnTitleAdded = true;
                }
                var strColName = keyValuePair.Key;
                HCException.ThrowIfTrue(validator.ContainsKey(strColName),
                    "Column already in event " + strColName);

                validator.Add(strColName, null);
                if (blnLoadProps)
                {
                    sb.Append(keyValuePair.Key.Trim() + "=>");
                }
                sb.Append(keyValuePair.Value);
            }

            //
            // long values
            //
            foreach (KeyValuePair<string, long> keyValuePair in m_lngValues)
            {
                if (blnTitleAdded)
                {
                    sb.Append(strDelim);
                }
                else
                {
                    blnTitleAdded = true;
                }
                var strColName = keyValuePair.Key;
                HCException.ThrowIfTrue(validator.ContainsKey(strColName),
                    "Column already in event " + strColName);

                validator.Add(strColName, null);
                if (blnLoadProps)
                {
                    sb.Append(keyValuePair.Key.Trim() + "=>");
                }
                sb.Append(keyValuePair.Value);
            }

            //
            // date values
            //
            foreach (KeyValuePair<string, DateTime> keyValuePair in m_dateValues)
            {
                if (blnTitleAdded)
                {
                    sb.Append(strDelim);
                }
                else
                {
                    blnTitleAdded = true;
                }
                var strColName = keyValuePair.Key;
                HCException.ThrowIfTrue(validator.ContainsKey(strColName),
                    "Column already in event " + strColName);

                validator.Add(strColName, null);
                if (blnLoadProps)
                {
                    sb.Append(keyValuePair.Key.Trim() + "=>");
                }
                sb.Append(
                    DateHelper.ToDateTimeString(
                        keyValuePair.Value));
            }

            //
            // properties
            //
            if (m_reflector != null)
            {
                foreach (string strPropertyName in m_reflector.GetPropertyNames())
                {
                    if (blnTitleAdded)
                    {
                        sb.Append(strDelim);
                    }
                    else
                    {
                        blnTitleAdded = true;
                    }
                    //string strColName = keyValuePair.Key;
                    HCException.ThrowIfTrue(validator.ContainsKey(strPropertyName),
                        "Column already in event " + strPropertyName);

                    validator.Add(strPropertyName, null);
                    var oValue = GetHardPropertyValue(strPropertyName);
                    if (blnLoadProps)
                    {
                        sb.Append(strPropertyName.Trim() + "=>");
                    }
                    sb.AppendLine(oValue == null ? string.Empty : oValue.ToString().Trim());
                }
            }
            return sb.ToString();
        }
        
        public override string ToString()
        {
            try
            {
                var sb =
                    new StringBuilder();
                foreach (string strPropertyName in GetHardPropertyNames())
                {
                    var value = GetHardPropertyValue(strPropertyName);
                    sb.AppendLine(
                        strPropertyName + "=" +
                        (value == null ? string.Empty : value.ToString().Trim()) + " ");
                }

                foreach (KeyValuePair<string, bool> kvp in m_blnValues)
                {
                    sb.AppendLine(
                        kvp.Key.Trim() + "=" +
                        kvp.ToString().Trim() + " ");
                }
                foreach (KeyValuePair<string, double> kvp in m_dblValues)
                {
                    sb.AppendLine(
                        kvp.Key.Trim() + "=" +
                        kvp.ToString().Trim() + " ");
                }
                foreach (KeyValuePair<string, int> kvp in m_intValues)
                {
                    sb.AppendLine(
                        kvp.Key.Trim() + "=" +
                        kvp.ToString().Trim() + " ");
                }
                foreach (KeyValuePair<string, DateTime> kvp in m_dateValues)
                {
                    sb.AppendLine(
                        kvp.Key.Trim() + "=" +
                        kvp.ToString().Trim() + " ");
                }
                foreach (KeyValuePair<string, long> kvp in m_lngValues)
                {
                    sb.AppendLine(
                        kvp.Key.Trim() + "=" +
                        kvp.ToString().Trim() + " ");
                }
                foreach (KeyValuePair<string, string> kvp in m_strValues)
                {
                    sb.AppendLine(
                        kvp.Key.Trim() + "=" +
                        kvp.ToString().Trim() + " ");
                }
                foreach (KeyValuePair<string, object> kvp in m_objValues)
                {
                    sb.AppendLine(
                        kvp.Key.Trim() + "=" +
                        kvp.ToString().Trim() + " ");
                }

                var strDescr = sb.ToString().Trim();
                if (string.IsNullOrEmpty(strDescr))
                {
                    throw new HCException("Null description");
                }
                return strDescr;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return string.Empty;
        }

        public void Dispose()
        {
            ResetMaps();
        }

        public DateTime GetDateValue(Enum enumValue)
        {
            return GetDateValue(enumValue.ToString());
        }

        public DateTime GetDateValue(string strPropertyName)
        {
            DateTime dateValue;
            if (!TryGetDateValue(strPropertyName, out dateValue))
            {
                throw new HCException("Value not found for key [" +
                strPropertyName + "]");
            }
            return dateValue;
        }

        private DateTime GetHardDateValue(string strPropertyName)
        {
            var objValue = GetHardPropertyValue(strPropertyName);
            if (objValue == null)
            {
                return new DateTime();
            }
            var dateValue = ParserHelper.ParseObject<DateTime>(objValue);
            return dateValue;
        }

        #endregion

        #region ISerializable Members

        public virtual byte[] GetByteArr()
        {
            var writer = Serializer.GetWriter();
            Serialize(writer);
            return writer.GetBytes();
        }

        public virtual object Deserialize(byte[] bytes)
        {
            try
            {
                ISerializerReader serializationReader = Serializer.GetReader(bytes);
                if(serializationReader == null)
                {
                    return null;
                }
                return DeserializeStatic(serializationReader);
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        #endregion

        public virtual void Serialize(ISerializerWriter writerBase)
        {
            try
            {
                if (writerBase == null)
                {
                    return;
                }
                if (string.IsNullOrEmpty(m_strClassName))
                {
                    throw new HCException("Empty class name");
                }
                writerBase.Write(m_strClassName);
                writerBase.Write(typeof (SelfDescribingClass));
                ISerializerWriter serializer = SerializeProperties();
                writerBase.Write(serializer.GetBytes());
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        protected ISerializerWriter SerializeProperties()
        {
            try
            {
                var serializer = Serializer.GetWriter();
                lock (m_blnValues)
                {
                    foreach (KeyValuePair<string, bool> kvp in m_blnValues)
                    {
                        serializer.Write((byte)EnumSerializedType.BooleanType);
                        serializer.Write(kvp.Key);
                        serializer.Write(kvp.Value);
                    }
                }
                lock (m_dblValues)
                {
                    foreach (KeyValuePair<string, double> kvp in m_dblValues)
                    {
                        serializer.Write((byte)EnumSerializedType.DoubleType);
                        serializer.Write(kvp.Key);
                        serializer.Write(kvp.Value);
                    }
                }

                lock (m_intValues)
                {
                    foreach (KeyValuePair<string, int> kvp in m_intValues)
                    {
                        serializer.Write((byte)EnumSerializedType.Int32Type);
                        serializer.Write(kvp.Key);
                        serializer.Write(kvp.Value);
                    }
                }
                lock (m_dateValues)
                {
                    foreach (KeyValuePair<string, DateTime> kvp in m_dateValues)
                    {
                        serializer.Write((byte)EnumSerializedType.DateTimeType);
                        serializer.Write(kvp.Key);
                        serializer.Write(kvp.Value);
                    }
                }
                lock (m_lngValues)
                {
                    foreach (KeyValuePair<string, long> kvp in m_lngValues)
                    {
                        serializer.Write((byte)EnumSerializedType.Int64Type);
                        serializer.Write(kvp.Key);
                        serializer.Write(kvp.Value);
                    }
                }
                lock (m_strValues)
                {
                    foreach (KeyValuePair<string, string> kvp in m_strValues)
                    {
                        serializer.Write((byte)EnumSerializedType.StringType);
                        serializer.Write(kvp.Key);
                        serializer.Write(kvp.Value);
                    }
                }
                lock (m_objValues)
                {
                    foreach (KeyValuePair<string, object> kvp in m_objValues.ToArray())
                    {
                        serializer.Write((byte)EnumSerializedType.ObjectType);
                        serializer.Write(kvp.Key);
                        object obj = kvp.Value;

                        if (obj == null)
                        {
                            serializer.Write((byte)EnumSerializedType.NullType);
                        }
                        else
                        {
                            serializer.Write((byte)EnumSerializedType.NonNullType);
                            Type type = obj.GetType();
                            serializer.Write(ComplexTypeSerializer.Serialize(type));
                            SerializerCache.GetSerializer(type).Serialize(
                                obj,
                                serializer);
                        }
                    }
                }

                //
                // set property values
                //
                var expressionBinder = ReflectorCache.GetReflector(GetType());

                //
                // map properties for the given object
                //
                foreach (string strPropertyName in expressionBinder.GetPropertyNames())
                {
                    if (ContainsHardProperty(strPropertyName))
                    {
                        var objValue = expressionBinder.GetPropertyValue(
                            this,
                            strPropertyName);
                        Type propertyType;
                        if (objValue == null)
                        {
                            if (string.IsNullOrEmpty(GetClassName()))
                            {
                                string strMessage = "Null object value for property name [" +
                                                    strPropertyName + "]. class: [" + GetClassName() +
                                                    "]";
                                throw new HCException("Empty class name. " +
                                                      strMessage);
                            }
                            propertyType = expressionBinder.GetPropertyType(strPropertyName);
                            objValue = ReflectionHelper.GetDefaltValue(propertyType);
                        }
                        else
                        {
                            propertyType = objValue.GetType();
                        }

                        serializer.Write((byte) PrimitiveTypesCache.GetSerializedPrimitiveType(propertyType));
                        serializer.Write(strPropertyName);
                        serializer.WriteRaw(objValue);
                    }
                }

                serializer.Write((byte)EnumSerializedType.EndOfProperties);
                return serializer;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        /// <summary>
        /// Leave it public, and do not change the name of the method.
        /// We need it for serialization parsing
        /// </summary>
        /// <param name="serializationReader"></param>
        /// <returns></returns>
        public static object DeserializeStatic(
            ISerializerReader serializationReader)
        {
            string strClassName = serializationReader.ReadString();
            var type = serializationReader.ReadType();
            ASelfDescribingClass selfDescribingClass;
            if(type != typeof(SelfDescribingClass))
            {
                selfDescribingClass = (ASelfDescribingClass)
                    ReflectorCache.GetReflector(type).CreateInstance();
            }
            else
            {
                //
                // get factory
                //
                SelfDescribingClassFactory classFactory = SelfDescribingClassFactory.CreateFactory(
                    strClassName,
                    typeof(ASelfDescribingClass).Namespace);
                if(classFactory.Properties.Count > 0)
                {
                    selfDescribingClass = classFactory.CreateInstance();
                }
                else
                {
                    selfDescribingClass = new SelfDescribingClass();
                }
            }
             
            selfDescribingClass.SetClassName(strClassName);
            var bytes = serializationReader.ReadByteArray();

            if (bytes != null)
            {
                var reader = Serializer.GetReader(
                    bytes);

                DeserializeProperties(selfDescribingClass,
                                      reader);
            }
            return selfDescribingClass;
        }

        public static void DeserializeProperties(
            ASelfDescribingClass selfDescribingClass,
            ISerializerReader serializationReader)
        {
            while (serializationReader.BytesRemaining > 0)
            {
                var serializedType = 
                    (EnumSerializedType)serializationReader.ReadByte();

                if(serializedType == EnumSerializedType.EndOfProperties)
                {
                    break;
                }

                var strKey = serializationReader.ReadString();
                if (serializedType == EnumSerializedType.DoubleType)
                {
                    var objValue = serializationReader.ReadDouble();
                    selfDescribingClass.SetDblValue(strKey, objValue);
                }
                else if (serializedType == EnumSerializedType.Int32Type)
                {
                    var objValue = serializationReader.ReadInt32();
                    selfDescribingClass.SetIntValue(strKey, objValue);
                }
                else if (serializedType == EnumSerializedType.BooleanType)
                {
                    var objValue = serializationReader.ReadBoolean();
                    selfDescribingClass.SetBlnValue(strKey, objValue);
                }
                else if (serializedType == EnumSerializedType.DateTimeType)
                {
                    var objValue = serializationReader.ReadDateTime();
                    selfDescribingClass.SetDateValue(strKey, objValue);
                }
                else if (serializedType == EnumSerializedType.StringType)
                {
                    var objValue = serializationReader.ReadString();
                    selfDescribingClass.SetStrValue(strKey, objValue);
                }
                else if (serializedType == EnumSerializedType.Int64Type)
                {
                    var objValue = serializationReader.ReadInt64();
                    selfDescribingClass.SetLngValue(strKey, objValue);
                }
                else if (serializedType == EnumSerializedType.ObjectType)
                {
                    var serializedObjType = (EnumSerializedType)serializationReader.ReadByte();
                    if (serializedObjType == EnumSerializedType.NullType)
                    {
                        selfDescribingClass.SetObjValueToDict(strKey, null);
                    }
                    else
                    {
                        Type type = ComplexTypeSerializer.Deserialize(
                            serializationReader.ReadByteArray());
                        var objValue = SerializerCache.GetSerializer(type).Deserialize(
                            serializationReader);
                        selfDescribingClass.SetObjValueToDict(strKey, objValue);
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        public object GetValueFromAnyProperty(
            string strPropertyName)
        {
            object objValue;
            if (TryGetValueFromAnyProperty(
                strPropertyName,
                out objValue))
            {
                return objValue;
            }
            throw new HCException("Property not found");
        }

        public bool TryGetValueFromAnyProperty(
            string strPropertyName,
            out object objResult)
        {
            objResult = null;
            bool blnValue;
            if (TryGetBlnValue(strPropertyName, out blnValue))
            {
                objResult = blnValue;
                return true;
            }
            DateTime dateValue;
            if (TryGetDateValue(strPropertyName, out dateValue))
            {
                objResult = dateValue;
                return true;
            }
            double dblValue;
            if (TryGetDblValue(strPropertyName, out dblValue))
            {
                objResult = dblValue;
                return true;
            }
            int intValue;
            if (TryGetIntValue(strPropertyName, out intValue))
            {
                objResult = intValue;
                return true;
            }
            long lngValue;
            if (TryGetLngValue(strPropertyName, out lngValue))
            {
                objResult = lngValue;
                return true;
            }
            object objValue;
            if (TryGetObjValue(strPropertyName, out objValue))
            {
                objResult = objValue;
                return true;
            }
            string strValue;
            if (TryGetStrValue(strPropertyName, out strValue))
            {
                objResult = strValue;
                return true;
            }
            return false;
        }

        public object GetHardPropertyValue(string strPropertyName)
        {
            ValidateReflector();
            return m_reflector.GetPropertyValue(this, strPropertyName);
        }

        private void ValidateReflector()
        {
            if (m_reflector == null)
            {
                //
                // the binder will stay null, unless this class has been compiled
                //
                if (!string.IsNullOrEmpty(m_strClassName))
                {
                    string strTypeName = GetType().Name;
                    if (strTypeName.Equals(m_strClassName) ||
                        (!strTypeName.Equals(typeof(SelfDescribingClass).Name) &&
                        !strTypeName.Equals(typeof(ASelfDescribingClass).Name)))
                    {
                        m_reflector = ReflectorCache.GetReflector(GetType());
                    }
                }
            }
        }

        public bool TryGetStrValue(string strPropertyName, out string strValue)
        {
            lock (m_strValues)
            {
                if (ContainsHardProperty(strPropertyName))
                {
                    strValue = GetHardStrValue(strPropertyName);
                    return true;
                }
                if (m_strValues.TryGetValue(
                    strPropertyName,
                    out strValue))
                {
                    return true;
                }
            }
            return false;
        }

        public bool TryRemoveStrValue(string strPropertyName, out string strValue)
        {
            lock (m_strValues)
            {
                if (ContainsHardProperty(strPropertyName))
                {
                    strValue = GetHardStrValue(strPropertyName);
                    return true;
                }
                if (m_strValues.TryGetValue(
                    strPropertyName,
                    out strValue))
                {
                    m_strValues.Remove(strPropertyName);
                    return true;
                }
            }
            return false;
        }

        public bool TryGetDblValue(Enum enumValue, out double dblValue)
        {
            return TryGetDblValue(enumValue.ToString(), out dblValue);
        }

        public bool TryGetDblValue(string strPropertyName, out double dblValue)
        {
            lock (m_dblValues)
            {
                if (ContainsHardProperty(strPropertyName))
                {
                    dblValue = GetHardDblValue(strPropertyName);
                    return true;
                }
                if (m_dblValues.TryGetValue(
                    strPropertyName,
                    out dblValue))
                {
                    return true;
                }
            }
            dblValue = 0;
            return false;
        }

        public bool TryGetObjValue(Enum enumValue, out object obj)
        {
            return TryGetObjValue(enumValue.ToString(), out obj);
        }

        public bool TryGetDateValue(Enum enumValue, out DateTime dateValue)
        {
            return TryGetDateValue(enumValue.ToString(), out dateValue);
        }

        public bool TryGetLngValue(Enum enumValue, out long lngValue)
        {
            return TryGetLngValue(enumValue.ToString(), out lngValue);
        }

        public bool TryGetBlnValue(Enum enumValue, out bool blnValue)
        {
            return TryGetBlnValue(enumValue.ToString(), out blnValue);
        }

        public bool TryGetStrValue(Enum enumValue, out string strValue)
        {
            return TryGetStrValue(enumValue.ToString(), out strValue);
        }

        public bool TryGetIntValue(Enum enumValue, out int intValue)
        {
            return TryGetIntValue(enumValue.ToString(), out intValue);
        }

        public bool TryGetIntValue(string strPropertyName, out int intValue)
        {
            lock (m_intValues)
            {
                if (ContainsHardProperty(strPropertyName))
                {
                    intValue = GetHardIntValue(strPropertyName);
                    return true;
                }
                if (m_intValues.TryGetValue(
                    strPropertyName,
                    out intValue))
                {
                    return true;
                }
            }
            intValue = 0;
            return false;
        }

        public bool TryGetBlnValue(string strPropertyName, out bool blnValue)
        {
            lock (m_blnValues)
            {
                if (ContainsHardProperty(strPropertyName))
                {
                    blnValue = GetHardBlnValue(strPropertyName);
                    return true;
                }
                if (m_blnValues.TryGetValue(
                    strPropertyName,
                    out blnValue))
                {
                    return true;
                }
            }
            return false;
        }

        public bool TryGetLngValue(string strPropertyName, out long lngValue)
        {
            lock (m_lngValues)
            {
                if (ContainsHardProperty(strPropertyName))
                {
                    lngValue = GetHardLngValue(strPropertyName);
                    return true;
                }
                if (m_lngValues.TryGetValue(
                    strPropertyName,
                    out lngValue))
                {
                    return true;
                }
            }
            lngValue = 0;
            return false;
        }

        public bool TryGetDateValue(string strPropertyName, out DateTime dateTime)
        {
            lock (m_dateValues)
            {
                if (ContainsHardProperty(strPropertyName))
                {
                    dateTime = GetHardDateValue(strPropertyName);
                    return true;
                }
                if (m_dateValues.TryGetValue(
                    strPropertyName,
                    out dateTime))
                {
                    return true;
                }
            }
            dateTime = new DateTime();
            return false;
        }

        public bool TryGetObjValue(string strPropertyName, out object objValue)
        {
            lock (m_objValues)
            {
                if (ContainsHardProperty(strPropertyName))
                {
                    objValue = GetHardObjValue(strPropertyName);
                    return true;
                }
                if (m_objValues.TryGetValue(
                    strPropertyName,
                    out objValue))
                {
                    return true;
                }
            }
            return false;
        }

        public object GetPropertyValueByType(Type propertyType, string strPropertyName)
        {
            if (propertyType == typeof(int))
            {
                return GetIntValue(strPropertyName);
            }
            if (propertyType == typeof(double))
            {
                return GetDblValue(strPropertyName);
            }
            if (propertyType == typeof(long))
            {
                return GetLngValue(strPropertyName);
            }
            if (propertyType == typeof(bool))
            {
                return GetBlnValue(strPropertyName);
            }
            if (propertyType == typeof(string))
            {
                return GetStrValue(strPropertyName);
            }
            if (propertyType == typeof(object))
            {
                return GetObjValue(strPropertyName);
            }
            throw new NotImplementedException();
        }

        public void SetValueToDictByType(string strPropertyName, object objProperty)
        {
            Type propertyType = objProperty.GetType();
            if (propertyType == typeof(int))
            {
                SetIntValue(strPropertyName, (int)objProperty);
            }
            else if (propertyType == typeof(double))
            {
                SetDblValue(strPropertyName, (double)objProperty);
            }
            else if (propertyType == typeof(long))
            {
                SetLngValue(strPropertyName, (long)objProperty);
            }
            else if (propertyType == typeof(bool))
            {
                SetBlnValue(strPropertyName, (bool)objProperty);
            }
            else if (propertyType == typeof(string))
            {
                SetStrValue(strPropertyName, objProperty.ToString());
            }
            else if (propertyType == typeof(object))
            {
                SetObjValueToDict(strPropertyName, objProperty);
            }
            else if (propertyType == typeof(DateTime))
            {
                SetDateValue(strPropertyName, (DateTime)objProperty);
            }
            else
            {
                SetObjValueToDict(strPropertyName, objProperty);
            }
        }

        public void ResetMaps()
        {
            m_blnValues.Clear();
            m_dateValues.Clear();
            m_dblValues.Clear();
            m_intValues.Clear();
            m_lngValues.Clear();
            m_strValues.Clear();
            m_objValues.Clear();
        }

        #region IDisposable Members

        //public void Dispose()
        //{
        //    ResetMaps();
        //}

        #endregion

        public string ToScrappedString()
        {
            return ToCsvString("|", true);
        }
    }
}


