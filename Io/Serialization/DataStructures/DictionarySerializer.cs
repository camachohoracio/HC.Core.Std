#region

using System;
using System.Collections;
using System.Linq;
using HC.Core.Exceptions;
using HC.Core.Io.Serialization.Interfaces;
using HC.Core.Io.Serialization.Readers;
using HC.Core.Io.Serialization.Writers;
using HC.Core.Logging;

#endregion

namespace HC.Core.Io.Serialization.DataStructures
{
    public static class DictionarySerializer
    {
        public static void DeserializeDictionary(
            IDictionary map,
            Type keyType,
            Type valueType,
            ISerializerReader serializer)
        {
            try
            {
                if(valueType.IsInterface)
                {
                    throw new HCException("Invterface value maps not yet implemented. Meanwhile Use object or a concrete type");
                }

                var enumSerializedType = (EnumSerializedType) serializer.ReadByte();
                if (enumSerializedType == EnumSerializedType.NullType)
                {
                    return;
                }

                int intCollectionSize = serializer.ReadInt32();

                IDynamicSerializable dynamicSerializableKey = SerializerCache.GetSerializer(keyType);
                IDynamicSerializable dynamicSerializableValue = SerializerCache.GetSerializer(valueType);
                for (int i = 0; i < intCollectionSize; i++)
                {
                    try
                    {
                        object key = dynamicSerializableKey.Deserialize(serializer);
                        object value = dynamicSerializableValue.Deserialize(serializer);
                        if (key != null)
                        {
                            map[key] = value;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public static void SerializeDictionary(
            IDictionary dictionary,
            ISerializerWriter serializer)
        {
            if (dictionary == null)
            {
                serializer.Write((byte)EnumSerializedType.NullType);
                return;
            }
            serializer.Write((byte)EnumSerializedType.NonNullType);

            Type keyType = dictionary.Keys.AsQueryable().ElementType;
            Type valueType = dictionary.Values.AsQueryable().ElementType;

            //
            // serialize array of objects
            //
            IDynamicSerializable dynamicSerializableKey = SerializerCache.GetSerializer(keyType);
            IDynamicSerializable dynamicSerializableValue = SerializerCache.GetSerializer(valueType);

            if (dictionary.Count == 0)
            {
                //
                // this means the dictionary should not be loaded
                // when deserializing
                //
                serializer.Write(-1);
                return;
            }

            //
            // write number of items
            //
            int intItems = dictionary.Count;
            serializer.Write(intItems);

            //
            // serialize each item in the map
            //
            IEnumerator keys = dictionary.Keys.GetEnumerator();
            IEnumerator values = dictionary.Values.GetEnumerator();
            for (int i = 0; i < intItems; i++)
            {
                keys.MoveNext();
                values.MoveNext();
                dynamicSerializableKey.Serialize(keys.Current, serializer);
                dynamicSerializableValue.Serialize(values.Current, serializer);
            }
        }
    }
}



