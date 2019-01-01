#region

using System;
using System.IO;
using HC.Core.DynamicCompilation;
using HC.Core.Exceptions;
using HC.Core.Io.KnownObjects.KnownTypes;
using HC.Core.Io.Serialization.Interfaces;
using HC.Core.Io.Serialization.ProtoBuf;
using HC.Core.Io.Serialization.Types;
using HC.Core.Logging;

#endregion

namespace HC.Core.Io.Serialization.Readers
{
    public abstract class ASerializerReader : ISerializerReader
    {
        readonly MemoryStream m_ms;
        protected readonly CodedInputStream m_input;
        private readonly int m_intLength;

        #region Properties

        /// <summary>
        /// The current position within the buffer
        /// </summary>
        public int Position
        {
            get { return (int)m_ms.Position; }
        }

        public int BytesRemaining
        {
            get { return m_intLength - (m_input.bufferPos + 1); }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the instance
        /// </summary>
        /// <param name="data">An existing buffer to use</param>
        public ASerializerReader(byte[] data)
        {
            try
            {
                m_ms = new MemoryStream(data);
                m_input = CodedInputStream.CreateInstance(m_ms);
                m_intLength = (int) m_ms.Length;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        #endregion

        #region Abstract methods

        public abstract short ReadInt16();
        public abstract int ReadInt32();
        public abstract long ReadInt64();
        public abstract float ReadSingle();
        public abstract double ReadDouble();
        public abstract bool ReadBoolean();
        public abstract DateTime ReadDateTime();
        public abstract Type ReadType();
        public abstract string ReadString();

        #endregion

        #region read arrays

        public Single[] ReadSingleArray()
        {
            int intArrayLength = ReadInt32();
            if (intArrayLength <= 0)
            {
                return null;
            }

            var values = new Single[intArrayLength];
            for (int i = 0; i < intArrayLength; i++)
            {
                values[i] = ReadSingle();
            }
            return values;
        }

        public string[] ReadStringArray()
        {
            int intArrayLength = ReadInt32();
            if (intArrayLength <= 0)
            {
                return null;
            }

            var values = new string[intArrayLength];
            for (int i = 0; i < intArrayLength; i++)
            {
                values[i] = ReadString();
            }
            return values;
        }

        public DateTime[] ReadDateTimeArray()
        {
            int intArrayLength = ReadInt32();
            if (intArrayLength <= 0)
            {
                return null;
            }

            var values = new DateTime[intArrayLength];
            for (int i = 0; i < intArrayLength; i++)
            {
                values[i] = ReadDateTime();
            }
            return values;
        }

        public short[] ReadInt16Array()
        {
            int intArrayLength = ReadInt32();
            if (intArrayLength <= 0)
            {
                return null;
            }

            var values = new short[intArrayLength];
            for (int i = 0; i < intArrayLength; i++)
            {
                values[i] = ReadInt16();
            }
            return values;
        }

        public Type[] ReadTypeArray()
        {
            int intArrayLength = ReadInt32();
            if (intArrayLength <= 0)
            {
                return null;
            }

            var values = new Type[intArrayLength];
            for (int i = 0; i < intArrayLength; i++)
            {
                values[i] = ReadType();
            }
            return values;
        }

        public long[] ReadInt64Array()
        {
            int intArrayLength = ReadInt32();
            if (intArrayLength <= 0)
            {
                return null;
            }

            var values = new long[intArrayLength];
            for (int i = 0; i < intArrayLength; i++)
            {
                values[i] = ReadInt64();
            }
            return values;
        }

        public byte[] ReadByteArray()
        {
            int intArrayLength = ReadInt32();
            if (intArrayLength <= 0)
            {
                return null;
            }

            return m_input.ReadRawBytes(intArrayLength);
        }

        public int[] ReadInt32Array()
        {
            int intArrayLength = ReadInt32();
            if (intArrayLength <= 0)
            {
                return null;
            }
            var values = new int[intArrayLength];
            for (int i = 0; i < intArrayLength; i++)
            {
                values[i] = ReadInt32();
            }
            return values;
        }

        public bool[] ReadBooleanArray()
        {
            int intArrayLength = ReadInt32();
            if (intArrayLength <= 0)
            {
                return null;
            }
            
            var values = new bool[intArrayLength];
            for (int i = 0; i < intArrayLength; i++)
            {
                values[i] = ReadBoolean();
            }
            return values;
        }

        public double[] ReadDblArray()
        {
            int intArrayLength = ReadInt32();
            if (intArrayLength <= 0)
            {
                return null;
            }

            var values = new double[intArrayLength];
            for (int i = 0; i < intArrayLength; i++)
            {
                values[i] = ReadDouble();
            }
            return values;
        }

        #endregion


        public object ReadObject()
        {
            try
            {
                var serializedType = (EnumSerializedType) ReadByte();
                if (serializedType == EnumSerializedType.NullType)
                {
                    return null;
                }

                byte[] typeByte = ReadByteArray();
                Type type = ComplexTypeSerializer.Deserialize(typeByte);

                //Type type = ReadType();

                if (typeof (ASelfDescribingClass).IsAssignableFrom(type))
                {
                    var currBuffer = ReadByteArray();
                    return new SelfDescribingClass().Deserialize(currBuffer);
                }
                if(type == null)
                {
                    return null;
                }
                if (type.IsValueType)
                {
                    if (type == typeof (bool))
                    {
                        return ReadBoolean();
                    }
                    if (type == typeof (Int16))
                    {
                        return ReadInt16();
                    }
                    if (type == typeof (Int32))
                    {
                        return ReadInt32();
                    }
                    if (type == typeof (Int64))
                    {
                        return ReadInt64();
                    }
                    if (type == typeof (Double))
                    {
                        return ReadDouble();
                    }
                    if (type == typeof (float))
                    {
                        return ReadDouble();
                    }
                    if (type == typeof (Type))
                    {
                        return ReadType();
                    }
                    if (type == typeof (Byte))
                    {
                        return ReadByte();
                    }
                    if (type == typeof (DateTime))
                    {
                        return ReadDateTime();
                    }
                    if (type == typeof(TimeSpan))
                    {
                        return ReadTimeSpan();
                    }
                    if (type == typeof(Object))
                    {
                        return new object();
                    }
                    if (type.IsEnum)
                    {
                        return ReadEnum();
                    }
                    throw new HCException("Type not found [" +
                        type.Name + "]");
                }
                if (type == typeof (string))
                {
                    return ReadString();
                }

                if (type == typeof (Object))
                {
                    return new object();
                }
                byte[] buffer = ReadByteArray();
                return SerializerCache.GetSerializer(type).Deserialize(
                    Serializer.GetReader(buffer));
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        private object ReadEnum()
        {
            int intTypeIndex = ReadInt32();
            Type enumType;
            if(intTypeIndex == -1)
            {
                enumType = ReadType();
            }
            else
            {
                enumType = KnownTypesCache.GetTypeFromId(intTypeIndex);
            }
            Type underlyingType = Enum.GetUnderlyingType(enumType);
            if (underlyingType == typeof(string))
            {
                string strVal = ReadString();
                return Enum.ToObject(enumType, strVal);
            }
            if (underlyingType == typeof(int))
            {
                int intVal = ReadInt32();
                return Enum.ToObject(enumType, intVal);
            }
            if (underlyingType == typeof(long))
            {
                long lngVal = ReadInt64();
                return Enum.ToObject(enumType, lngVal);
            }
            if (underlyingType == typeof(byte))
            {
                byte byteVal = ReadByte();
                return Enum.ToObject(enumType, byteVal);
            }
            if (underlyingType == typeof(short))
            {
                short val = ReadInt16();
                return Enum.ToObject(enumType, val);
            }
            throw new HCException("Enum underlyig type not found [" +
                                  underlyingType.Name + "]");
        }

        public abstract TimeSpan ReadTimeSpan();

        public byte[] ReadBytes(int intLength)
        {
             return m_input.ReadRawBytes(intLength);
        }

        public byte ReadByte()
        {
            try
            {
                return m_input.ReadRawByte();
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return 0;
        }
    }
}



