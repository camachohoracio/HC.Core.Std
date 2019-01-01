//#region

//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Collections.Specialized;
//using System.Diagnostics;
//using HC.Core.DynamicCompilation;

//#endregion

//namespace HC.Core.Io.Serialization.FastSerialization
//{
//    ///<summary>
//    ///  A SerializationReader instance is used to read stored values and objects from a byte array.
//    ///
//    ///  Once an instance is created, use the various methods to read the required data.
//    ///  The data read MUST be exactly the same type and in the same order as it was written.
//    ///</summary>
//    public sealed class SerializationReader : ISerializerReader
//    {
//        #region Members

//        // Marker to denote that all elements in a value array are optimizable
//        private static readonly BitArray m_fullyOptimizableTypedArray = new BitArray(0);
//        //private static readonly Type m_nestedObjectType = typeof (NestedObjectType);
//        //private static readonly Type m_listObjectType = typeof (ListObjectType);
//        //private static readonly Type m_endOfListObjectType = typeof (EndOfListObjectType);
//        //private static readonly Type m_dictionaryObjectType = typeof (DictionaryObjectType);
//        //private static readonly Type m_endOfDictionaryObjectType = typeof (EndOfDictionaryObjectType);
//        //private static readonly object m_typeLock = new object();
//        private readonly int m_endPosition;
//        private readonly int m_startPosition;
//        private List<object> m_objectTokenList;
//        private List<string> m_stringTokenList;
//        private readonly SerializerReaderOld m_reader;

//        #endregion

//        public int Position
//        {
//            get { return m_reader.Position; }
//        }

//        #region read arrays

//        public Single[] ReadSingleArray()
//        {
//            int intArrayLength = ReadInt32();
//            if (intArrayLength <= 0)
//            {
//                return null;
//            }

//            var values = new Single[intArrayLength];
//            for (int i = 0; i < intArrayLength; i++)
//            {
//                values[i] = ReadSingle();
//            }
//            return values;
//        }

//        public string[] ReadStringArray()
//        {
//            int intArrayLength = ReadInt32();
//            if (intArrayLength <= 0)
//            {
//                return null;
//            }

//            var values = new string[intArrayLength];
//            for (int i = 0; i < intArrayLength; i++)
//            {
//                values[i] = ReadString();
//            }
//            return values;
//        }

//        public char[] ReadCharArray()
//        {
//            int intArrayLength = ReadInt32();
//            if (intArrayLength <= 0)
//            {
//                return null;
//            }

//            var values = new char[intArrayLength];
//            for (int i = 0; i < intArrayLength; i++)
//            {
//                values[i] = ReadChar();
//            }
//            return values;
//        }

//        #endregion

//        #region Debug Related

//        /// <summary>
//        ///   Dumps the string tables.
//        /// </summary>
//        /// <param name = "list">The list.</param>
//        [Conditional("DEBUG")]
//        public void DumpStringTables(ArrayList list)
//        {
//            list.AddRange(m_stringTokenList);
//        }

//        #endregion

//        private static readonly Dictionary<string, Type> m_typeLookup = new Dictionary<string, Type>();

//        /// <summary>
//        ///   Initializes a new instance of the <see cref = "SerializationReader" /> class.
//        /// </summary>
//        public SerializationReader(byte[] buffer)
//        {
//            m_reader = new SerializerReaderOld(buffer);
//            // Store the start position of the stream if seekable
//            m_startPosition = m_reader.Position;

//            // Always read the first 4 bytes
//            m_endPosition = m_startPosition + m_reader.ReadInt32();

//            // If the first four bytes are zero
//            if (m_startPosition == m_endPosition)
//            {
//                // then there is no token table presize info
//                InitializeTokenTables(0, 0);
//            }
//            else
//            {
//                // Use the correct token table sizes
//                InitializeTokenTables(m_reader.ReadInt32(), m_reader.ReadInt32());
//            }
//        }

//        /// <summary>
//        ///   Returns an ArrayList or null from the stream.
//        /// </summary>
//        /// <returns>An ArrayList instance.</returns>
//        //public ArrayList ReadArrayList()
//        //{
//        //    if (ReadTypeCode() == EnumSerializedType.NullType) return null;

//        //    return new ArrayList(ReadOptimizedObjectArray());
//        //}

//        /// <summary>
//        ///   Returns a BitArray or null from the stream.
//        /// </summary>
//        /// <returns>A BitArray instance.</returns>
//        public BitArray ReadBitArray()
//        {
//            if (ReadTypeCode() == EnumSerializedType.NullType) return null;

//            return ReadOptimizedBitArray();
//        }

//        /// <summary>
//        ///   Returns a BitVector32 value from the stream.
//        /// </summary>
//        /// <returns>A BitVector32 value.</returns>
//        public BitVector32 ReadBitVector32()
//        {
//            return new BitVector32(m_reader.ReadInt32());
//        }

//        /// <summary>
//        ///   Returns a DateTime value from the stream.
//        /// </summary>
//        /// <returns>A DateTime value.</returns>
//        public DateTime ReadDateTime()
//        {
//            var serializedType = (EnumSerializedType) m_reader.ReadByte();
//            switch (serializedType)
//            {
//                case EnumSerializedType.DateTimeType:
//                    return m_reader.ReadDateTime();
//                case EnumSerializedType.OptimizedDateTimeType:
//                    return ReadOptimizedDateTime();
//                case EnumSerializedType.MinDateTimeType:
//                    return DateTime.MinValue;
//                case EnumSerializedType.MaxDateTimeType:
//                    return DateTime.MaxValue;
//            }
//            throw new NotImplementedException();
//        }

//        public object ReadObject()
//        {
//            var serializedType = (EnumSerializedType) ReadByte();
//            if (serializedType == EnumSerializedType.NullType)
//            {
//                return null;
//            }
//            var type = ReadType();
//            var buffer = ReadByteArray();

//            if (typeof(ASelfDescribingClass).IsAssignableFrom(type))
//            {
//                return new SelfDescribingClass().Deserialize(buffer);
//            }
//            return SerializerCache.GetSerializer(type).Deserialize(
//                GetSerializer(buffer));
//        }

//        private static ISerializerReader GetSerializer(byte[] buffer)
//        {
//            return new SerializationReader(buffer);
//        }

//        /// <summary>
//        ///   Returns an object based on the EnumSerializedType read next from the stream.
//        /// </summary>
//        /// <returns>An object instance.</returns>
//        //public object ReadObject()
//        //{
//        //    var obj = ReadObjectIntern();

//        //    bool blnInnerType = false;
//        //    bool blnListObjectType = false;
//        //    bool blnDictionaryObjectType = false;

//        //    if (obj is byte)
//        //    {
//        //        var byteType = (EnumSerializedType)obj;
//        //        blnInnerType = byteType == EnumSerializedType.InnerObjectType;
//        //        blnListObjectType = byteType == EnumSerializedType.ListObjectType;
//        //        blnDictionaryObjectType = byteType == EnumSerializedType.DictionaryObjectType;
//        //    }
//        //    if (blnInnerType)
//        //    {
//        //        int intFastSerializableType = (int)ReadObjectIntern();
//        //        var fastSerializableType = SerializerHelper.KnownTypes[intFastSerializableType];
//        //        //var fastSerializableType = GetTypeFromLookup(strFastSerializableType);
//        //        var fastSerializableObj =
//        //            (ISerializable)ReflectorCache.GetReflector(
//        //                fastSerializableType).CreateInstance();
//        //        var bytes = ReadByteArray();
//        //        return fastSerializableObj.Deserialize(bytes);
//        //    }
//        //    if (blnListObjectType)
//        //    {
//        //        return ReadList();
//        //    }
//        //    if (blnDictionaryObjectType)
//        //    {
//        //        return ReadDictionary();
//        //    }
//        //    return obj;
//        //}

//        //private object ReadList()
//        //{
//        //    var listType = (Type)ReadObjectIntern();
//        //    //
//        //    // create a list with the specified type
//        //    //
//        //    var genericListType = typeof(List<>);
//        //    var specificBinderType = genericListType.MakeGenericType(listType);
//        //    var list = (IList)Activator.CreateInstance(specificBinderType);

//        //    //
//        //    // keep reading untill the end of the list
//        //    //
//        //    while (BytesRemaining > 0)
//        //    {
//        //        var item = ReadObject();
//        //        bool blnEndOfList = false;
//        //        bool blnListObject = false;
//        //        if (item is byte)
//        //        {
//        //            var serializedType = (EnumSerializedType)item;
//        //            blnEndOfList = serializedType == EnumSerializedType.EndOfListObjectType;
//        //            blnListObject = serializedType == EnumSerializedType.ListObjectType;
//        //        }

