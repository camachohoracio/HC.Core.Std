#region

using System;
using System.IO;
using HC.Core.DynamicCompilation;
using HC.Core.Exceptions;
using HC.Core.Io.Serialization.Interfaces;
using HC.Core.Io.Serialization.ProtoBuf;
using HC.Core.Io.Serialization.Types;

#endregion

namespace HC.Core.Io.Serialization.Writers
{
    public abstract class ASerializerWriter : ISerializerWriter
    {
        #region Members

        /// <summary>
        /// A scratch buffer used by derived classes to avoid allocating arrays
        /// </summary>
        //protected readonly byte[] m_scratch = new byte[8];
        private readonly MemoryStream m_ms;

        protected readonly CodedOutputStream m_output;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the instance with a hint size of 128 bytes
        /// </summary>
        protected ASerializerWriter()
        {
            try
            {
                m_ms = new MemoryStream();
                m_output = CodedOutputStream.CreateInstance(m_ms);
            }
            catch (Exception ex)
            {
                Logging.Logger.Log(ex);
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// The current position within the buffer
        /// </summary>
        public int Position
        {
            get { return (int) m_ms.Position; }
        }

        /// <summary>
        /// The buffer that holds the encoded data
        /// </summary>
        public byte[] Buffer
        {
            get
            {
                m_output.Flush();
                byte[] buff = m_ms.GetBuffer();
                var buffer = new byte[Position];
                Array.Copy(buff, 0, buffer, 0, buffer.Length);
                return buffer;
            }
        }

        #endregion

        #region Abstract methods

        public abstract void Write(Enum enumValue);
        public abstract void Write(Type type);
        public abstract void Write(byte value);
        public abstract void Write(short value);
        public abstract void Write(int value);
        public abstract void Write(long value);
        public abstract void Write(double value);
        public abstract void Write(float value);
        public abstract void Write(bool value);
        public abstract void Write(DateTime value);
        public abstract void Write(string value);

        #endregion

        #region Write arrays

        public void Write(short[] value)
        {
            if (value == null)
            {
                Write(0);
                return;
            }
            Write(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                Write(value[i]);
            }
        }

        public void Write(float[] value)
        {
            if (value == null)
            {
                Write(0);
                return;
            }
            Write(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                Write(value[i]);
            }
        }

        public void Write(DateTime[] value)
        {
            if (value == null)
            {
                Write(0);
                return;
            }
            Write(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                Write(value[i]);
            }
        }

        public void Write(Type[] value)
        {
            if (value == null)
            {
                Write(0);
                return;
            }
            Write(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                Write(value[i]);
            }
        }

        public void Write(string[] value)
        {
            if (value == null)
            {
                Write(0);
                return;
            }
            Write(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                Write(value[i]);
            }
        }

        public void Write(long[] value)
        {
            if (value == null)
            {
                Write(0);
                return;
            }
            Write(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                Write(value[i]);
            }
        }

        public void Write(double[] value)
        {
            if (value == null)
            {
                Write(0);
                return;
            }
            Write(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                Write(value[i]);
            }
        }

        public void Write(bool[] value)
        {
            if (value == null)
            {
                Write(0);
                return;
            }
            Write(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                Write(value[i]);
            }
        }

        public void Write(int[] value)
        {
            if (value == null)
            {
                Write(0);
                return;
            }
            Write(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                Write(value[i]);
            }
        }

        public void Write(byte[] value)
        {
            if (value == null)
            {
                Write(0);
                return;
            }
            Write(value.Length);
            m_output.WriteRawBytes(value);
        }

        public abstract void Write(TimeSpan value);

        #endregion


        public byte[] GetBytes()
        {
            return Buffer;
        }

        public byte[] ToArray()
        {
            return GetBytes();
        }

        /// <summary>
        /// Writes a byte to the buffer
        /// </summary>
        /// <param name="data">The byte to write</param>
        protected void InternalWriteByte(byte data)
        {
            m_output.WriteRawByte(data);
        }

        /// <summary>
        /// Writes an int to the buffer
        /// </summary>
        /// <param name="data">The int to write</param>
        protected void InternalWriteInt(int data)
        {
            if (data >= 0)
            {
                m_output.WriteRawVarint32((uint)data);
            }
            else
            {
                // Must sign-extend.
                m_output.WriteRawVarint64((ulong)data);
            }
        }

        protected void InternalWriteDouble(double data)
        {
            m_output.WriteDoubleNoTag(data);
        }

        protected void InternalWriteLong(long data)
        {
            m_output.WriteRawVarint64((ulong)data);
        }

        /// <summary>
        /// Called when the Pico framework has finished writing data
        /// </summary>
        public void EndOfStream()
        {
            InternalWriteByte(0);
        }

        public void WriteRaw(object objValue)
        {
            if (objValue is string)
            {
                Write((string)objValue);
            }
            else if (objValue is char)
            {
                Write((char)objValue);
            }
            else if (objValue is bool)
            {
                Write((bool)objValue);
            }
            else if (objValue is Int16)
            {
                Write((short)objValue);
            }
            else if (objValue is Int32)
            {
                Write((int)objValue);
            }
            else if (objValue is Int64)
            {
                Write((long)objValue);
            }
            else if (objValue is Double)
            {
                Write((double)objValue);
            }
            else if (objValue is float)
            {
                Write((float)objValue);
            }
            else if (objValue is Type)
            {
                Write((Type)objValue);
            }
            else if (objValue is Byte)
            {
                Write((Byte)objValue);
            }
            else if (objValue is DateTime)
            {
                Write((DateTime)objValue);
            }
            else if (objValue is Enum)
            {
                Write((Enum)objValue);
            }
            else
            {
                //
                // default: write an object type
                //
                if (objValue == null)
                {
                    Write((byte)EnumSerializedType.NullType);
                    return;
                }
                Write((byte)EnumSerializedType.NonNullType);

                var objType = objValue.GetType();
                var currSerializer = Serializer.GetWriter();
                if (typeof(ASelfDescribingClass).IsAssignableFrom(objType))
                {
                    Write(typeof(SelfDescribingClass));
                    ((ASelfDescribingClass)objValue).Serialize(currSerializer);
                }
                else
                {
                    Write(objType);
                    SerializerCache.GetSerializer(objType).Serialize(
                        objValue,
                        currSerializer);
                }
                var buffer = currSerializer.GetBytes();
                Write(buffer);
            }
        }
         
        public void Write(object objValue)
        {
            try
            {
                //
                // default: write an object type
                //
                if (objValue == null)
                {
                    Write((byte) EnumSerializedType.NullType);
                    return;
                }
                Write((byte) EnumSerializedType.NonNullType);

                Type objType = objValue.GetType();

                if (objType == null)
                {
                    throw new HCException("Type not found");
                }

                //
                // write type
                //
                var typeBytes = ComplexTypeSerializer.Serialize(objType);
                Write(typeBytes);

                //if (typeof(ASelfDescribingClass).IsAssignableFrom(objType))
                //{
                //    Write(typeof(SelfDescribingClass));
                //}
                //else
                //{
                //    Write(objType);
                //}

                if (objValue is string)
                {
                    Write((string) objValue);
                }
                else if (objValue is char)
                {
                    Write((char) objValue);
                }
                else if (objValue is bool)
                {
                    Write((bool) objValue);
                }
                else if (objValue is Int16)
                {
                    Write((short) objValue);
                }
                else if (objValue is Int32)
                {
                    Write((int) objValue);
                }
                else if (objValue is Int64)
                {
                    Write((long) objValue);
                }
                else if (objValue is Double)
                {
                    Write((double) objValue);
                }
                else if (objValue is float)
                {
                    Write((float) objValue);
                }
                else if (objValue is Type)
                {
                    Write((Type) objValue);
                }
                else if (objValue is Byte)
                {
                    Write((Byte) objValue);
                }
                else if (objValue is DateTime)
                {
                    Write((DateTime) objValue);
                }
                else if (objValue is Enum)
                {
                    Write((Enum) objValue);
                }
                else if (objType != typeof (Object))
                {
                    var currSerializer = Serializer.GetWriter();
                    if (typeof (ASelfDescribingClass).IsAssignableFrom(objType))
                    {
                        ((ASelfDescribingClass) objValue).Serialize(currSerializer);
                    }
                    else
                    {
                        SerializerCache.GetSerializer(objType).Serialize(
                            objValue,
                            currSerializer);
                    }
                    var buffer = currSerializer.GetBytes();
                    Write(buffer);
                }
            }
            catch(Exception ex)
            {
                Logging.Logger.Log(ex);
            }
        }

        public void Write0(object objValue)
        {
            if (objValue is string)
            {
                Write((string)objValue);
            }
            else if (objValue is char)
            {
                Write((char)objValue);
            }
            else if (objValue is bool)
            {
                Write((bool)objValue);
            }
            else if (objValue is Int16)
            {
                Write((short)objValue);
            }
            else if (objValue is Int32)
            {
                Write((int)objValue);
            }
            else if (objValue is Int64)
            {
                Write((long)objValue);
            }
            else if (objValue is Double)
            {
                Write((double)objValue);
            }
            else if (objValue is float)
            {
                Write((float)objValue);
            }
            else if (objValue is Type)
            {
                Write((Type)objValue);
            }
            else if (objValue is Byte)
            {
                Write((Byte)objValue);
            }
            else if (objValue is DateTime)
            {
                Write((DateTime)objValue);
            }
            else if (objValue is Enum)
            {
                Write((Enum)objValue);
            }
            else
            {
                //
                // default: write an object type
                //
                if (objValue == null)
                {
                    Write((byte)EnumSerializedType.NullType);
                    return;
                }
                Write((byte)EnumSerializedType.NonNullType);

                var objType = objValue.GetType();
                var currSerializer = Serializer.GetWriter();
                if (typeof(ASelfDescribingClass).IsAssignableFrom(objType))
                {
                    Write(typeof(SelfDescribingClass));
                    ((ASelfDescribingClass)objValue).Serialize(currSerializer);
                }
                else
                {
                    Write(objType);
                    SerializerCache.GetSerializer(objType).Serialize(
                        objValue,
                        currSerializer);
                }
                var buffer = currSerializer.GetBytes();
                Write(buffer);
            }
        }
    }
}



