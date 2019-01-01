#region

using System;
using System.Linq;
using HC.Core.Io.KnownObjects.KnownTypes;
using HC.Core.Io.Serialization.Interfaces;
using HC.Core.Io.Serialization.Types;
using HC.Core.Logging;

#endregion

namespace HC.Core.Io.Serialization.Readers
{
    public class SerializerReader : ASerializerReader
    {
        #region Constructors

        public SerializerReader(byte[] data)
            : base(data)
        {
        }

        #endregion

        public override short ReadInt16()
        {
            return (short)m_input.ReadRawVarint32();
        }

        public override unsafe int ReadInt32()
        {
            return (int)m_input.ReadRawVarint32();
        }

        public override unsafe long ReadInt64()
        {
            return (long) m_input.ReadRawVarint64();
        }

        public override float ReadSingle()
        {
            float floatVal = 0;
            m_input.ReadFloat(ref floatVal);
            return floatVal;
        }

        public override unsafe double ReadDouble()
        {
            try
            {
                double dblValue = 0;
                m_input.ReadDouble(ref dblValue);
                return dblValue;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return 0;
        }

        public override bool ReadBoolean()
        {
            return (EnumSerializedType)ReadByte() == EnumSerializedType.BooleanTrueType;
        }

        public override DateTime ReadDateTime()
        {
            return new DateTime(ReadInt64());
        }

        public override Type ReadType()
        {
            var serializedType = (EnumSerializedType)ReadByte();
            if (serializedType == EnumSerializedType.ValueType)
            {
                var intTypeIndex = ReadInt32();
                return PrimitiveTypesCache.PrimitiveTypes[intTypeIndex];
            }
            if (serializedType == EnumSerializedType.ReferenceType)
            {
                var intTypeIndex = ReadInt32();
                if(intTypeIndex < 0)
                {
                    string strClass = ReadString();
                    if (!string.IsNullOrEmpty(strClass))
                    {
                        string strAssembly = ReadString();
                        if(!string.IsNullOrEmpty(strAssembly))
                        {
                            Type calcType = Type.GetType(
                                strClass + "," +
                                strAssembly);

                            if(calcType == null)
                            {
                                KnownTypesCache.Allypes.TryGetValue(
                                    strClass.Split('.').Last(),
                                    out calcType);
                            }
                            return calcType;
                        }
                    }
                    return null;
                }
                return KnownTypesCache.GetTypeFromId(intTypeIndex);
            }
            return null;
        }

        public override string ReadString()
        {
            if(ReadBoolean())
            {
                return string.Empty;
            }
            string value = string.Empty;
            m_input.ReadString(ref value);
            return value;
        }

        public override TimeSpan ReadTimeSpan()
        {
            return new TimeSpan(ReadInt64());
        }
    }
}


