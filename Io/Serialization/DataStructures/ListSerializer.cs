#region

using System;
using System.Collections;
using System.Linq;
using HC.Core.DynamicCompilation;
using HC.Core.Io.Serialization.Interfaces;
using HC.Core.Io.Serialization.Readers;
using HC.Core.Io.Serialization.Types;
using HC.Core.Io.Serialization.Writers;
using HC.Core.Logging;

#endregion

namespace HC.Core.Io.Serialization.DataStructures
{
    public static class ListSerializer
    {
        public static void DeserializeList(
            ISerializerReader serializer,
            IList list,
            int intListSize,
            Type listType)
        {
            try
            {
                if (intListSize == -1)
                {
                    return;
                }
                serializer.ReadByteArray();
                if (!listType.IsInterface &&
                    !listType.IsAbstract)
                {
                    if (listType == typeof (object))
                    {
                        DeserializeObjectTypeList(serializer, list, intListSize);
                    }
                    else
                    {
                        DeserializeConcreteTypeList(serializer, list, intListSize, listType);
                    }
                }
                else
                {
                    DeserializeInterfaceTypeList(serializer, list, intListSize, listType);
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void DeserializeConcreteTypeList(ISerializerReader serializer, IList list, int intListSize, Type listType)
        {
            IDynamicSerializable dynamicSerializable = SerializerCache.GetSerializer(listType);
            for (int i = 0; i < intListSize; i++)
            {
                list.Add(dynamicSerializable.Deserialize(serializer));
            }
        }

        private static void DeserializeInterfaceTypeList(
            ISerializerReader serializer, 
            IList list, 
            int intListSize,
            Type listType)
        {
            for (int i = 0; i < intListSize; i++)
            {
                Type itemType = serializer.ReadType();
                IDynamicSerializable currDynamicSerializable = SerializerCache.GetSerializer(itemType);
                var item = currDynamicSerializable.Deserialize(serializer);
                list.Add(item);
            }
        }

        private static void DeserializeObjectTypeList(
            ISerializerReader serializerReader, 
            IList list, 
            int intListSize)
        {
            try
            {
                for (int i = 0; i < intListSize; i++)
                {
                    try
                    {
                        bool blnIsNull = serializerReader.ReadBoolean();
                        if (blnIsNull)
                        {
                            list.Add(null);
                            continue;
                        }
                        byte[] typeByte = serializerReader.ReadByteArray();
                        Type itemType = ComplexTypeSerializer.Deserialize(typeByte);
                        if (itemType == typeof (object))
                        {
                            list.Add(new object());
                        }
                        else if (typeof (ASelfDescribingClass).IsAssignableFrom(itemType))
                        {
                            list.Add(ASelfDescribingClass.DeserializeStatic(serializerReader));
                        }
                        else
                        {
                            if (itemType != m_runtimeType)
                            {
                                IDynamicSerializable dynamicSerializable = SerializerCache.GetSerializer(itemType);
                                list.Add(dynamicSerializable.Deserialize(serializerReader));
                            }
                            else
                            {
                                //
                                // deserialize a type
                                //
                                byte[] bytes = serializerReader.ReadByteArray();
                                list.Add(ComplexTypeSerializer.Deserialize(bytes));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public static void SerializeList(
            IList list,
            ISerializerWriter serializer)
        {
            if (list == null ||
                list.Count == 0)
            {
                //
                // this means the list should not be loaded
                // when deserializing
                //
                serializer.Write(-1);
                return;
            }

            int intItems = list.Count;
            serializer.Write(intItems);

            Type entryType = list.AsQueryable().ElementType;
            if (entryType == typeof(object))
            {
                SerializeObjectTypeList(list, serializer, entryType);
            }
            else if (!entryType.IsInterface &&
                !entryType.IsAbstract)
            {
                SerializeConcreteTypeList(list, serializer, entryType);
            }
            else
            {
                SerializeIntefaceTypeList(list, serializer, entryType);
            }
        }

        private static void SerializeIntefaceTypeList(
            IList list,
            ISerializerWriter serializer,
            Type entryType)
        {
            var typeBytes = ComplexTypeSerializer.Serialize(entryType);
            serializer.Write(typeBytes);
            //
            // serialize each item in the list
            //
            //bool blnWriteType = entryType != typeof (ASelfDescribingClass);
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                Type itemType = item.GetType();
                //if (blnWriteType)
                {
                    serializer.Write(itemType);
                }
                IDynamicSerializable dynamicSerializable = 
                    SerializerCache.GetSerializer(itemType);
                dynamicSerializable.Serialize(item, serializer);
            }
        }

        private static readonly Type m_runtimeType = Type.GetType("System.RuntimeType");

        private static void SerializeObjectTypeList(
            IList list,
            ISerializerWriter serializer,
            Type entryType)
        {
            byte[] typeBytes = ComplexTypeSerializer.Serialize(entryType);
            serializer.Write(typeBytes);
            //
            // serialize each item in the list
            //
            foreach (object item in list)
            {
                if(item == null)
                {
                    serializer.Write(true); // is null
                    continue;
                }
                serializer.Write(false); // is not null
                Type itemType = item.GetType();
                bool blnIsSelfDescr = false;
                if(typeof(ASelfDescribingClass).IsAssignableFrom(itemType))
                {
                    itemType = typeof (SelfDescribingClass);
                    blnIsSelfDescr = true;
                }
                typeBytes = ComplexTypeSerializer.Serialize(itemType);
                serializer.Write(typeBytes);
                if (itemType == typeof(object))
                {
                    continue;
                }
                if (!blnIsSelfDescr)
                {
                    if (itemType != m_runtimeType)
                    {
                        IDynamicSerializable dynamicSerializable = SerializerCache.GetSerializer(itemType);
                        dynamicSerializable.Serialize(item, serializer);
                    }
                    else
                    {
                        byte[] bytes = ComplexTypeSerializer.Serialize(item as Type);
                        serializer.Write(bytes);
                    }
                }
                else
                {
                    ((ASelfDescribingClass)item).Serialize(serializer);
                }
            }
        }

        private static void SerializeConcreteTypeList(IList list, ISerializerWriter serializer, Type entryType)
        {
            IDynamicSerializable dynamicSerializable = SerializerCache.GetSerializer(entryType);
            var typeBytes = ComplexTypeSerializer.Serialize(entryType);
            serializer.Write(typeBytes);
            //
            // serialize each item in the list
            //
            foreach (var item in list)
            {
                dynamicSerializable.Serialize(item, serializer);
            }
        }

    }
}



