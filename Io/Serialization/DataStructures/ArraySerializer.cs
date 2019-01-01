#region

using System;
using System.Linq;
using HC.Core.Io.Serialization.Interfaces;
using HC.Core.Io.Serialization.Readers;
using HC.Core.Io.Serialization.Types;
using HC.Core.Io.Serialization.Writers;
using HC.Core.Logging;

#endregion

namespace HC.Core.Io.Serialization.DataStructures
{
    public static class ArraySerializer
    {
        public static void DeserializeArray(
            ISerializerReader serializer,
            Array array,
            int intListSize,
            Type arrayType)
        {
            try
            {
                if (intListSize <= 0 ||
                    array == null)
                {
                    return;
                }
                serializer.ReadByteArray();
                if (!arrayType.IsInterface &&
                    !arrayType.IsAbstract)
                {
                    if (arrayType == typeof (object))
                    {
                        DeserializeArrayOfObjects(serializer, array, intListSize);
                    }
                    else
                    {
                        DeserializeArrayOfConcreteTypes(
                            serializer,
                            array,
                            intListSize,
                            arrayType);
                    }
                }
                else
                {
                    DeserializeArrayOfAbstractInterface(serializer, array, intListSize);
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void DeserializeArrayOfAbstractInterface(
            ISerializerReader serializer,
            Array array,
            int intListSize)
        {
            try
            {
                for (int i = 0; i < intListSize; i++)
                {
                    Type itemType = serializer.ReadType();
                    IDynamicSerializable currDynamicSerializable = SerializerCache.GetSerializer(itemType);
                    array.SetValue(currDynamicSerializable.Deserialize(serializer), i);
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void DeserializeArrayOfConcreteTypes(
            ISerializerReader serializer, 
            Array array,
            int intListSize,
            Type arratType)
        {
            try
            {
                IDynamicSerializable dynamicSerializable = SerializerCache.GetSerializer(arratType);
                for (int i = 0; i < intListSize; i++)
                {
                    array.SetValue(dynamicSerializable.Deserialize(serializer), i);
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void DeserializeArrayOfObjects(
            ISerializerReader serializer,
            Array array,
            int intListSize)
        {
            try
            {
                for (int i = 0; i < intListSize; i++)
                {
                    byte[] typeByte = serializer.ReadByteArray();
                    Type arrayType = ComplexTypeSerializer.Deserialize(typeByte);
                    if (arrayType == typeof (object))
                    {
                        array.SetValue(new object(), i);
                    }
                    else
                    {
                        IDynamicSerializable dynamicSerializable = SerializerCache.GetSerializer(arrayType);
                        array.SetValue(dynamicSerializable.Deserialize(serializer), i);
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        #region Serialize

        public static void SerializeArray(
            Array array,
            ISerializerWriter serializer)
        {
            try
            {
                if (array == null ||
                    array.Length == 0)
                {
                    //
                    // this means the array should not be loaded
                    // when deserializing
                    //
                    serializer.Write(-1);
                    return;
                }

                int intItems = array.Length;
                serializer.Write(intItems);

                Type entryType = array.AsQueryable().ElementType;
                if (entryType == typeof (object))
                {
                    SerializeObjectTypeArray(array, serializer, entryType);
                }
                else if (!entryType.IsInterface &&
                         !entryType.IsAbstract)
                {
                    SerializeConcreteTypeArray(array, serializer, entryType);
                }
                else
                {
                    SerializeIntefaceTypeArray(array, serializer, entryType);
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void SerializeIntefaceTypeArray(
            Array array,
            ISerializerWriter serializer,
            Type entryType)
        {
            try
            {
                var typeBytes = ComplexTypeSerializer.Serialize(entryType);
                serializer.Write(typeBytes);
                //
                // serialize each item in the array
                //
                for (int i = 0; i < array.Length; i++)
                {
                    var item = array.GetValue(i);
                    var itemType = item.GetType();
                    serializer.Write(itemType);
                    IDynamicSerializable dynamicSerializable =
                        SerializerCache.GetSerializer(itemType);
                    dynamicSerializable.Serialize(item, serializer);
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void SerializeObjectTypeArray(
            Array array,
            ISerializerWriter serializer,
            Type entryType)
        {
            try
            {
                byte[] typeBytes = ComplexTypeSerializer.Serialize(entryType);
                serializer.Write(typeBytes);
                //
                // serialize each item in the array
                //
                foreach (object item in array)
                {
                    Type itemType = item == null ? typeof(object) : item.GetType();
                    typeBytes = ComplexTypeSerializer.Serialize(itemType);
                    serializer.Write(typeBytes);
                    if (itemType == typeof (object))
                    {
                        continue;
                    }
                    IDynamicSerializable dynamicSerializable = SerializerCache.GetSerializer(itemType);
                    dynamicSerializable.Serialize(item, serializer);
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void SerializeConcreteTypeArray(
            Array array,
            ISerializerWriter serializer,
            Type entryType)
        {
            try
            {
                IDynamicSerializable dynamicSerializable = SerializerCache.GetSerializer(entryType);
                var typeBytes = ComplexTypeSerializer.Serialize(entryType);
                serializer.Write(typeBytes);
                //
                // serialize each item in the array
                //
                foreach (var item in array)
                {
                    dynamicSerializable.Serialize(item, serializer);
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        #endregion

    }
}