//        //        //if (itemType != null)
//        //        {
//        //            if (blnEndOfList)
//        //            {
//        //                //
//        //                // this is the end of the lst
//        //                //
//        //                return list;
//        //            }
//        //            if (blnListObject)
//        //            {
//        //                return ReadList();
//        //            }
//        //        }
//        //        list.Add(item);
//        //    }
//        //    return null;
//        //}

//        //private object ReadDictionary()
//        //{
//        //    var keyType = (Type)ReadObjectIntern();
//        //    var valueType = (Type)ReadObjectIntern();
//        //    //
//        //    // create a list with the specified type
//        //    //
//        //    var genericDictType = typeof(Dictionary<,>);
//        //    var specificBinderType = genericDictType.MakeGenericType(new[] { keyType, valueType });
//        //    var dictionary = (IDictionary)Activator.CreateInstance(specificBinderType);

//        //    //
//        //    // keep reading untill the end of the list
//        //    //
//        //    while (BytesRemaining > 0)
//        //    {
//        //        var keyObj = ReadObject();
//        //        bool blnEndOfDictionaryType = false;
//        //        bool blnDictionaryType = false;
//        //        if (keyObj is byte)
//        //        {
//        //            var serializedType = (EnumSerializedType)keyObj;
//        //            blnEndOfDictionaryType = serializedType == EnumSerializedType.EndOfDictionaryObjectType;
//        //            blnDictionaryType = serializedType == EnumSerializedType.DictionaryObjectType;
//        //        }
//        //        if (blnEndOfDictionaryType)
//        //        {
//        //            //
//        //            // this is the end of the lst
//        //            //
//        //            return dictionary;
//        //        }
//        //        if (blnDictionaryType)
//        //        {
//        //            return ReadDictionary();
//        //        }
//        //        var valueObj = ReadObject();
//        //        dictionary.Add(keyObj, valueObj);
//        //    }
//        //    return null;
//        //}

//        //private object ReadObjectIntern()
//        //{
//        //    return ProcessObject((EnumSerializedType)m_reader.ReadByte());
//        //}

//        /// <summary>
//        ///   Called ReadOptimizedString().
//        ///   This override to hide base BinaryReader.ReadString().
//        /// </summary>
//        /// <returns>A string value.</returns>
//        public string ReadString()
//        {
//            return ReadOptimizedString();
//        }

//        /// <summary>
//        ///   Returns a string value from the stream.
//        /// </summary>
//        /// <returns>A string value.</returns>
//        //public string ReadStringDirect()
//        //{
//        //    return m_reader.ReadString();
//        //}

//        /// <summary>
//        ///   Returns a TimeSpan value from the stream.
//        /// </summary>
//        /// <returns>A TimeSpan value.</returns>
//        public TimeSpan ReadTimeSpan()
//        {
//            return new TimeSpan(m_reader.ReadInt64());
//        }

//        /// <summary>
//        ///   Returns a Type or null from the stream.
//        /// 
//        ///   Throws an exception if the Type cannot be found.
//        /// </summary>
//        /// <returns>A Type instance.</returns>
//        public Type ReadType()
//        {
//            return m_reader.ReadType();
//        }

//        /// <summary>
//        ///   Returns a Type or null from the stream.
//        /// 
//        ///   Throws an exception if the Type cannot be found and throwOnError is true.
//        /// </summary>
//        /// <returns>A Type instance.</returns>
//        public Type ReadType(bool throwOnError)
//        {
//            throw new NotImplementedException();
//            if (ReadTypeCode() == EnumSerializedType.NullType) return null;

//            return Type.GetType(ReadOptimizedString(), throwOnError);
//        }

//        /// <summary>
//        ///   Returns an ArrayList from the stream that was stored optimized.
//        /// </summary>
//        /// <returns>An ArrayList instance.</returns>
//        public ArrayList ReadOptimizedArrayList()
//        {
//            return new ArrayList(ReadOptimizedObjectArray());
//        }

//        /// <summary>
//        ///   Returns a BitArray from the stream that was stored optimized.
//        /// </summary>
//        /// <returns>A BitArray instance.</returns>
//        public BitArray ReadOptimizedBitArray()
//        {
//            var length = ReadOptimizedInt32();
//            if (length == 0) return m_fullyOptimizableTypedArray;

//            return new BitArray(m_reader.ReadBytes((length + 7) / 8)) { Length = length };
//        }

//        /// <summary>
//        ///   Returns a DateTime value from the stream that was stored optimized.
//        /// </summary>
//        /// <returns>A DateTime value.</returns>
//        public DateTime ReadOptimizedDateTime()
//        {
//            // Read date information from first three bytes
//            var dateMask = new BitVector32(m_reader.ReadByte() |
//                (m_reader.ReadByte() << 8) | (m_reader.ReadByte() << 16));
//            var result = new DateTime(
//                dateMask[SerializationWriter.DateYearMask],
//                dateMask[SerializationWriter.DateMonthMask],
//                dateMask[SerializationWriter.DateDayMask]
//                );

//            if (dateMask[SerializationWriter.DateHasTimeOrKindMask] == 1)
//            {
//                var initialByte = m_reader.ReadByte();
//                var dateTimeKind = (DateTimeKind)(initialByte & 0x03);

//                // Remove the IsNegative and HasDays flags which are never true for a DateTime
//                initialByte &= 0xfc;
//                if (dateTimeKind != DateTimeKind.Unspecified)
//                {
//                    result = DateTime.SpecifyKind(result, dateTimeKind);
//                }

//                if (initialByte == 0)
//                {
//                    // No need to call decodeTimeSpan if there is no time information
//                    m_reader.ReadByte();
//                }
//                else
//                {
//                    result = result.Add(DecodeTimeSpan(initialByte));
//                }
//            }

//            return result;
//        }

//        /// <summary>
//        ///   Returns a Decimal value from the stream that was stored optimized.
//        /// </summary>
//        /// <returns>A Decimal value.</returns>
//        public Decimal ReadOptimizedDecimal()
//        {
//            var flags = m_reader.ReadByte();
//            var lo = 0;
//            var mid = 0;
//            var hi = 0;
//            byte scale = 0;

//            if ((flags & 0x02) != 0)
//            {
//                scale = m_reader.ReadByte();
//            }

//            if ((flags & 4) == 0)
//            {
//                lo = (flags & 32) != 0 ? ReadOptimizedInt32() : m_reader.ReadInt32();
//            }

//            if ((flags & 8) == 0)
//            {
//                mid = (flags & 64) != 0 ? ReadOptimizedInt32() : m_reader.ReadInt32();
//            }

//            if ((flags & 16) == 0)
//            {
//                hi = (flags & 128) != 0 ? ReadOptimizedInt32() : m_reader.ReadInt32();
//            }

//            return new decimal(lo, mid, hi, (flags & 0x01) != 0, scale);
//        }

//        /// <summary>
//        ///   Returns an Int32 value from the stream that was stored optimized.
//        /// </summary>
//        /// <returns>An Int32 value.</returns>
//        public int ReadOptimizedInt32()
//        {
//            var result = 0;
//            var bitShift = 0;

//            while (true)
//            {
//                var nextByte = m_reader.ReadByte();

//                result |= (nextByte & 0x7f) << bitShift;
//                bitShift += 7;

//                if ((nextByte & 0x80) == 0) return result;
//            }
//        }

//        /// <summary>
//        ///   Returns an Int16 value from the stream that was stored optimized.
//        /// </summary>
//        /// <returns>An Int16 value.</returns>
//        public short ReadOptimizedInt16()
//        {
//            return (short)ReadOptimizedInt32();
//        }

//        /// <summary>
//        ///   Returns an Int64 value from the stream that was stored optimized.
//        /// </summary>
//        /// <returns>An Int64 value.</returns>
//        public long ReadOptimizedInt64()
//        {
//            long result = 0;
//            var bitShift = 0;

//            while (true)
//            {
//                var nextByte = m_reader.ReadByte();

//                result |= ((long)nextByte & 0x7f) << bitShift;
//                bitShift += 7;

//                if ((nextByte & 0x80) == 0) return result;
//            }
//        }

//        public object[] ReadOptimizedObjectArray()
//        {
//            var length = ReadOptimizedInt32();
//            var result = new object[length];

//            for (var i = 0; i < result.Length; i++)
//            {
//                result[i] =  m_reader.ReadObject();
//            }
//            return result;
//        }

//        /// <summary>
//        ///   Returns a pair of object[] arrays from the stream that were stored optimized.
//        /// </summary>
//        /// <returns>A pair of object[] arrays.</returns>
//        //public void ReadOptimizedObjectArrayPair(out object[] values1, out object[] values2)
//        //{
//        //    values1 = ReadOptimizedObjectArray(null);
//        //    values2 = new object[values1.Length];

//        //    for (var i = 0; i < values2.Length; i++)
//        //    {
//        //        var serializedType = (EnumSerializedType)m_reader.ReadByte();

