#region

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using HC.Core.DynamicCompilation;
using HC.Core.Helpers;
using HC.Core.Io.KnownObjects;
using HC.Core.Io.KnownObjects.KnownTypes;
using HC.Core.Io.Serialization.DataStructures;
using HC.Core.Io.Serialization.Interfaces;
using HC.Core.Io.Serialization.Parsers;
using HC.Core.Io.Serialization.Readers;
using HC.Core.Io.Serialization.Types;
using HC.Core.Io.Serialization.Writers;
using HC.Core.Logging;
using HC.Core.Reflection;
using HC.Core.Threading;

#endregion

namespace HC.Core.Io.Serialization
{
    public static class SerializerCache
    {
        #region Members

        private static readonly ConcurrentDictionary<string, IDynamicSerializable> m_serializersMap;

        #endregion

        #region Constructor

        static SerializerCache()
        {
            m_serializersMap = new ConcurrentDictionary<string, IDynamicSerializable>();
        }

        #endregion

        public static IDynamicSerializable GetSerializer(
            Type objType)
        {
            IDynamicSerializable serializer;
            if (m_serializersMap.TryGetValue(
                objType.Name,
                out serializer))
            {
                return serializer;
            }

            lock (LockObjectHelper.GetLockObject(objType.Name + typeof(SerializerCache).Name))
            {
                string strObjTypeName = ReflectionHelper.GetTypeNameRecursive(objType);
                if (m_serializersMap.TryGetValue(
                    strObjTypeName,
                    out serializer))
                {
                    return serializer;
                }

                var logTime = DateTime.Now;
                Type serializerType = typeof(ISerializerReader);
                SelfDescribingClassFactory classFactory =
                    GetSerializerClassFactory(
                        serializerType,
                        objType);
                //classFactory.AddReferencedAssembly(objType);
                //classFactory.AddUsingStatement(objType);
                SerializerParserHelper.Parse(objType, classFactory, serializerType);

                serializer = (IDynamicSerializable)classFactory.CreateInstance();
                AddToSerializeMap(serializer, strObjTypeName);

                string strMessage = "Loaded serializer for type: " + strObjTypeName +
                                    ". Time (secs) = " + (DateTime.Now - logTime).TotalSeconds;
                Logger.Log(strMessage);
                Console.WriteLine(strMessage);
                return serializer;
            }
        }

        public static void AddToSerializeMap(
            IDynamicSerializable serializer, 
            string strObjType)
        {
            m_serializersMap[strObjType] =
                serializer;
        }

        public static SelfDescribingClassFactory GetSerializerClassFactory(
            Type serializerType,
            Type reflectedType)
        {
            try
            {
                string strObjName = ReflectionHelper.GetTypeNameRecursive(reflectedType);
                SelfDescribingClassFactory classFactory = GetSerializerClassFactory(
                    serializerType,
                    strObjName);
                classFactory.AddUsingStatement(reflectedType.Namespace);
                classFactory.AddUsingStatement(typeof (KnownTypesCache));
                classFactory.AddReferencedAssembly(reflectedType);

                if (AssemblyCache.LoadedAssemblies != null)
                {
                    foreach (Assembly assembly in AssemblyCache.LoadedAssemblies)
                    {
                        classFactory.AddReferencedAssembly(assembly);
                    }
                }

                List<Type> types = ComplexTypeFinder.FindTypes(reflectedType);
                foreach (var type in types)
                {
                    classFactory.AddReferencedAssembly(type);
                    classFactory.AddUsingStatement(type);
                }
                classFactory.AddUsingStatement(typeof (ISerializerWriter));
                classFactory.AddUsingStatement(typeof (ISerializerReader));
                classFactory.AddUsingStatement(typeof (ListSerializer));

                return classFactory;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        private static SelfDescribingClassFactory GetSerializerClassFactory(
            Type serializerType,
            string strReflectedType)
        {
            var selfDescribingClassFactory = new SelfDescribingClassFactory(
                typeof(SerializerCache).Name + "_" + strReflectedType,
                typeof(SerializerCache).Namespace);

            Type helperType = typeof(IDynamicSerializable);

            //
            // add unsings
            //
            selfDescribingClassFactory.AddUsingStatement(helperType.Namespace);
            selfDescribingClassFactory.AddUsingStatement(
                typeof(SerializerCache).Namespace);
            selfDescribingClassFactory.AddUsingStatement(
                typeof(ReflectorCache).Namespace);
            selfDescribingClassFactory.AddUsingStatement(
                typeof(IList).Namespace);

            if (KnownTypesCache.KnownTypes != null)
            {
                foreach (var typeLookup in KnownTypesCache.KnownTypes.Values)
                {
                    Type knowType = typeLookup.Type;

                    if (knowType == null)
                    {
                        continue;
                    }
                    selfDescribingClassFactory.AddUsingStatement(
                        knowType.Namespace);
                    selfDescribingClassFactory.AddReferencedAssembly(knowType);
                }
            }
            //
            // add assemblies
            //
            selfDescribingClassFactory.AddReferencedAssembly(serializerType);
            selfDescribingClassFactory.AddInterface(helperType.Name);
            return selfDescribingClassFactory;
        }
    }
}



