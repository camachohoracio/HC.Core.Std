#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HC.Core.Exceptions;
using HC.Core.Logging;

#endregion

namespace HC.Core.Io.KnownObjects.KnownTypes
{
    public static class KnownTypesCache
    {
        #region Properties

        public static Dictionary<string, KnownTypeLookup> KnownTypes { get; private set; }
        public static Dictionary<string, Type> AllKnownTypes { get; private set; }
        public static Dictionary<string, Type> Allypes { get; private set; }

        #endregion

        #region Members

        private static readonly object m_lockObject = new object();
        private static readonly Dictionary<int, Type> m_mapIndexToType;

        #endregion

        #region Constructors

        static KnownTypesCache()
        {
            m_mapIndexToType = new Dictionary<int, Type>();
            AllKnownTypes = new Dictionary<string, Type>();
            Allypes = new Dictionary<string, Type>();
        }

        #endregion

        #region Public

        public static void LoadKnownTypes()
        {
            try
            {
                if (KnownTypes != null)
                {
                    return;
                }

                lock (m_lockObject)
                {
                    if (KnownTypes != null)
                    {
                        return;
                    }

                    //
                    // add primitive types
                    //
                    var knownTypes = new Dictionary<string, KnownTypeLookup>();
                    AddPrimitiveTypes(knownTypes);

                    //
                    // get known types from known assemblies
                    //
                    AddKnownTypesFromKnownAssemblies(knownTypes);

                    GetIdLookup(knownTypes);

                    Logger.Log("Loaded " + knownTypes.Count + "known types: " +
                               string.Join(",", from n in knownTypes.Keys select n));

                    KnownTypes = knownTypes;
                    KnownTypesSerializer.AddSerializers();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public static Type GetTypeFromId(int intIndex)
        {
            Type type;
            m_mapIndexToType.TryGetValue(intIndex, out type);

            if(type == null)
            {
                throw new HCException("Null type from id " + intIndex);
            }
            return type;
        }

        public static int GetTypeId(Type type)
        {
            KnownTypeLookup knownTypeLookup;
            if(KnownTypes.TryGetValue(type.Name, out knownTypeLookup))
            {

                return knownTypeLookup.TypeId;
            }
            return -1;
        }

        #endregion

        #region Private

        private static void GetIdLookup(Dictionary<string, KnownTypeLookup> knownTypes)
        {
            try
            {
                foreach (KnownTypeLookup typeLookup in knownTypes.Values)
                {
                    if (typeLookup.Type == null)
                    {
                        continue;
                    }

                    if (m_mapIndexToType.ContainsKey(typeLookup.TypeId))
                    {
                        throw new HCException("Type id already in list");
                    }

                    m_mapIndexToType[typeLookup.TypeId] = typeLookup.Type;
                }
                Logger.Log("Loaded [" + 
                    m_mapIndexToType.Count + "] non null types");
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void AddKnownTypesFromKnownAssemblies(
            Dictionary<string, KnownTypeLookup> knownTypes)
        {
            List<Type> typeList = FindTypesFromKnownAssemblies().Distinct().ToList();
            foreach (Type type in typeList)
            {
                try
                {
                    AllKnownTypes[type.Name] = type;
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
            }
            List<string> knownTypesStrList = Config.GetKnownTypes();
            foreach (string strKnownType in knownTypesStrList)
            {
                Type currType;
                if (AllKnownTypes.TryGetValue(strKnownType, out currType))
                {
                    AddKnownType(currType, knownTypes);
                }
                else if (!knownTypes.ContainsKey(strKnownType))
                {
                    Logger.Log("Warning. Type not found: " + strKnownType);
                    AddKnownType(null, knownTypes, strKnownType);
                }
            }
        }

        private static void AddPrimitiveTypes(
            Dictionary<string, KnownTypeLookup> knownTypes)
        {
            AddKnownType(typeof(Object), knownTypes);
            AddKnownType(typeof(string), knownTypes);
            AddKnownType(typeof (double), knownTypes);
            AddKnownType(typeof (DateTime), knownTypes);
            AddKnownType(typeof (int), knownTypes);
            AddKnownType(typeof (bool), knownTypes);
            AddKnownType(typeof (byte), knownTypes);
            AddKnownType(typeof (float), knownTypes);
            AddKnownType(typeof (long), knownTypes);
            AddKnownType(typeof (short), knownTypes);
            AddKnownType(typeof (IList), knownTypes);
            AddKnownType(typeof (IDictionary), knownTypes);
            AddKnownType(typeof (Array), knownTypes);
        }

        private static List<Type> FindTypesFromKnownAssemblies()
        {
            try
            {
                List<Assembly> loadedAssemblies =
                    AssemblyCache.GetLoadedAssemblies(Assembly.GetCallingAssembly());
                List<Type> baseTypes = GetBaseTypes(loadedAssemblies);

                var selectedTypes = new List<Type>();
                foreach (Assembly assembly in loadedAssemblies)
                {
                    try
                    {
                        foreach (Type currType in assembly.GetTypes())
                        {
                            try
                            {
                                foreach (Type baseType in baseTypes)
                                {
                                    try
                                    {
                                        if (baseType.IsAssignableFrom(currType) &&
                                            !selectedTypes.Contains(currType))
                                        {
                                            selectedTypes.Add(currType);
                                        }
                                        Allypes[currType.Name] = currType;
                                    }
                                    catch(Exception ex)
                                    {
                                        Console.WriteLine(ex);
                                    }
                                }
                            }
                            catch(Exception ex)
                            {
                                Console.WriteLine(ex);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
                return selectedTypes;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return new List<Type>();
        }

        private static void AddKnownType(
            Type type, 
            Dictionary<string, KnownTypeLookup> typeMap)
        {
            string strTypeName = type.Name;
            AddKnownType(type, typeMap, strTypeName);
        }

        private static void AddKnownType(
            Type type, 
            Dictionary<string, KnownTypeLookup> typeMap, 
            string strTypeName)
        {
            if (!typeMap.ContainsKey(strTypeName) &&
                !string.IsNullOrEmpty(strTypeName))
            {
                int intIndex = typeMap.Count;
                //if (intIndex == 68)
                //{
                //    Debugger.Break();
                //}

                var typeLookup = new KnownTypeLookup
                                     {
                                         Type = type,
                                         TypeId = intIndex
                                     };
                typeMap[strTypeName] = typeLookup;
            }
        }

        private static List<Type> GetBaseTypes(
            List<Assembly> loadedAssemblies)
        {
            var baseTypes = new List<Type>();
            foreach (Assembly assembly in loadedAssemblies)
            {
                try
                {
                    Type[] types = assembly.GetTypes();
                    for (int i = 0; i < types.Length; i++)
                    {
                        Type currType = types[i];
                        if (currType.GetCustomAttributes(typeof (IsAKnownTypeAttr), true).Length > 0)
                        {
                            baseTypes.Add(currType);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
            }
            baseTypes = baseTypes.Distinct().ToList();
            return baseTypes;
        }

        #endregion
    }
}