//        //        switch (serializedType)
//        //        {
//        //            case EnumSerializedType.DuplicateValueSequenceType:
//        //                values2[i] = values1[i];
//        //                var duplicateValueCount = ReadOptimizedInt32();

//        //                while (duplicateValueCount-- > 0)
//        //                {
//        //                    values2[++i] = values1[i];
//        //                }

//        //                break;

//        //            case EnumSerializedType.DuplicateValueType:
//        //                values2[i] = values1[i];

//        //                break;

//        //            case EnumSerializedType.NullSequenceType:
//        //                i += ReadOptimizedInt32();

//        //                break;

//        //            case EnumSerializedType.DBNullSequenceType:
//        //                values2[i] = DBNull.Value;
//        //                var duplicates = ReadOptimizedInt32();

//        //                while (duplicates-- > 0)
//        //                {
//        //                    values2[++i] = DBNull.Value;
//        //                }

//        //                break;

//        //            default:
//        //                if (serializedType != EnumSerializedType.NullType)
//        //                {
//        //                    values2[i] = ProcessObject(serializedType);
//        //                }

//        //                break;
//        //        }
//        //    }
//        //}

//        /// <summary>
//        ///   Returns a string value from the stream that was stored optimized.
//        /// </summary>
//        /// <returns>A string value.</returns>
//        public string ReadOptimizedString()
//        {
//            var typeCode = ReadTypeCode();

//            if (typeCode < EnumSerializedType.NullType)
//            {
//                return ReadTokenizedString((int)typeCode);
//            }

//            switch (typeCode)
//            {
//                case EnumSerializedType.NullType:
//                    return null;
//                case EnumSerializedType.YStringType:
//                    return "Y";
//                case EnumSerializedType.NStringType:
//                    return "N";
//                case EnumSerializedType.SingleCharStringType:
//                    return Char.ToString(m_reader.ReadChar());
//                case EnumSerializedType.SingleSpaceType:
//                    return " ";
//                case EnumSerializedType.EmptyStringType:
//                    return string.Empty;

//                default:
//                    throw new InvalidOperationException("Unrecognized TypeCode");
//            }
//        }

//        /// <summary>
//        ///   Returns a TimeSpan value from the stream that was stored optimized.
//        /// </summary>
//        /// <returns>A TimeSpan value.</returns>
//        public TimeSpan ReadOptimizedTimeSpan()
//        {
//            return DecodeTimeSpan(m_reader.ReadByte());
//        }

//        /// <summary>
//        ///   Returns a Type from the stream.
//        /// 
//        ///   Throws an exception if the Type cannot be found.
//        /// </summary>
//        /// <returns>A Type instance.</returns>
//        public Type ReadOptimizedType()
//        {
//            return ReadOptimizedType(true);
//        }

//        /// <summary>
//        ///   Returns a Type from the stream.
//        /// 
//        ///   Throws an exception if the Type cannot be found and throwOnError is true.
//        /// </summary>
//        /// <returns>A Type instance.</returns>
//        public Type ReadOptimizedType(bool throwOnError)
//        {
//            int intType = ReadOptimizedInt32();
//            return SerializerHelper.KnownTypes[intType];
//            //return Type.GetType(ReadOptimizedString(), throwOnError);
//        }

//        /// <summary>
//        ///   Returns a UInt16 value from the stream that was stored optimized.
//        /// </summary>
//        /// <returns>A UInt16 value.</returns>
//        [CLSCompliant(false)]
//        public ushort ReadOptimizedUInt16()
//        {
//            return (ushort)ReadOptimizedUInt32();
//        }

//        /// <summary>
//        ///   Returns a UInt32 value from the stream that was stored optimized.
//        /// </summary>
//        /// <returns>A UInt32 value.</returns>
//        [CLSCompliant(false)]
//        public uint ReadOptimizedUInt32()
//        {
//            uint result = 0;
//            var bitShift = 0;

//            while (true)
//            {
//                var nextByte = m_reader.ReadByte();

//                result |= ((uint)nextByte & 0x7f) << bitShift;
//                bitShift += 7;

//                if ((nextByte & 0x80) == 0) return result;
//            }
//        }

//        /// <summary>
//        ///   Returns a UInt64 value from the stream that was stored optimized.
//        /// </summary>
//        /// <returns>A UInt64 value.</returns>
//        [CLSCompliant(false)]
//        public ulong ReadOptimizedUInt64()
//        {
//            ulong result = 0;
//            var bitShift = 0;

//            while (true)
//            {
//                var nextByte = m_reader.ReadByte();

//                result |= ((ulong)nextByte & 0x7f) << bitShift;
//                bitShift += 7;

//                if ((nextByte & 0x80) == 0) return result;
//            }
//        }

//        /// <summary>
//        ///   Returns a typed array from the stream.
//        /// </summary>
//        /// <returns>A typed array.</returns>
//        //public Array ReadTypedArray()
//        //{
//        //    return (Array)ProcessArrayTypes(ReadTypeCode(), null);
//        //}

//        /// <summary>
//        ///   Returns a new, simple generic dictionary populated with keys and values from the stream.
//        /// </summary>
//        /// <typeparam name = "K">The key Type.</typeparam>
//        /// <typeparam name = "V">The value Type.</typeparam>
//        /// <returns>A new, simple, populated generic Dictionary.</returns>
//        //public Dictionary<K, V> ReadDictionary<K, V>()
//        //{
//        //    var result = new Dictionary<K, V>();

//        //    ReadDictionary(result);

//        //    return result;
//        //}


//        /// <summary>
//        ///   Populates a pre-existing generic dictionary with keys and values from the stream.
//        ///   This allows a generic dictionary to be created without using the default constructor.
//        /// </summary>
//        /// <typeparam name = "K">The key Type.</typeparam>
//        /// <typeparam name = "V">The value Type.</typeparam>
//        //public void ReadDictionary<K, V>(Dictionary<K, V> dictionary)
//        //{
//        //    var keys = (K[])ProcessArrayTypes(ReadTypeCode(), typeof(K));
//        //    var values = (V[])ProcessArrayTypes(ReadTypeCode(), typeof(V));

//        //    if (dictionary == null)
//        //    {
//        //        dictionary = new Dictionary<K, V>(keys.Length);
//        //    }

//        //    for (var i = 0; i < keys.Length; i++)
//        //    {
//        //        dictionary.Add(keys[i], values[i]);
//        //    }
//        //}

//        /// <summary>
//        ///   Returns a generic List populated with values from the stream.
//        /// </summary>
//        /// <typeparam name = "T">The list Type.</typeparam>
//        /// <returns>A new generic List.</returns>
//        //public List<T> ReadList<T>()
//        //{
//        //    return new List<T>((T[])ProcessArrayTypes(ReadTypeCode(), typeof(T)));
//        //}

//        /// <summary>
//        ///   Returns a Nullable struct from the stream.
//        ///   The value returned must be cast to the correct Nullable type.
//        ///   Synonym for ReadObject();
//        /// </summary>
//        /// <returns>A struct value or null</returns>
//        public ValueType ReadNullable()
//        {
//            return (ValueType)ReadObject();
//        }

//        /// <summary>
//        ///   Returns a Nullable Boolean from the stream.
//        /// </summary>
//        /// <returns>A Nullable Boolean.</returns>
//        public Boolean? ReadNullableBoolean()
//        {
//            return (bool?)ReadObject();
//        }

//        /// <summary>
//        ///   Returns a Nullable Byte from the stream.
//        /// </summary>
//        /// <returns>A Nullable Byte.</returns>
//        public Byte? ReadNullableByte()
//        {
//            return (byte?)ReadObject();
//        }

//        /// <summary>
//        ///   Returns a Nullable Char from the stream.
//        /// </summary>
//        /// <returns>A Nullable Char.</returns>
//        public Char? ReadNullableChar()
//        {
//            return (char?)ReadObject();
//        }

//        /// <summary>
//        ///   Returns a Nullable DateTime from the stream.
//        /// </summary>
//        /// <returns>A Nullable DateTime.</returns>
//        public DateTime? ReadNullableDateTime()
//        {
//            return (DateTime?)ReadObject();
//        }

//        /// <summary>
//        ///   Returns a Nullable Decimal from the stream.
//        /// </summary>
//        /// <returns>A Nullable Decimal.</returns>
//        public Decimal? ReadNullableDecimal()
//        {
//            return (decimal?)ReadObject();
//        }

//        /// <summary>
//        ///   Returns a Nullable Double from the stream.
//        /// </summary>
//        /// <returns>A Nullable Double.</returns>
//        public Double? ReadNullableDouble()
//        {
//            return (double?)ReadObject();
//        }

//        /// <summary>
//        ///   Returns a Nullable Guid from the stream.
//        /// </summary>
//        /// <returns>A Nullable Guid.</returns>
//        public Guid? ReadNullableGuid()
//        {
//            return (Guid?)ReadObject();
//        }

