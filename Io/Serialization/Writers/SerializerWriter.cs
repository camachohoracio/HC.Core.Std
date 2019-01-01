#region

using System;
using HC.Core.Exceptions;
using HC.Core.Io.KnownObjects.KnownTypes;
using HC.Core.Io.Serialization.Interfaces;
using HC.Core.Io.Serialization.Types;

#endregion

namespace HC.Core.Io.Serialization.Writers
{
    public class SerializerWriter : ASerializerWriter
    {
        public override void Write(Enum enumValue)
        {
            var currPropertyType = enumValue.GetType();
            int intTypeIndex = KnownTypesCache.GetTypeId(currPropertyType);
            Write(intTypeIndex);

            if (intTypeIndex == -1)
            {
                Write(enumValue.GetType());
            }

            var underlyingType = Enum.GetUnderlyingType(currPropertyType);
            if (underlyingType == typeof(string))
            {
                Write((string)(object)enumValue);
            }
            else if (underlyingType == typeof(int))
            {
                Write((int)(object)enumValue);
            }
            else if (underlyingType == typeof(long))
            {
                Write((long)(object)enumValue);
            }
            else if (underlyingType == typeof(byte))
            {
                Write((byte)(object)enumValue);
            }
            else if (underlyingType == typeof(short))
            {
                Write((short)(object)enumValue);
            }
            else
            {
                throw new HCException("Type not found");
            }
        }

        public override void Write(Type type)
        {
            if (type == null)
            {
                Write((byte)EnumSerializedType.NullType);
                return;
            }

            int intTypeIndex;

            if ((type.IsValueType ||
                type == typeof(string)) &&
                !type.IsEnum)
            {
                Write((byte)EnumSerializedType.ValueType);
                intTypeIndex = PrimitiveTypesCache.PrimitiveTypes.IndexOf(type);
            }
            else
            {
                Write((byte)EnumSerializedType.ReferenceType);
                intTypeIndex = KnownTypesCache.GetTypeId(type);

                if(intTypeIndex <0)
                {
                    Write(-1);
                    Write(type.FullName);
                    Write(type.Assembly.GetName().Name);
                    return;
                }
            }

            if(intTypeIndex < 0)
            {
                throw new HCException("Type not found: " + type.Name);
            }

            Write(intTypeIndex);
        }

        public override void Write(byte value)
        {
            m_output.WriteRawByte(value);
        }

        public override void Write(short value)
        {
            m_output.WriteRawVarint32((uint)value);
        }

        public override void Write(int value)
        {
            InternalWriteInt(value);
        }

        public override void Write(long value)
        {
            InternalWriteLong(value);
        }

        public override void Write(double value)
        {
            InternalWriteDouble(value);
        }

        public override void Write(float value)
        {
            m_output.WriteFloatNoTag(value);
        }

        public override void Write(bool value)
        {
            byte byteVal = (value ?
                (byte)EnumSerializedType.BooleanTrueType :
                (byte)EnumSerializedType.BooleanFalseType);
            Write(byteVal);
        }

        public override void Write(DateTime value)
        {
            Write(value.Ticks);
        }

        public override void Write(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                Write(true);
                return;
            }

            Write(false);
            m_output.WriteString(value);
        }

        public override void Write(TimeSpan value)
        {
            Write(value.Ticks);
        }
    }
}



