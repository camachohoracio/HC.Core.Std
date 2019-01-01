#region

using System;
using System.Collections.Generic;
using HC.Core.Io.Serialization.Interfaces;

#endregion

namespace HC.Core.Io.Serialization.Types
{
    public static class PrimitiveTypesCache
    {
        #region Properties

        public static List<Type> PrimitiveTypes { get; set; }

        #endregion

        static PrimitiveTypesCache()
        {
            PrimitiveTypes = new List<Type>(new[]
                                                {
                                                    typeof(int),
                                                    typeof(long),
                                                    typeof(double),
                                                    typeof(bool),
                                                    typeof(byte),
                                                    typeof(char),
                                                    typeof(DateTime),
                                                    typeof(string),
                                                    typeof(DayOfWeek)
                                                });
        }

        public static EnumSerializedType GetSerializedPrimitiveType(Type type)
        {
            if (type == typeof(int))
            {
                return EnumSerializedType.Int32Type;
            }
            if (type == typeof(double))
            {
                return EnumSerializedType.DoubleType;
            }
            if (type == typeof(long))
            {
                return EnumSerializedType.Int64Type;
            }
            if (type == typeof(string))
            {
                return EnumSerializedType.StringType;
            }
            if (type == typeof(DateTime))
            {
                return EnumSerializedType.DateTimeType;
            }
            if (type == typeof(char))
            {
                return EnumSerializedType.CharType;
            }
            return EnumSerializedType.ObjectType;
        }

    }
}