//        /// <summary>
//        ///   Returns a Nullable Int16 from the stream.
//        /// </summary>
//        /// <returns>A Nullable Int16.</returns>
//        public Int16? ReadNullableInt16()
//        {
//            return (short?)ReadObject();
//        }

//        /// <summary>
//        ///   Returns a Nullable Int32 from the stream.
//        /// </summary>
//        /// <returns>A Nullable Int32.</returns>
//        public Int32? ReadNullableInt32()
//        {
//            return (int?)ReadObject();
//        }

//        /// <summary>
//        ///   Returns a Nullable Int64 from the stream.
//        /// </summary>
//        /// <returns>A Nullable Int64.</returns>
//        public Int64? ReadNullableInt64()
//        {
//            return (long?)ReadObject();
//        }

//        /// <summary>
//        ///   Returns a Nullable SByte from the stream.
//        /// </summary>
//        /// <returns>A Nullable SByte.</returns>
//        [CLSCompliant(false)]
//        public SByte? ReadNullableSByte()
//        {
//            return (sbyte?)ReadObject();
//        }

//        /// <summary>
//        ///   Returns a Nullable Single from the stream.
//        /// </summary>
//        /// <returns>A Nullable Single.</returns>
//        public Single? ReadNullableSingle()
//        {
//            return (float?)ReadObject();
//        }

//        /// <summary>
//        ///   Returns a Nullable TimeSpan from the stream.
//        /// </summary>
//        /// <returns>A Nullable TimeSpan.</returns>
//        public TimeSpan? ReadNullableTimeSpan()
//        {
//            return (TimeSpan?)ReadObject();
//        }

//        /// <summary>
//        ///   Returns a Nullable UInt16 from the stream.
//        /// </summary>
//        /// <returns>A Nullable UInt16.</returns>
//        [CLSCompliant(false)]
//        public UInt16? ReadNullableUInt16()
//        {
//            return (ushort?)ReadObject();
//        }

//        /// <summary>
//        ///   Returns a Nullable UInt32 from the stream.
//        /// </summary>
//        /// <returns>A Nullable UInt32.</returns>
//        [CLSCompliant(false)]
//        public UInt32? ReadNullableUInt32()
//        {
//            return (uint?)ReadObject();
//        }

//        /// <summary>
//        ///   Returns a Nullable UInt64 from the stream.
//        /// </summary>
//        /// <returns>A Nullable UInt64.</returns>
//        [CLSCompliant(false)]
//        public UInt64? ReadNullableUInt64()
//        {
//            return (ulong?)ReadObject();
//        }

//        /// <summary>
//        ///   Returns a Byte[] from the stream.
//        /// </summary>
//        /// <returns>A Byte instance; or null.</returns>
//        public byte[] ReadByteArray()
//        {
//            var serializedType = ReadTypeCode();
//            switch (serializedType)
//            {
//                case EnumSerializedType.NullType:
//                    return null;
//                case EnumSerializedType.EmptyTypedArrayType:
//                    return new byte[0];

//                default:
//                    return ReadByteArrayInternal();
//            }
//        }

//        /// <summary>
//        ///   Returns a Double[] from the stream.
//        /// </summary>
//        /// <returns>A Double[] instance; or null.</returns>
//        public double[] ReadDoubleArray()
//        {
//            var readTypeCode = ReadTypeCode();
//            switch (readTypeCode)
//            {
//                case EnumSerializedType.NullType:
//                    return null;
//                case EnumSerializedType.EmptyTypedArrayType:
//                    return new double[0];

//                default:
//                    return ReadDoubleArrayInternal();
//            }
//        }

//        /// <summary>
//        ///   Returns an Int16[] from the stream.
//        /// </summary>
//        /// <returns>An Int16[] instance; or null.</returns>
//        public short[] ReadInt16Array()
//        {
//            var t = ReadTypeCode();

//            switch (t)
//            {
//                case EnumSerializedType.NullType:
//                    return null;
//                case EnumSerializedType.EmptyTypedArrayType:
//                    return new short[0];

//                default:
//                    var optimizeFlags = ReadTypedArrayOptimizeFlags(t);
//                    var result = new short[ReadOptimizedInt32()];

//                    for (var i = 0; i < result.Length; i++)
//                    {
//                        if ((optimizeFlags == null) ||
//                            ((optimizeFlags != m_fullyOptimizableTypedArray) && !optimizeFlags[i]))
//                        {
//                            result[i] = m_reader.ReadInt16();
//                        }
//                        else
//                        {
//                            result[i] = ReadOptimizedInt16();
//                        }
//                    }

//                    return result;
//            }
//        }

//        /// <summary>
//        ///   Returns an object[] or null from the stream.
//        /// </summary>
//        /// <returns>A DateTime value.</returns>
//        public object[] ReadObjectArray()
//        {
//            var typeCode = ReadTypeCode();
//            switch (typeCode)
//            {
//                case EnumSerializedType.NullType:
//                    return null;
//                case EnumSerializedType.EmptyObjectArrayType:
//                    return new object[0];
//                default:
//                    return ReadOptimizedObjectArray();
//            }
//        }

//        /// <summary>
//        ///   Returns a Single[] from the stream.
//        /// </summary>
//        /// <returns>A Single[] instance; or null.</returns>
//        //public float[] ReadSingleArray()
//        //{
//        //    switch (ReadTypeCode())
//        //    {
//        //        case EnumSerializedType.NullType:
//        //            return null;
//        //        case EnumSerializedType.EmptyTypedArrayType:
//        //            return new float[0];

//        //        default:
//        //            return ReadSingleArrayInternal();
//        //    }
//        //}

//        ///// <summary>
//        /////   Returns a string[] or null from the stream.
//        ///// </summary>
//        ///// <returns>An string[] instance.</returns>
//        //public string[] ReadStringArray()
//        //{
//        //    return (string[])ReadObjectArray(typeof(string));
//        //}

//        /// <summary>
//        ///   Returns a UInt16[] from the stream.
//        /// </summary>
//        /// <returns>A UInt16[] instance; or null.</returns>
//        [CLSCompliant(false)]
//        public ushort[] ReadUInt16Array()
//        {
//            var typeCode = ReadTypeCode();

//            switch (typeCode)
//            {
//                case EnumSerializedType.NullType:
//                    return null;
//                case EnumSerializedType.EmptyTypedArrayType:
//                    return new ushort[0];

//                default:
//                    var optimizeFlags = ReadTypedArrayOptimizeFlags(typeCode);
//                    var result = new ushort[ReadOptimizedUInt32()];

//                    for (var i = 0; i < result.Length; i++)
//                    {
//                        if ((optimizeFlags == null) ||
//                            ((optimizeFlags != m_fullyOptimizableTypedArray) && !optimizeFlags[i]))
//                        {
//                            result[i] = (ushort)m_reader.ReadInt32();
//                        }
//                        else
//                        {
//                            result[i] = ReadOptimizedUInt16();
//                        }
//                    }

//                    return result;
//            }
//        }

//        /// <summary>
//        ///   Returns a Boolean[] from the stream.
//        /// </summary>
//        /// <returns>A Boolean[] instance; or null.</returns>
//        public bool[] ReadBooleanArray()
//        {
//            switch (ReadTypeCode())
//            {
//                case EnumSerializedType.NullType:
//                    return null;
//                case EnumSerializedType.EmptyTypedArrayType:
//                    return new bool[0];

//                default:
//                    return ReadBooleanArrayInternal();
//            }
//        }

//        /// <summary>
//        ///   Returns a DateTime[] from the stream.
//        /// </summary>
//        /// <returns>A DateTime[] instance; or null.</returns>
//        public DateTime[] ReadDateTimeArray()
//        {
//            var typeCode = ReadTypeCode();
//            switch (typeCode)
//            {
//                case EnumSerializedType.NullType:
//                    return null;
//                case EnumSerializedType.EmptyTypedArrayType:
//                    return new DateTime[0];

//                default:
//                    var optimizeFlags = ReadTypedArrayOptimizeFlags(typeCode);
//                    var result = new DateTime[ReadOptimizedInt32()];

//                    for (var i = 0; i < result.Length; i++)
//                    {
//                        if ((optimizeFlags == null) ||
//                            ((optimizeFlags != m_fullyOptimizableTypedArray) && !optimizeFlags[i]))
//                        {
//                            result[i] = m_reader.ReadDateTime();
//                        }
//                        else
//                        {
//                            result[i] = ReadOptimizedDateTime();
//                        }
//                    }

//                    return result;
//            }
//        }

//        /// <summary>
//        ///   Returns a Decimal[] from the stream.
//        /// </summary>
//        /// <returns>A Decimal[] instance; or null.</returns>
//        public decimal[] ReadDecimalArray()
//        {
//            switch (ReadTypeCode())
//            {
//                case EnumSerializedType.NullType:
//                    return null;
//                case EnumSerializedType.EmptyTypedArrayType:
//                    return new decimal[0];

