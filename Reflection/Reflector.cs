#region

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using HC.Core.DynamicCompilation;
using HC.Core.DynamicCompilation.ExpressionClasses;
using HC.Core.Exceptions;
using HC.Core.Logging;

#endregion

namespace HC.Core.Reflection
{
    public class Reflector<T> : IReflector
    {
        #region Members

        private readonly object m_constructorLockObject = new object();
        private readonly object m_extractorLockObject = new object();
        private readonly object m_getSetLockObject = new object();
        private readonly object m_namesLockObject = new object();
        private readonly object m_propertyLockObject = new object();
        private readonly Type m_type;
        private CreateInstanceHelper<T> m_createInstanceHelper;
        private Action<T, Action<string, object>> m_extractor;
        private readonly bool m_blnIsPrimitiveType;
        private readonly bool m_blIsSelfDescribingClass;
        private Dictionary<string, Func<T, object>> m_getters;
        private Func<T, object>[] m_getterArr;
        private Dictionary<string, Action<T, object>> m_setters;
        private Action<T, object>[] m_settArr;
        private Dictionary<string, PropertyInfo> m_propertyLookup;
        private PropertyInfo[] m_propertyLookupArr;
        private List<string> m_propertyNames;

        #region Read-write members

        private Func<T, object>[] m_readWriteGetterArr;
        private Action<T, object>[] m_readWriteSetterArr;
        private PropertyInfo[] m_readWritePropertyLookupArr;
        private List<string> m_readWritePropertyNames;
        private List<Type> m_propertyTypes;
        private List<Type> m_readWritePropertyTypes;

        #endregion

        #endregion

        #region Constructor

