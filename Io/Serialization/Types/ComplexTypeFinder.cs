#region

using System;
using System.Collections;
using System.Collections.Generic;
using HC.Core.Logging;
using HC.Core.Reflection;

#endregion

namespace HC.Core.Io.Serialization.Types
{
    public static class ComplexTypeFinder
    {
        #region Public

        public static List<Type> FindTypes(
            Type currType)
        {
            var types = new List<Type>();
            FindTypes(
                currType,
                types);
            return types;
        }

        #endregion

        #region Private

        private static void FindTypes(
            Type currType,
            List<Type> types)
        {
            try
            {
                if (typeof (IList).IsAssignableFrom(currType) &&
                    !currType.IsArray)
                {
                    FindTypesFromList(currType, types);
                }
                if (typeof (IDictionary).IsAssignableFrom(currType))
                {
                    FindTypesFromDictionary(currType, types);
                }
                List<Type> props = 
                    ReflectorCache.GetReflector(currType).GetPropertyTypes();
                foreach (Type type in props)
                {
                    try
                    {
                        if (type == null)
                        {
                            continue;
                        }

                        if (!types.Contains(type))
                        {
                            //
                            // add here to avoid circular calls, stack oveflow
                            //
                            types.Add(type);
                            FindTypes(type, types);
                        }
                    }
                    catch(Exception ex)
                    {
                        Logger.Log(ex);
                    }
                }
                if (!types.Contains(currType))
                {
                    types.Add(currType);
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void FindTypesFromList(
            Type currType, 
            List<Type> types)
        {
            Type elementType = currType.GetGenericArguments()[0];
            
            if (!types.Contains(elementType))
            {
                FindTypes(elementType, types);
            }
        }

        private static void FindTypesFromDictionary(
            Type currType, 
            List<Type> types)
        {
            Type[] genericTypes = currType.GetGenericArguments();
            Type keyType = genericTypes[0];
            Type valueType = genericTypes[1];

            if (!types.Contains(keyType))
            {
                FindTypes(keyType, types);
            }
            if (!types.Contains(valueType))
            {
                FindTypes(valueType, types);
            }
        }

        #endregion
    }
}