//                default:
//                    return ReadDecimalArrayInternal();
//            }
//        }

//        /// <summary>
//        ///   Returns an Int32[] from the stream.
//        /// </summary>
//        /// <returns>An Int32[] instance; or null.</returns>
//        public int[] ReadInt32Array()
//        {
//            var typeCode = ReadTypeCode();

//            switch (typeCode)
//            {
//                case EnumSerializedType.NullType:
//                    return null;
//                case EnumSerializedType.EmptyTypedArrayType:
//                    return new int[0];

//                default:
//                    var optimizeFlags = ReadTypedArrayOptimizeFlags(typeCode);
//                    var result = new int[ReadOptimizedInt32()];

//                    for (var i = 0; i < result.Length; i++)
//                    {
//                        if ((optimizeFlags == null) ||
//                            ((optimizeFlags != m_fullyOptimizableTypedArray) && !optimizeFlags[i]))
//                        {
//                            result[i] = m_reader.ReadInt32();
//                        }
//                        else
//                        {
//                            result[i] = ReadOptimizedInt32();
//                        }
//                    }

//                    return result;
//            }
//        }

//        /// <summary>
//        ///   Returns an Int64[] from the stream.
//        /// </summary>
//        /// <returns>An Int64[] instance; or null.</returns>
//        public long[] ReadInt64Array()
//        {
//            var typeCode = ReadTypeCode();

//            switch (typeCode)
//            {
//                case EnumSerializedType.NullType:
//                    return null;
//                case EnumSerializedType.EmptyTypedArrayType:
//                    return new long[0];

//                default:
//                    var optimizeFlags = ReadTypedArrayOptimizeFlags(typeCode);
//                    var result = new long[ReadOptimizedInt64()];

//                    for (var i = 0; i < result.Length; i++)
//                    {
//                        if ((optimizeFlags == null) ||
//                            ((optimizeFlags != m_fullyOptimizableTypedArray) && !optimizeFlags[i]))
//                        {
//                            result[i] = m_reader.ReadInt64();
//                        }
//                        else
//                        {
//                            result[i] = ReadOptimizedInt64();
//                        }
//                    }

//                    return result;
//            }
//        }

//        /// <summary>
//        ///   Returns a string[] from the stream that was stored optimized.
//        /// </summary>
//        /// <returns>An string[] instance.</returns>
//        //public string[] ReadOptimizedStringArray()
//        //{
//        //    return (string[])ReadOptimizedObjectArray(typeof(string));
//        //}

//        /// <summary>
//        ///   Returns a TimeSpan[] from the stream.
//        /// </summary>
//        /// <returns>A TimeSpan[] instance; or null.</returns>
//        public TimeSpan[] ReadTimeSpanArray()
//        {
//            var typeCode = ReadTypeCode();

//            switch (typeCode)
//            {
//                case EnumSerializedType.NullType:
//                    return null;
//                case EnumSerializedType.EmptyTypedArrayType:
//                    return new TimeSpan[0];

//                default:
//                    var optimizeFlags = ReadTypedArrayOptimizeFlags(typeCode);
//                    var result = new TimeSpan[ReadOptimizedInt32()];

//                    for (var i = 0; i < result.Length; i++)
//                    {
//                        if ((optimizeFlags == null) ||
//                            ((optimizeFlags != m_fullyOptimizableTypedArray) && !optimizeFlags[i]))
//                        {
//                            result[i] = ReadTimeSpan();
//                        }
//                        else
//                        {
//                            result[i] = ReadOptimizedTimeSpan();
//                        }
//                    }

//                    return result;
//            }
//        }

//        /// <summary>
//        ///   Returns a UInt[] from the stream.
//        /// </summary>
//        /// <returns>A UInt[] instance; or null.</returns>
//        [CLSCompliant(false)]
//        public uint[] ReadUInt32Array()
//        {
//            var typeCode = ReadTypeCode();

//            switch (typeCode)
//            {
//                case EnumSerializedType.NullType:
//                    return null;
//                case EnumSerializedType.EmptyTypedArrayType:
//                    return new uint[0];

//                default:
//                    var optimizeFlags = ReadTypedArrayOptimizeFlags(typeCode);
//                    var result = new uint[ReadOptimizedUInt32()];

//                    for (var i = 0; i < result.Length; i++)
//                    {
//                        if ((optimizeFlags == null) ||
//                            ((optimizeFlags != m_fullyOptimizableTypedArray) && !optimizeFlags[i]))
//                        {
//                            result[i] = (uint)m_reader.ReadInt32();
//                        }
//                        else
//                        {
//                            result[i] = ReadOptimizedUInt32();
//                        }
//                    }

//                    return result;
//            }
//        }

//        /// <summary>
//        ///   Returns a UInt64[] from the stream.
//        /// </summary>
//        /// <returns>A UInt64[] instance; or null.</returns>
//        [CLSCompliant((false))]
//        public ulong[] ReadUInt64Array()
//        {
//            var typeCode = ReadTypeCode();

//            switch (typeCode)
//            {
//                case EnumSerializedType.NullType:
//                    return null;
//                case EnumSerializedType.EmptyTypedArrayType:
//                    return new ulong[0];

//                default:
//                    var optimizeFlags = ReadTypedArrayOptimizeFlags(typeCode);
//                    var result = new ulong[ReadOptimizedInt64()];

//                    for (var i = 0; i < result.Length; i++)
//                    {
//                        if ((optimizeFlags == null) ||
//                            ((optimizeFlags != m_fullyOptimizableTypedArray) && !optimizeFlags[i]))
//                        {
//                            result[i] = (ulong)m_reader.ReadInt64();
//                        }
//                        else
//                        {
//                            result[i] = ReadOptimizedUInt64();
//                        }
//                    }

//                    return result;
//            }
//        }

//        /// <summary>
//        ///   Returns a Boolean[] from the stream.
//        /// </summary>
//        /// <returns>A Boolean[] instance; or null.</returns>
//        public bool[] ReadOptimizedBooleanArray()
//        {
//            return ReadBooleanArray();
//        }

//        /// <summary>
//        ///   Returns a DateTime[] from the stream.
//        /// </summary>
//        /// <returns>A DateTime[] instance; or null.</returns>
//        public DateTime[] ReadOptimizedDateTimeArray()
//        {
//            return ReadDateTimeArray();
//        }

//        /// <summary>
//        ///   Returns a Decimal[] from the stream.
//        /// </summary>
//        /// <returns>A Decimal[] instance; or null.</returns>
//        public decimal[] ReadOptimizedDecimalArray()
//        {
//            return ReadDecimalArray();
//        }

//        /// <summary>
//        ///   Returns a Int16[] from the stream.
//        /// </summary>
//        /// <returns>An Int16[] instance; or null.</returns>
//        public short[] ReadOptimizedInt16Array()
//        {
//            return ReadInt16Array();
//        }

//        /// <summary>
//        ///   Returns a Int32[] from the stream.
//        /// </summary>
//        /// <returns>An Int32[] instance; or null.</returns>
//        public int[] ReadOptimizedInt32Array()
//        {
//            return ReadInt32Array();
//        }

//        /// <summary>
//        ///   Returns a Int64[] from the stream.
//        /// </summary>
//        /// <returns>A Int64[] instance; or null.</returns>
//        public long[] ReadOptimizedInt64Array()
//        {
//            return ReadInt64Array();
//        }

//        /// <summary>
//        ///   Returns a TimeSpan[] from the stream.
//        /// </summary>
//        /// <returns>A TimeSpan[] instance; or null.</returns>
//        public TimeSpan[] ReadOptimizedTimeSpanArray()
//        {
//            return ReadTimeSpanArray();
//        }

//        /// <summary>
//        ///   Returns a UInt16[] from the stream.
//        /// </summary>
//        /// <returns>A UInt16[] instance; or null.</returns>
//        [CLSCompliant(false)]
//        public ushort[] ReadOptimizedUInt16Array()
//        {
//            return ReadUInt16Array();
//        }

//        /// <summary>
//        ///   Returns a UInt32[] from the stream.
//        /// </summary>
//        /// <returns>A UInt32[] instance; or null.</returns>
//        [CLSCompliant(false)]
//        public uint[] ReadOptimizedUInt32Array()
//        {
//            return ReadUInt32Array();
//        }

//        /// <summary>
//        ///   Returns a UInt64[] from the stream.
//        /// </summary>
//        /// <returns>A UInt64[] instance; or null.</returns>
//        [CLSCompliant(false)]
//        public ulong[] ReadOptimizedUInt64Array()
//        {
//            return ReadUInt64Array();
//        }

//        /// <summary>
//        ///   Allows an existing object, implementing IOwnedDataSerializable, to 
//        ///   retrieve its owned data from the stream.
//        /// </summary>
//        /// <param name = "target">Any IOwnedDataSerializable object.</param>
//        /// <param name = "context">An optional, arbitrary object to allow context to be provided.</param>
//        //public void ReadOwnedData(IOwnedDataSerializable target, object context)
//        //{
//        //    target.DeserializeOwnedData(this, context);
//        //}