        public Reflector()
        {
            try
            {
                m_type = typeof(T);
                if (typeof(T).IsValueType ||
                    typeof(T) == typeof(string))
                {
                    m_blnIsPrimitiveType = true;
                }
                else if (typeof(ASelfDescribingClass).IsAssignableFrom(typeof(T)))
                {
                    m_blIsSelfDescribingClass = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        #endregion

        #region Public

        public bool ContainsProperty(string strPropertyName)
        {
            try
            {
                ValidateProperties();
                return m_propertyLookup.ContainsKey(strPropertyName);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return false;
        }

        public string ToStringObject(
            Object obj,
            string strDelim)
        {
            try
            {
                if (obj == null)
                {
                    return string.Empty;
                }
                List<string> props = GetPropertyNames();
                if (props == null || props.Count == 0)
                {
                    return string.Empty;
                }
                string strCurrVal = GetStrVal(obj, props[0]);
                var sb = new StringBuilder(strCurrVal);
                for (int i = 1; i < props.Count; i++)
                {
                    strCurrVal = GetStrVal(obj, props[i]);
                    sb.Append(strDelim).Append(strCurrVal);
                }
                return sb.ToString();
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return string.Empty;
        }

        private string GetStrVal(object obj, string strProp)
        {
            try
            {
                object objCurrVal = GetPropertyValue(
                    obj,
                    strProp);
                string strCurrVal = objCurrVal == null ? string.Empty : objCurrVal.ToString();
                return strCurrVal;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return string.Empty;
        }

        public List<Type> GetReadWritePropertyTypes()
        {
            try
            {
                if (m_readWritePropertyTypes != null)
                {
                    return m_readWritePropertyTypes;
                }
                var propertyTypes = new List<Type>();
                List<string> propNames = GetReadWritePropertyNames();
                for (int i = 0; i < propNames.Count; i++)
                {
                    string strPropertyName = propNames[i];
                    propertyTypes.Add(
                        GetPropertyType(strPropertyName));
                }

                m_readWritePropertyTypes = propertyTypes;
                return m_readWritePropertyTypes;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return new List<Type>();
        }

        public bool TryGetPropertyType(
            string strPropertyName,
            out Type type)
        {
            type = null;

            try
            {
                ValidateProperties();
                PropertyInfo propInfo;
                if (m_propertyLookup.TryGetValue(strPropertyName, out propInfo))
                {
                    type = propInfo.PropertyType;
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return false;
        }

        public Type GetPropertyType(int intIndex)
        {
            try
            {
                ValidateProperties();
                return m_propertyLookupArr[intIndex].PropertyType;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public Type GetReadWritePropertyType(int intIndex)
        {
            try
            {
                List<Type> readWritePropertyTypes = GetReadWritePropertyTypes();
                return readWritePropertyTypes[intIndex];
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public void SetReadWritePropertyValue(
            object obj,
            int intIndex,
            object propetyValue)
        {
            try
            {
                SetReadWriteProperty(obj,
                    intIndex,
                    propetyValue);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public Type GetPropertyType(string strPropertyName)
        {
            try
            {
                ValidateProperties();
                PropertyInfo propInfo;
                if (!m_propertyLookup.TryGetValue(strPropertyName, out propInfo))
                {
                    throw new HCException("Property not found [" +
                        strPropertyName + "]");
                }
                return propInfo.PropertyType;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public List<Type> GetPropertyTypes()
        {
            try
            {
                if (m_propertyTypes != null)
                {
                    return m_propertyTypes;
                }
                var propertyTypes = new List<Type>();
                List<string> propNames = GetPropertyNames();
                for (int i = 0; i < propNames.Count; i++)
                {
                    string strPropertyName = propNames[i];
                    propertyTypes.Add(GetPropertyType(strPropertyName));
                }

                m_propertyTypes = propertyTypes;
                return m_propertyTypes;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return new List<Type>();
        }

        public K CreateInstanceFromDr<K>(DataRow dr)
        {
            try
            {
                object obj = CreateInstance();
                foreach (string strPropertyName in GetPropertyNames())
                {
                    if (CanWriteProperty(strPropertyName))
                    {
                        SetPropertyValue(obj, strPropertyName, dr[strPropertyName]);
                    }
                }
                return (K)obj;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return default(K);
        }

        public object GetReadWritePropertyValue(object item, int i)
        {
            try
            {
                return GetReadWriteProperty((T)item, i);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public List<string> GetPropertyNames()
        {
            try
            {
                ValidatePropertyNames();
                return m_propertyNames.ToList();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return new List<string>();
        }

        public List<string> GetReadWritePropertyNames()
        {
            try
            {
                if (m_readWritePropertyNames != null)
                {
                    return m_readWritePropertyNames;
                }

                List<string> propertyNames = GetPropertyNames();
                var readWritePropertyNames = new List<string>();
                for (int i = 0; i < propertyNames.Count; i++)
                {
                    string strProp = propertyNames[i];
                    if (CanWriteProperty(strProp))
                    {
                        readWritePropertyNames.Add(strProp);
                    }
                }
                m_readWritePropertyNames = readWritePropertyNames;
                return m_readWritePropertyNames;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return new List<string>();
        }

        public bool IsPropertyValueType(string strPropertyName)
        {
            try
            {
                ValidateProperties();
                return m_propertyLookup[strPropertyName].PropertyType.IsValueType;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return false;
        }

        public PropertyInfo GetPropertyInfo(string strPropertyName)
        {
            try
            {
                ValidateProperties();
                return m_propertyLookup[strPropertyName];
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public PropertyInfo GetPropertyInfo(int intProp)
        {
            try
            {
                ValidateProperties();
                return m_propertyLookupArr[intProp];
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public PropertyInfo GetReadWritePropertyInfo(int intIndex)
        {
            try
            {
                ValidateProperties();
                return m_propertyLookupArr[intIndex];
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public void SetPropertyValue(
            T obj,
            int intPropertyIndex,
            object propetyValue)
        {
            try
            {
                SetProperty(obj,
                    intPropertyIndex,
                    propetyValue);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public void SetPropertyValue(
            object obj,
            int intPropertyIndex,
            object propetyValue)
        {
            try
            {
                SetProperty(obj, intPropertyIndex, propetyValue);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public void SetPropertyValue(
            object obj,
            string strPropertyName,
            object propetyValue)
        {
            try
            {
                SetProperty(obj, strPropertyName, propetyValue);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public object CreateInstance()
        {
            try
            {
                if (m_blnIsPrimitiveType)
                {
                    return default(T);
                }
                ValidateDefaultConstructor();
                T instance = m_createInstanceHelper.CreateInstance(null, null);

                if (m_blIsSelfDescribingClass)
                {
                    var inst = instance as ASelfDescribingClass;
                    if (inst != null)
                    {
                        inst.SetClassName(typeof(T).Name);
                    }
                }

                return instance;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public object GetPropertyValue(object item, int i)
        {
            try
            {
                return GetProperty((T)item, i);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public object GetPropertyValue(object item, string strPropertyName)
        {
            try
            {
                return GetProperty((T)item, strPropertyName);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        private void ValidatePropertyNames()
        {
            try
            {
                if (m_propertyNames == null)
                {
                    lock (m_namesLockObject)
                    {
                        if (m_propertyNames == null)
                        {
                            ValidateProperties();
                            var propertyNames = new List<string>();
                            foreach (string strPropertyName in m_propertyLookup.Keys)
                            {
                                if (propertyNames.Contains(strPropertyName))
                                {
                                    throw new HCException("Property already added");
                                }
                                propertyNames.Add(strPropertyName);
                            }
                            m_propertyNames = propertyNames;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public List<Type> GetTypes()
        {
            try
            {
                var t = new List<Type>();
                t.AddRange(TypeExtractor<T>.Types);
                return t;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return new List<Type>();
        }

        public void Store(T instance, IList<object> l)
        {
            try
            {
                ValidateExtractor();
                m_extractor(instance, (name, value) => l.Add(value));
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public bool CanWriteProperty(string strPropertyName)
        {
            try
            {

                ValidateProperties();
                PropertyInfo prop = m_propertyLookup[strPropertyName];

                return CanWriteProperty(prop);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return false;
        }

        private static bool CanWriteProperty(PropertyInfo prop)
        {
            try
            {
                return prop.CanWrite &&
                       prop.GetSetMethod() != null;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return false;
        }

        public void SetProperty(
            T toObject,
            int intPropertyIndex,
            object propValue)
        {
            try
            {
                ValidatePropertyGetAndSetters();
                m_settArr[intPropertyIndex](toObject, propValue);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public void SetReadWriteProperty(
            T toObject,
            int intPropertyIndex,
            object propValue)
        {
            try
            {
                ValidatePropertyGetAndSetters();
                m_readWriteSetterArr[intPropertyIndex](toObject, propValue);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public void SetReadWriteProperty(
            object toObject,
            int intPropertyIndex,
            object propValue)
        {
            try
            {
                SetReadWriteProperty(
                    (T)toObject,
                    intPropertyIndex,
                    propValue);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public void SetProperty(
            object toObject,
            int intPropertyIndex,
            object propValue)
        {
            try
            {
                SetProperty(
                    (T)toObject,
                    intPropertyIndex,
                    propValue);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public void SetProperty(
            object toObject,
            string strPropertyName,
            object propValue)
        {
            try
            {
                ValidatePropertyGetAndSetters();
                m_setters[strPropertyName]((T)toObject, propValue);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public object GetProperty(
            T tObject,
            string strPropertyName)
        {
            try
            {
                ValidatePropertyGetAndSetters();
                return m_getters[strPropertyName](tObject);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public object GetReadWriteProperty(
            T tObject,
            int i)
        {
            try
            {
                ValidatePropertyGetAndSetters();
                return m_readWriteGetterArr[i](tObject);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public object GetProperty(
            T tObject,
            int i)
        {
            try
            {
                ValidatePropertyGetAndSetters();
                return m_getterArr[i](tObject);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public T Load(
            List<object> objects,
            List<string> names)
        {
            try
            {
                ValidatePropertyGetAndSetters();
                var obj = (T)CreateInstance();
                for (var i = 0; i < names.Count; i++)
                {
                    m_setters[names[i]](obj, objects[i]);
                }
                return obj;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return default(T);
        }

        #endregion

        #region Private

        private void ValidateProperties()
        {
            try
            {
                if (m_propertyLookup == null)
                {
                    lock (m_propertyLockObject)
                    {
                        if (m_propertyLookup == null)
                        {
                            m_propertyLookup = new Dictionary<string, PropertyInfo>();
                            PropertyInfo[] propertyArr = m_type.GetProperties(BindingFlags.Public |
                                                                   BindingFlags.Instance);
                            var validProps = new List<PropertyInfo>();
                            var readWriteValidProps = new List<PropertyInfo>();
                            for (int i = 0; i < propertyArr.Length; i++)
                            {
                                PropertyInfo propertyInfo = propertyArr[i];
                                object[] attr =
                                    propertyInfo.GetCustomAttributes(
                                        typeof(XmlIgnoreAttribute), false);

                                if (attr.Length == 0)
                                {
                                    m_propertyLookup[propertyInfo.Name] = propertyInfo;
                                    validProps.Add(propertyInfo);
                                    if (CanWriteProperty(propertyInfo))
                                    {
                                        readWriteValidProps.Add(propertyInfo);
                                    }
                                }
                            }
                            m_propertyLookupArr = validProps.ToArray();
                            m_readWritePropertyLookupArr = readWriteValidProps.ToArray();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private void ValidateDefaultConstructor()
        {
            try
            {
                if (m_createInstanceHelper == null)
                {
                    lock (m_constructorLockObject)
                    {
                        if (m_createInstanceHelper == null)
                        {
                            m_createInstanceHelper = new CreateInstanceHelper<T>();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private void ValidateExtractor()
        {
            try
            {
                if (m_extractor == null)
                {
                    lock (m_extractorLockObject)
                    {
                        if (m_extractor == null)
                        {
                            m_extractor = TypeExtractor<T>.Generate(BindingTypes.Supported);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static Action<T, object> GetValueSetter<T>(PropertyInfo propertyInfo)
        {
            try
            {
                ParameterExpression instance = Expression.Parameter(propertyInfo.DeclaringType, "i");
                ParameterExpression argument = Expression.Parameter(typeof(object), "a");
                MethodCallExpression setterCall = Expression.Call(
                    instance,
                    propertyInfo.GetSetMethod(),
                    Expression.Convert(argument, propertyInfo.PropertyType));
                return (Action<T, object>)Expression.Lambda(setterCall,
                                                             instance,
                                                             argument).Compile();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }


        private static Func<T, object> GetValueGetter<T>(PropertyInfo propertyInfo)
        {
            try
            {
                //if (typeof(T) != propertyInfo.DeclaringType)
                //{
                //    throw new ArgumentException();
                //}

                var instance = Expression.Parameter(propertyInfo.DeclaringType, "i");
                var property = Expression.Property(instance, propertyInfo);
                var convert = Expression.TypeAs(property, typeof(object));
                return (Func<T, object>)Expression.Lambda(convert, instance).Compile();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        private void ValidatePropertyGetAndSetters()
        {
            try
            {
                if (m_setters == null)
                {
                    lock (m_getSetLockObject)
                    {
                        if (m_setters == null)
                        {
                            ValidateProperties();
                            var setters = new Dictionary<string, Action<T, object>>();
                            var getters = new Dictionary<string, Func<T, object>>();
                            var setterArr = new Action<T, object>[
                                m_propertyLookupArr.Length];
                            var getterArr = new Func<T, object>[
                                m_propertyLookupArr.Length];
                            var readWriteSetterArr = new Action<T, object>[
                                m_readWritePropertyLookupArr.Length];
                            var readWriteGetterArr = new Func<T, object>[
                                m_readWritePropertyLookupArr.Length];
                            int intReadWriteCounter = 0;

                            for (int i = 0; i < m_propertyLookupArr.Length; i++)
                            {
                                PropertyInfo propertyInfo = m_propertyLookupArr[i];
                                bool blnCanWriteProp = CanWriteProperty(propertyInfo);
                                bool blnCanReadProp = CanReadProperty(propertyInfo);

                                //
                                // add properties with public setters
                                //
                                if (blnCanReadProp)
                                {
                                    Func<T, object> getter = GetValueGetter<T>(
                                        propertyInfo);
                                    getters[propertyInfo.Name] = getter;
                                    getterArr[i] = getter;

                                    if (blnCanWriteProp)
                                    {
                                        readWriteGetterArr[intReadWriteCounter] = getter;
                                    }
                                }

                                if (blnCanReadProp && blnCanWriteProp)
                                {
                                    Action<T, object> setter = GetValueSetter<T>(
                                        propertyInfo);
                                    setters[propertyInfo.Name] = setter;
                                    setterArr[i] = setter;
                                    readWriteSetterArr[intReadWriteCounter] =
                                        setter;
                                    intReadWriteCounter++;
                                }
                            }

                            m_getters = getters;
                            m_setters = setters;
                            m_settArr = setterArr;
                            m_getterArr = getterArr;
                            m_readWriteGetterArr = readWriteGetterArr;
                            m_readWriteSetterArr = readWriteSetterArr;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static bool CanReadProperty(PropertyInfo propertyInfo)
        {
            try
            {
                return propertyInfo.GetGetMethod() != null &&
                       propertyInfo.GetGetMethod().IsPublic;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return false;
        }

        #endregion
    }
}


