#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HC.Core.DynamicCompilation;
using HC.Core.Exceptions;
using HC.Core.Io.KnownObjects.KnownTypes;
using HC.Core.Io.Serialization.Readers;
using HC.Core.Io.Serialization.Writers;
using HC.Core.Logging;
using HC.Core.Reflection;

#endregion

namespace HC.Core.Io.Serialization.Types
{
    public static class ComplexTypeSerializer
    {
        #region Public

        public static byte[] Serialize(
            Type type)
        {
            try
            {
                ISerializerWriter serializerWriter = Serializer.GetWriter();
                SerializeRecursive(type, serializerWriter);
                return serializerWriter.GetBytes();
            }
            catch(Exception ex)
            {
                Logging.Logger.Log(ex);
            }
            return null;
        }

        public static Type Deserialize(byte[] bytes)
        {
            try
            {
                if (bytes == null)
                {
                    return null;

                }
                return DeserializeRecursive(
                    Serializer.GetReader(bytes));
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public static Type ReadEnumType(ISerializerReader reader)
        {
            try
            {
                int intTypeId = reader.ReadInt32();
                if (intTypeId != -1)
                {
                    return KnownTypesCache.GetTypeFromId(intTypeId);
                }

                string strClass = reader.ReadString();
                string strAssembly = reader.ReadString();
                reader.ReadInt32();
                if (!string.IsNullOrEmpty(strClass))
                {
                    if (!string.IsNullOrEmpty(strAssembly))
                    {
                        Type calcType = Type.GetType(
                            strClass + "," +
                            strAssembly);

                        if (calcType == null)
                        {

                            KnownTypesCache.Allypes.TryGetValue(
                                strClass.Split('.').Last(),
                                out calcType);

                            if (calcType == null)
                            {
                                throw new HCException(
                                    "Null enum type [" +
                                    strClass + "]");
                            }
                        }
                        return calcType;
                    }
                }

                throw new HCException("Type not found");
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }

            return null;
        }

        #endregion

        #region Private

        private static void SerializeRecursive(
            Type type,
            ISerializerWriter serializerWriter)
        {
            int intArrRank = -1;
            if(type.IsArray &&
                (intArrRank = type.GetArrayRank()) == 1)
            {
                serializerWriter.Write(typeof(Array));
                var entryType = type.GetElementType();
                SerializeRecursive(entryType, serializerWriter);
            }
            else if(typeof(IList).IsAssignableFrom(type) &&
                intArrRank == -1)
            {
                serializerWriter.Write(typeof (IList));
                Type entryType = type.GetGenericArguments()[0];
                SerializeRecursive(entryType, serializerWriter);
            }
            else if (typeof(IDictionary).IsAssignableFrom(type))
            {
                if (type.GetGenericTypeDefinition() == typeof(SortedDictionary<,>))
                {
                    serializerWriter.Write(typeof(SortedDictionaryType));
                }
                else
                {
                    serializerWriter.Write(typeof (IDictionary));
                }

                var dictionary = (IDictionary)ReflectorCache.GetReflector(
                    type).CreateInstance();
                
                Type keyType = dictionary.Keys.AsQueryable().ElementType;
                Type valueType = dictionary.Values.AsQueryable().ElementType;
                SerializeRecursive(keyType, serializerWriter);
                SerializeRecursive(valueType, serializerWriter);
            }
            else
            {
                if (type == typeof(ASelfDescribingClass))
                {
                    serializerWriter.Write(typeof(SelfDescribingClass));
                    //
                    // true for the abstract class
                    //
                    serializerWriter.Write(true);
                }
                else if (type == typeof(SelfDescribingClass))
                {
                    serializerWriter.Write(typeof(SelfDescribingClass));
                    //
                    // true for the concrete class
                    //
                    serializerWriter.Write(false);
                }
                else
                {
                    serializerWriter.Write(type);
                }
            }
        }

        private static Type DeserializeRecursive(
            ISerializerReader serializerReader)
        {
            try
            {
                Type type = serializerReader.ReadType();

                int intArrRank = -1;
                if (type == typeof (Array) ||
                    (typeof (Array).IsAssignableFrom(type) &&
                     (intArrRank = type.GetArrayRank()) == 1))
                {
                    Type arrayType = DeserializeRecursive(serializerReader);
                    Array newArray = Array.CreateInstance(arrayType, 0);
                    return newArray.GetType();
                }
                if (typeof (IList).IsAssignableFrom(type) && intArrRank == -1)
                {
                    Type genericListType = typeof (List<>);
                    Type specificBinderType = DeserializeRecursive(serializerReader);
                    Type listType = genericListType.MakeGenericType(specificBinderType);
                    return listType;
                }
                if (typeof (IDictionary).IsAssignableFrom(type))
                {
                    Type genericListType = typeof (Dictionary<,>);
                    Type keyType = DeserializeRecursive(serializerReader);
                    Type valueType = DeserializeRecursive(serializerReader);
                    Type[] typeArgs = {keyType, valueType};
                    Type dictionaryType = genericListType.MakeGenericType(typeArgs);
                    return dictionaryType;
                }
                if (typeof (SortedDictionaryType) == type)
                {
                    Type genericListType = typeof (SortedDictionary<,>);
                    Type keyType = DeserializeRecursive(serializerReader);
                    Type valueType = DeserializeRecursive(serializerReader);
                    Type[] typeArgs = {keyType, valueType};
                    Type dictionaryType = genericListType.MakeGenericType(typeArgs);
                    return dictionaryType;
                }
                if (typeof (SelfDescribingClass) == type)
                {
                    bool blnIsAbstract = serializerReader.ReadBoolean();
                    if (blnIsAbstract)
                    {
                        return typeof (ASelfDescribingClass);
                    }
                }
                return type;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        #endregion
    }
}