//        /// <summary>
//        ///   Returns the object associated with the object token read next from the stream.
//        /// </summary>
//        /// <returns>An object.</returns>
//        public object ReadTokenizedObject()
//        {
//            var token = ReadOptimizedInt32();

//            if (token >= m_objectTokenList.Count)
//            {
//                var tokenizedObject = ReadObject();

//                m_objectTokenList.Add(tokenizedObject);

//                return tokenizedObject;
//            }

//            return m_objectTokenList[token];
//        }

//        /// <summary>
//        ///   Initializes the token tables.
//        /// </summary>
//        /// <param name = "stringTokenTablePresize">The string token table presize.</param>
//        /// <param name = "objectTokenTablePresize">The object token table presize.</param>
//        private void InitializeTokenTables(int stringTokenTablePresize, int objectTokenTablePresize)
//        {
//            m_stringTokenList = new List<string>(stringTokenTablePresize);
//            m_objectTokenList = new List<object>(objectTokenTablePresize);
//        }

//        /// <summary>
//        ///   Returns a TimeSpan decoded from packed data.
//        ///   This routine is called from ReadOptimizedDateTime() and ReadOptimizedTimeSpan().
//        ///   <remarks>
//        ///     This routine uses a parameter to allow ReadOptimizedDateTime() to 'peek' at the
//        ///     next byte and extract the DateTimeKind from bits one and two (IsNegative and HasDays)
//        ///     which are never set for a Time portion of a DateTime.
//        ///   </remarks>
//        /// </summary>
//        /// <param name = "initialByte">The first of two always-present bytes.</param>
//        /// <returns>A decoded TimeSpan</returns>
//        private TimeSpan DecodeTimeSpan(byte initialByte)
//        {
//            var packedData = new BitVector32(initialByte | (m_reader.ReadByte() << 8)); // Read first two bytes
//            var hasTime = packedData[SerializationWriter.HasTimeSection] == 1;
//            var hasSeconds = packedData[SerializationWriter.HasSecondsSection] == 1;
//            var hasMilliseconds = packedData[SerializationWriter.HasMillisecondsSection] == 1;
//            long ticks = 0;

//            if (hasMilliseconds)
//            {
//                packedData = new BitVector32(packedData.Data | (m_reader.ReadByte() << 16) |
//                    (m_reader.ReadByte() << 24));
//            }
//            else if (hasTime && hasSeconds)
//            {
//                packedData = new BitVector32(packedData.Data |
//                    (m_reader.ReadByte() << 16));
//            }

//            if (hasTime)
//            {
//                ticks += packedData[SerializationWriter.HoursSection] * TimeSpan.TicksPerHour;
//                ticks += packedData[SerializationWriter.MinutesSection] * TimeSpan.TicksPerMinute;
//            }

//            if (hasSeconds)
//            {
//                ticks += packedData[(!hasTime && !hasMilliseconds)
//                                        ? SerializationWriter.MinutesSection
//                                        : SerializationWriter.SecondsSection] * TimeSpan.TicksPerSecond;
//            }

//            if (hasMilliseconds)
//            {
//                ticks += packedData[SerializationWriter.MillisecondsSection] * TimeSpan.TicksPerMillisecond;
//            }

//            if (packedData[SerializationWriter.HasDaysSection] == 1)
//            {
//                ticks += ReadOptimizedInt32() * TimeSpan.TicksPerDay;
//            }

//            if (packedData[SerializationWriter.IsNegativeSection] == 1)
//            {
//                ticks = -ticks;
//            }

//            return new TimeSpan(ticks);
//        }

//        /// <summary>
//        ///   Creates a BitArray representing which elements of a typed array
//        ///   are serializable.
//        /// </summary>
//        /// <param name = "serializedType">The type of typed array.</param>
//        /// <returns>A BitArray denoting which elements are serializable.</returns>
//        private BitArray ReadTypedArrayOptimizeFlags(EnumSerializedType serializedType)
//        {
//            switch (serializedType)
//            {
//                case EnumSerializedType.FullyOptimizedTypedArrayType:
//                    return m_fullyOptimizableTypedArray;
//                case EnumSerializedType.PartiallyOptimizedTypedArrayType:
//                    return ReadOptimizedBitArray();

//                default:
//                    return null;
//            }
//        }

//        /// <summary>
//        ///   Returns an object based on supplied EnumSerializedType.
//        /// </summary>
//        /// <returns>An object instance.</returns>
//        //private object ProcessObject(EnumSerializedType typeCode)
//        //{
//        //    if (typeCode < EnumSerializedType.NullType) return ReadTokenizedString((int)typeCode);

//        //    switch (typeCode)
//        //    {
//        //        case EnumSerializedType.NullType:
//        //            return null;
//        //        case EnumSerializedType.Int32Type:
//        //            return m_reader.ReadInt32();
//        //        case EnumSerializedType.OptimizedInt32Type:
//        //            return ReadOptimizedInt32();
//        //        case EnumSerializedType.ZeroInt32Type:
//        //            return 0;
//        //        case EnumSerializedType.EmptyStringType:
//        //            return string.Empty;
//        //        case EnumSerializedType.BooleanFalseType:
//        //            return false;
//        //        case EnumSerializedType.OptimizedInt32NegativeType:
//        //            return -ReadOptimizedInt32() - 1;
//        //        case EnumSerializedType.DecimalType:
//        //            return ReadOptimizedDecimal();
//        //        case EnumSerializedType.ZeroDecimalType:
//        //            return (Decimal)0;
//        //        case EnumSerializedType.YStringType:
//        //            return "Y";
//        //        case EnumSerializedType.DateTimeType:
//        //            return m_reader.ReadDateTime();
//        //        case EnumSerializedType.OptimizedDateTimeType:
//        //            return ReadOptimizedDateTime();
//        //        case EnumSerializedType.SingleCharStringType:
//        //            return Char.ToString(m_reader.ReadChar());
//        //        case EnumSerializedType.SingleSpaceType:
//        //            return " ";
//        //        case EnumSerializedType.OneInt32Type:
//        //            return 1;
//        //        case EnumSerializedType.OptimizedInt16Type:
//        //            return ReadOptimizedInt16();
//        //        case EnumSerializedType.OptimizedInt16NegativeType:
//        //            return (short)(-ReadOptimizedInt16() - 1);
//        //        case EnumSerializedType.OneDecimalType:
//        //            return (Decimal)1;
//        //        case EnumSerializedType.BooleanTrueType:
//        //            return true;
//        //        case EnumSerializedType.NStringType:
//        //            return "N";
//        //        case EnumSerializedType.DBNullType:
//        //            return DBNull.Value;
//        //        case EnumSerializedType.ObjectArrayType:
//        //            return ReadOptimizedObjectArray();
//        //        case EnumSerializedType.EmptyObjectArrayType:
//        //            return new object[0];
//        //        case EnumSerializedType.MinusOneInt32Type:
//        //            return -1;
//        //        case EnumSerializedType.MinusOneInt64Type:
//        //            return (Int64)(-1);
//        //        case EnumSerializedType.MinusOneInt16Type:
//        //            return (Int16)(-1);
//        //        case EnumSerializedType.MinDateTimeType:
//        //            return DateTime.MinValue;
//        //        case EnumSerializedType.EmptyGuidType:
//        //            return Guid.Empty;
//        //        case EnumSerializedType.TimeSpanType:
//        //            return ReadTimeSpan();
//        //        case EnumSerializedType.MaxDateTimeType:
//        //            return DateTime.MaxValue;
//        //        case EnumSerializedType.ZeroTimeSpanType:
//        //            return TimeSpan.Zero;
//        //        case EnumSerializedType.OptimizedTimeSpanType:
//        //            return ReadOptimizedTimeSpan();
//        //        case EnumSerializedType.DoubleType:
//        //            return m_reader.ReadDouble();
//        //        case EnumSerializedType.ZeroDoubleType:
//        //            return (Double)0;
//        //        case EnumSerializedType.Int64Type:
//        //            return m_reader.ReadInt64();
//        //        case EnumSerializedType.ZeroInt64Type:
//        //            return (Int64)0;
//        //        case EnumSerializedType.OptimizedInt64Type:
//        //            return ReadOptimizedInt64();
//        //        case EnumSerializedType.OptimizedInt64NegativeType:
//        //            return -ReadOptimizedInt64() - 1;
//        //        case EnumSerializedType.Int16Type:
//        //            return m_reader.ReadInt16();
//        //        case EnumSerializedType.ZeroInt16Type:
//        //            return (Int16)0;
//        //        case EnumSerializedType.OneSingleType:
//        //            return (Single)1;
//        //        case EnumSerializedType.SingleType:
//        //            return m_reader.ReadSingle();
//        //        case EnumSerializedType.ZeroSingleType:
//        //            return (Single)0;
//        //        case EnumSerializedType.ByteType:
//        //            return m_reader.ReadByte();
//        //        case EnumSerializedType.ZeroByteType:
//        //            return (Byte)0;
//        //        case EnumSerializedType.OtherType:
//        //            throw new NotImplementedException();
//        //        case EnumSerializedType.UInt16Type:
//        //            return m_reader.ReadInt16();
//        //        case EnumSerializedType.ZeroUInt16Type:
//        //            return (UInt16)0;
//        //        case EnumSerializedType.UInt32Type:
//        //            return m_reader.ReadInt32();
//        //        case EnumSerializedType.ZeroUInt32Type:
//        //            return (UInt32)0;
//        //        case EnumSerializedType.OptimizedUInt32Type:
//        //            return ReadOptimizedUInt32();
//        //        case EnumSerializedType.UInt64Type:
//        //            return m_reader.ReadInt64();
//        //        case EnumSerializedType.ZeroUInt64Type:
//        //            return (UInt64)0;
//        //        case EnumSerializedType.OptimizedUInt64Type:
//        //            return ReadOptimizedUInt64();
//        //        case EnumSerializedType.BitVector32Type:
//        //            return ReadBitVector32();
//        //        case EnumSerializedType.CharType:
//        //            return m_reader.ReadChar();
//        //        case EnumSerializedType.ZeroCharType:
//        //            return (Char)0;
//        //        case EnumSerializedType.SByteType:
//        //            return m_reader.ReadByte();
//        //        case EnumSerializedType.ZeroSByteType:
//        //            return (SByte)0;
//        //        case EnumSerializedType.OneByteType:
//        //            return (Byte)1;
//        //        case EnumSerializedType.OneDoubleType:
//        //            return (Double)1;
//        //        case EnumSerializedType.OneCharType:
//        //            return (Char)1;
//        //        case EnumSerializedType.OneInt16Type:
//        //            return (Int16)1;
//        //        case EnumSerializedType.OneInt64Type:
//        //            return (Int64)1;
//        //        case EnumSerializedType.OneUInt16Type:
//        //            return (UInt16)1;
//        //        case EnumSerializedType.OptimizedUInt16Type:
//        //            return ReadOptimizedUInt16();
//        //        case EnumSerializedType.OneUInt32Type:
//        //            return (UInt32)1;
//        //        case EnumSerializedType.OneUInt64Type:
//        //            return (UInt64)1;
//        //        case EnumSerializedType.OneSByteType:
//        //            return (SByte)1;
//        //        case EnumSerializedType.BitArrayType:
//        //            return ReadOptimizedBitArray();
//        //        case EnumSerializedType.TypeType:
//        //            int intType = ReadOptimizedInt32();
//        //            Type type = SerializerHelper.KnownTypes[intType];
//        //            return type;

