#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HC.Core.DynamicCompilation;
using HC.Core.Helpers;
using HC.Core.Io.Serialization;
using HC.Core.Io.Serialization.DataStructures;
using HC.Core.Io.Serialization.Interfaces;
using HC.Core.Io.Serialization.Parsers;
using HC.Core.Io.Serialization.Readers;
using HC.Core.Io.Serialization.Types;
using HC.Core.Io.Serialization.Writers;
using HC.Core.Reflection;

#endregion

namespace HC.Core.Io.KnownObjects.KnownTypes
{
    public class KnownTypesSerializer
    {
        private static bool IsReferenceType(Type n)
        {
            return !n.IsAbstract &&
                   !n.IsInterface &&
                   !n.IsValueType &&
                   n != typeof (string);
        }

        public static void AddSerializers()
        {
            List<Type> knownTypes = (from n in KnownTypesCache.AllKnownTypes.Values
                              where !n.IsAbstract && !n.IsInterface select n).ToList();
            knownTypes.Add(typeof(object));
            knownTypes.Add(typeof(string));
            knownTypes.Add(typeof(String));
            knownTypes.Add(typeof(double));
            knownTypes.Add(typeof(int));
            knownTypes.Add(typeof(DateTime));
            knownTypes.Add(typeof(long));
            knownTypes.Add(typeof(bool));
            knownTypes.Add(typeof(byte));
            //knownTypes.AddRange(from n in foundTypes
            //                    where IsReferenceType(n)
            //                    select n);
            knownTypes = knownTypes.Distinct().ToList();
            knownTypes.AddRange(from n in KnownTypesCache.AllKnownTypes.Values
                select typeof(List<>).MakeGenericType(n));
            knownTypes = knownTypes.Distinct().ToList();
            List<Type> foundTypes = FindTypes(knownTypes);

            string strObjName = typeof(KnownTypesSerializer).Name;
            var classFactory = new SelfDescribingClassFactory(
                strObjName,
                typeof(SerializerCache).Namespace);

            foreach (Type foundType in foundTypes)
            {
                classFactory.AddUsingStatement(foundType.Namespace);
                classFactory.AddReferencedAssembly(foundType);
            }
            Type serializerType = typeof(IDynamicSerializable);
            Type readerType = typeof(ISerializerReader);
            DateTime logTime = DateTime.Now;
            Console.WriteLine("Parsing serializer types...");
            var typeNames = new Dictionary<string, string>();
            foreach (Type knownType in knownTypes)
            {
                string strReflectedName = ReflectionHelper.GetTypeNameRecursive(knownType);
                string strTypeName = typeof(SerializerCache).Name + "_" + strReflectedName;
                var currClassFactory = new SelfDescribingClassFactory(
                    strTypeName,
                    typeof(SerializerCache).Namespace);
                currClassFactory.AddInterface(serializerType.Name);
                SerializerParserHelper.Parse(knownType, currClassFactory, readerType);
                string strParsedClass = currClassFactory.ParseClassWithoutUsiung();
                classFactory.AddClass(strParsedClass);
                typeNames[strTypeName] = strReflectedName;
            }
            classFactory.CreateInstance();
            Console.WriteLine("Finish parsing serializer types. Time (secs) = " +
                (DateTime.Now - logTime).TotalSeconds);

            logTime = DateTime.Now;
            Console.WriteLine("Reflecting types...");
            foreach (var kvp in typeNames)
            {
                string strTypeName = kvp.Key;
                Type type = classFactory.GetType(strTypeName);
                if (type != null)
                {
                    var reflector = ReflectorCache.GetReflector(type);
                    SerializerCache.AddToSerializeMap(
                        (IDynamicSerializable)reflector.CreateInstance(),
                        kvp.Value);
                }
            }
            Console.WriteLine("Finish reflecting types. Time (secs) = " +
                (DateTime.Now - logTime).TotalSeconds);
        }

        private static List<Type> FindTypes(List<Type> knownTypes)
        {
            var logTime = DateTime.Now;
            Console.WriteLine("Finding all types...");
            var allFoundTypes = (from n in
                                     knownTypes
                                 select ComplexTypeFinder.FindTypes(n)).ToList();

            var foundTypes = new List<Type>(knownTypes)
                                 {
                                     typeof (IDynamicSerializable),
                                     typeof (KnownTypesCache),
                                     typeof (ISerializerWriter),
                                     typeof (ISerializerReader),
                                     typeof (ListSerializer),
                                     typeof (IList),
                                 };

            foreach (List<Type> typeList in allFoundTypes)
            {
                foundTypes.AddRange(typeList);
            }
            foundTypes = foundTypes.Distinct().ToList();

            Console.WriteLine("Finish finding all types. Time (sec) = " +
                (DateTime.Now - logTime).TotalSeconds);
            return foundTypes;
        }
    }
}