//        //        case EnumSerializedType.ArrayListType:
//        //            return ReadOptimizedArrayList();
//        //        case EnumSerializedType.SingleInstanceType:
//        //            try
//        //            {
//        //                return Activator.CreateInstance(Type.GetType(ReadStringDirect()), true);
//        //            }
//        //            catch
//        //            {
//        //                // cannot recover from this, swallow.
//        //                return null;
//        //            }

//        //        case EnumSerializedType.OwnedDataSerializableAndRecreatableType:
//        //            {
//        //                var result = Activator.CreateInstance(ReadOptimizedType());

//        //                //ReadOwnedData((IOwnedDataSerializable)result, null);

//        //                return result;
//        //            }

//        //        case EnumSerializedType.OptimizedEnumType:
//        //            {
//        //                var enumType = ReadOptimizedType();
//        //                var underlyingType = Enum.GetUnderlyingType(enumType);

//        //                if ((underlyingType == typeof(int)) || (underlyingType == typeof(uint)) ||
//        //                    (underlyingType == typeof(long)) || (underlyingType == typeof(ulong)))
//        //                {
//        //                    return Enum.ToObject(enumType, ReadOptimizedUInt64());
//        //                }

//        //                return Enum.ToObject(enumType, m_reader.ReadInt64());
//        //            }

//        //        case EnumSerializedType.EnumType:
//        //            {
//        //                var enumType = ReadOptimizedType();
//        //                var underlyingType = Enum.GetUnderlyingType(enumType);

//        //                if (underlyingType == typeof(Int32)) return Enum.ToObject(enumType, m_reader.ReadInt32());
//        //                if (underlyingType == typeof(Byte)) return Enum.ToObject(enumType, m_reader.ReadByte());
//        //                if (underlyingType == typeof(Int16)) return Enum.ToObject(enumType, m_reader.ReadInt16());
//        //                if (underlyingType == typeof(UInt32)) return Enum.ToObject(enumType, m_reader.ReadInt32());
//        //                if (underlyingType == typeof(Int64)) return Enum.ToObject(enumType, m_reader.ReadInt64());
//        //                if (underlyingType == typeof(SByte)) return Enum.ToObject(enumType, m_reader.ReadByte());
//        //                if (underlyingType == typeof(UInt16)) return Enum.ToObject(enumType, m_reader.ReadInt16());

//        //                return Enum.ToObject(enumType, m_reader.ReadInt64());
//        //            }

//        //        case EnumSerializedType.SurrogateHandledType:
//        //            {
//        //                var serializedType = ReadOptimizedType();
//        //                //var typeSurrogate = SerializationWriter.FindSurrogateForType(serializedType);
//        //                return null;
//        //                //return typeSurrogate.Deserialize(this, serializedType);
//        //            }

//        //        default:
//        //            {
//        //                var result = ProcessArrayTypes(typeCode, null);
//        //                if (result != null) return result;

//        //                throw new InvalidOperationException("Unrecognized TypeCode: " + typeCode);
//        //            }
//        //    }
//        //}

//        //private static Type GetTypeFromLookup(string strType)
//        //{
//        //    Type type;
//        //    if (!m_typeLookup.TryGetValue(strType, out type))
//        //    {
//        //        lock (m_typeLock)
//        //        {
//        //            if (!m_typeLookup.TryGetValue(strType, out type))
//        //            {
//        //                type = Type.GetType(strType, false);
//        //                m_typeLookup[strType] = type;
//        //            }
//        //        }
//        //    }
//        //    return type;
//        //}

//        ///// <summary>
//        /////   Determine whether the passed-in type code refers to an array type
//        /////   and deserializes the array if it is.
//        /////   Returns null if not an array type.
//        ///// </summary>
//        ///// <param name = "typeCode">The EnumSerializedType to check.</param>
//        ///// <param name = "defaultElementType">The Type of array element; null if to be read from stream.</param>
//        ///// <returns></returns>
//        //private object ProcessArrayTypes(EnumSerializedType typeCode, Type defaultElementType)
//        //{
//        //    switch (typeCode)
//        //    {
//        //        case EnumSerializedType.StringArrayType:
//        //            return ReadOptimizedStringArray();
//        //        case EnumSerializedType.Int32ArrayType:
//        //            return ReadInt32Array();
//        //        case EnumSerializedType.Int64ArrayType:
//        //            return ReadInt64Array();
//        //        case EnumSerializedType.DecimalArrayType:
//        //            return ReadDecimalArrayInternal();
//        //        case EnumSerializedType.TimeSpanArrayType:
//        //            return ReadTimeSpanArray();
//        //        case EnumSerializedType.UInt32ArrayType:
//        //            return ReadUInt32Array();
//        //        case EnumSerializedType.UInt64ArrayType:
//        //            return ReadUInt64Array();
//        //        case EnumSerializedType.DateTimeArrayType:
//        //            return ReadDateTimeArray();
//        //        case EnumSerializedType.BooleanArrayType:
//        //            return ReadBooleanArrayInternal();
//        //        case EnumSerializedType.ByteArrayType:
//        //            return ReadByteArrayInternal();
//        //        case EnumSerializedType.CharArrayType:
//        //            throw new NotImplementedException();
//        //        case EnumSerializedType.DoubleArrayType:
//        //            return ReadDoubleArrayInternal();
//        //        case EnumSerializedType.SingleArrayType:
//        //            return ReadSingleArrayInternal();
//        //        case EnumSerializedType.GuidArrayType:
//        //            throw new NotImplementedException();
//        //        case EnumSerializedType.SByteArrayType:
//        //            throw new NotImplementedException();
//        //        case EnumSerializedType.Int16ArrayType:
//        //            return ReadInt16Array();
//        //        case EnumSerializedType.UInt16ArrayType:
//        //            return ReadUInt16Array();
//        //        case EnumSerializedType.EmptyTypedArrayType:
//        //            return Array.CreateInstance(defaultElementType ?? ReadOptimizedType(), 0);
//        //        case EnumSerializedType.OtherTypedArrayType:
//        //            return ReadOptimizedObjectArray(ReadOptimizedType());
//        //        case EnumSerializedType.ObjectArrayType:
//        //            return ReadOptimizedObjectArray(defaultElementType);
//        //        case EnumSerializedType.NonOptimizedTypedArrayType:
//        //        case EnumSerializedType.PartiallyOptimizedTypedArrayType:
//        //        case EnumSerializedType.FullyOptimizedTypedArrayType:
//        //            var optimizeFlags = ReadTypedArrayOptimizeFlags(typeCode);
//        //            var length = ReadOptimizedInt32();

//        //            if (defaultElementType == null)
//        //            {
//        //                defaultElementType = ReadOptimizedType();
//        //            }

//        //            var result = Array.CreateInstance(defaultElementType, length);

//        //            for (var i = 0; i < length; i++)
//        //            {
//        //                if (optimizeFlags == null)
//        //                {
//        //                    result.SetValue(ReadObject(), i);
//        //                }
//        //                else if ((optimizeFlags == m_fullyOptimizableTypedArray) || !optimizeFlags[i])
//        //                {
//        //                    //var value = (IOwnedDataSerializable)Activator.CreateInstance(defaultElementType);

//        //                    //ReadOwnedData(value, null);
//        //                    //result.SetValue(value, i);
//        //                }
//        //            }

//        //            return result;
//        //    }

//        //    return null;
//        //}

//        /// <summary>
//        ///   Returns the string value associated with the string token read next from the stream.
//        /// </summary>
//        /// <returns>A DateTime value.</returns>
//        private string ReadTokenizedString(int bucket)
//        {
//            var stringTokenIndex = (ReadOptimizedInt32() << 7) + bucket;

//            if (stringTokenIndex >= m_stringTokenList.Count)
//            {
//                m_stringTokenList.Add(m_reader.ReadString());
//            }

//            return m_stringTokenList[stringTokenIndex];
//        }

//        /// <summary>
//        ///   Returns the EnumSerializedType read next from the stream.
//        /// </summary>
//        /// <returns>A EnumSerializedType value.</returns>
//        private EnumSerializedType ReadTypeCode()
//        {
//            return (EnumSerializedType)m_reader.ReadByte();
//        }

//        /// <summary>
//        ///   Internal implementation returning a Bool[].
//        /// </summary>
//        /// <returns>A Bool[].</returns>
//        private bool[] ReadBooleanArrayInternal()
//        {
//            var bitArray = ReadOptimizedBitArray();
//            var result = new bool[bitArray.Count];

//            for (var i = 0; i < result.Length; i++)
//            {
//                result[i] = bitArray[i];
//            }

//            return result;
//        }

//        /// <summary>
//        ///   Internal implementation returning a Byte[].
//        /// </summary>
//        /// <returns>A Byte[].</returns>
//        private byte[] ReadByteArrayInternal()
//        {
//            return m_reader.ReadBytes(ReadOptimizedInt32());
//        }

//        /// <summary>
//        ///   Internal implementation returning a Decimal[].
//        /// </summary>
//        /// <returns>A Decimal[].</returns>
//        private decimal[] ReadDecimalArrayInternal()
//        {
//            var result = new decimal[ReadOptimizedInt32()];

//            for (var i = 0; i < result.Length; i++)
//            {
//                result[i] = ReadOptimizedDecimal();
//            }

//            return result;
//        }

//        /// <summary>
//        ///   Internal implementation returning a Double[].
//        /// </summary>
//        /// <returns>A Double[].</returns>
//        private double[] ReadDoubleArrayInternal()
//        {
//            var result = new double[ReadOptimizedInt32()];

//            for (var i = 0; i < result.Length; i++)
//            {
//                result[i] = ReadDouble();
//            }

//            return result;
//        }

//        /// <summary>
//        ///   Internal implementation returning a Single[].
//        /// </summary>
//        /// <returns>A Single[].</returns>
//        private float[] ReadSingleArrayInternal()
//        {
//            var result = new float[ReadOptimizedInt32()];

//            for (var i = 0; i < result.Length; i++)
//            {
//                result[i] = m_reader.ReadSingle();
//            }

//            return result;
//        }

//        #region Class Property declarations

//        /// <summary>
//        ///   Returns the number of bytes or serialized remaining to be processed.
//        ///   Useful for checking that deserialization is complete.
//        /// 
//        ///   Warning: Retrieving the Position in certain stream types can be expensive,
//        ///   e.g. a FileStream, so use sparingly unless known to be a MemoryStream.
//        /// </summary>
//        public int BytesRemaining
//        {

//            get
//            {
//                return m_reader.BytesRemaining;
//            }
//        }

//        #endregion

//        public double ReadDouble()
//        {
//            var serializedType = (EnumSerializedType) m_reader.ReadByte();
//            switch (serializedType)
//            {
//                case EnumSerializedType.DoubleType:
//                    return m_reader.ReadDouble();
//                case EnumSerializedType.OneDoubleType:
//                    return 1;
//                case EnumSerializedType.ZeroDoubleType:
//                    return 0;
//            }
//            throw new NotImplementedException();
//        }

//        public int ReadInt32()
//        {
//            var serializedType = (EnumSerializedType) m_reader.ReadByte();
//            switch (serializedType)
//            {
//                case EnumSerializedType.ZeroInt32Type:
//                    return 0;
//                case EnumSerializedType.MinusOneInt32Type:
//                    return -1;
//                case EnumSerializedType.OneInt32Type:
//                    return 1;
//                case EnumSerializedType.Int32Type:
//                    return m_reader.ReadInt32();
//                case EnumSerializedType.OptimizedInt32Type:
//                    return ReadOptimizedInt32();
//                case EnumSerializedType.OptimizedInt32NegativeType:
//                    return -ReadOptimizedInt32() - 1;
//            }
//            throw new NotImplementedException();
//        }

//        public short ReadInt16()
//        {
//            var serializedType = (EnumSerializedType)m_reader.ReadByte();
//            switch (serializedType)
//            {
//                case EnumSerializedType.MinusOneInt16Type:
//                    return -1;
//                case EnumSerializedType.Int16Type:
//                    return m_reader.ReadInt16();
//                case EnumSerializedType.ZeroInt16Type:
//                    return 0;
//                case EnumSerializedType.OptimizedInt16Type:
//                    return ReadOptimizedInt16();
//                case EnumSerializedType.OptimizedInt16NegativeType:
//                    return (short)(-ReadOptimizedInt16() - 1);
//                case EnumSerializedType.OneInt16Type:
//                    return 1;
//            }
//            throw new NotImplementedException();
//        }

//        public Single ReadSingle()
//        {
//            var serializedType = (EnumSerializedType) m_reader.ReadByte();
//            switch (serializedType)
//            {
//                case EnumSerializedType.OneSingleType:
//                    return 1;
//                case EnumSerializedType.SingleType:
//                    return m_reader.ReadSingle();
//                case EnumSerializedType.ZeroSingleType:
//                    return 0;
//            }
//            throw new NotImplementedException();
//        }

//        public long ReadInt64()
//        {
//            var serializedType = (EnumSerializedType) m_reader.ReadByte();
//            switch (serializedType)
//            {
//                case EnumSerializedType.MinusOneInt64Type:
//                    return -1;
//                case EnumSerializedType.Int64Type:
//                    return m_reader.ReadInt64();
//                case EnumSerializedType.ZeroInt64Type:
//                    return 0;
//                case EnumSerializedType.OptimizedInt64Type:
//                    return ReadOptimizedInt64();
//                case EnumSerializedType.OptimizedInt64NegativeType:
//                    return -ReadOptimizedInt64() - 1;
//                case EnumSerializedType.OneInt64Type:
//                    return 1;
//            }
//            throw new NotImplementedException();
//        }

//        public bool ReadBoolean()
//        {
//            var serializedType = (EnumSerializedType) m_reader.ReadByte();
//            if (serializedType == EnumSerializedType.BooleanFalseType)
//            {
//                return false;
//            }
//            return true;
//        }

//        public byte ReadByte()
//        {
//            return m_reader.ReadByte();
//        }

//        public char ReadChar()
//        {
//            var serializedType = (EnumSerializedType) m_reader.ReadByte();
//            switch (serializedType)
//            {
//                case EnumSerializedType.CharType:
//                    return m_reader.ReadChar();
//                case EnumSerializedType.OneCharType:
//                    return (char)1;
//                case EnumSerializedType.ZeroCharType:
//                    return (char)0;
//            }
//            throw new NotImplementedException();
//        }

//        public Type[] ReadTypeArray()
//        {
//            int intArrayLength = ReadInt32();
//            if (intArrayLength <= 0)
//            {
//                return null;
//            }

//            var values = new Type[intArrayLength];
//            for (int i = 0; i < intArrayLength; i++)
//            {
//                values[i] = ReadType();
//            }
//            return values;
//        }

//    }
//}


