#region

using System;
using System.Text;
using HC.Core.DynamicCompilation;
using HC.Core.Exceptions;
using HC.Core.Io.Serialization.DataStructures;
using HC.Core.Io.Serialization.Types;

#endregion

namespace HC.Core.Io.Serialization.Parsers
{
    public static class ArrayReaderParser
    {
        public static void AddDeserializeArrayComplexObj(
            StringBuilder sb,
            string strProperty,
            Type elementType)
        {
            string strArrTypeName = SerializerParserHelper.GetUniqueTypeName();
            AddDeserializeArrayParse(
                elementType,
                sb,
                strArrTypeName);
            string strElementType;
            sb.AppendLine(strProperty + " = (" +
                ComplexTypeParser.ToStringArray(elementType, out strElementType) + 
                ")" + strArrTypeName + "_array;");
        }

        public static void AddDeserializeArray(
            Type reflectedType,
            Type serializerType,
            SelfDescribingClassFactory selfDescribingClassFactory)
        {
            string strArrTypeName = SerializerParserHelper.GetUniqueTypeName();
            var sb = new StringBuilder();
            AddDeserializeArrayParse(reflectedType, sb, strArrTypeName);
            sb.AppendLine("return " + strArrTypeName + "_array;");
            ReaderParser.AddReturnMethodDeserialize(sb, serializerType, selfDescribingClassFactory);
        }

        public static void AddDeserializeArrayParse(
            Type arrType,
            StringBuilder sb,
            string strArrTypeName)
        {
            ReaderParser.AddDeseserializeNullType(sb);
            var elementType = arrType.GetElementType();
            if (!elementType.IsValueType &&
                elementType != typeof(string) &&
                !elementType.IsArray)
            {
                string strArrType;
                string strArr = ComplexTypeParser.ToStringArraysType(
                    arrType, out strArrType);
                //
                // create an array with the specified type
                //
                string strIntSize = SerializerParserHelper.GetUniqueTypeName() + "_intSize";

                sb.AppendLine("int " + strIntSize + " = " + SerializerParserConstants.SERIALIZER_OBJ_NAME + ".ReadInt32();");
                sb.AppendLine("var " + strArrTypeName + "_array = (" + strIntSize + " <= 0 ? null : new " + strArr + "[" + strIntSize +  "]);");
                sb.AppendLine(typeof (ArraySerializer).Name +
                              ".DeserializeArray(" +
                              SerializerParserConstants.SERIALIZER_OBJ_NAME +
                              ", " + strArrTypeName + "_array, " + strIntSize + ", typeof(" + strArrType +
                              "));");
            }
            else
            {
                int intRank = arrType.GetArrayRank();
                var entryType = arrType.GetElementType();
                bool blnIsMultiDimensional = intRank > 1 || (
                    entryType.IsArray &&
                    intRank == 1 &&
                    entryType.GetArrayRank() == 1);

                sb.AppendLine("object " + strArrTypeName + "_array;");
                sb.AppendLine(SerializerParserConstants.SERIALIZER_OBJ_NAME + ".ReadBoolean();");
                if (blnIsMultiDimensional)
                {
                    sb.AppendLine(strArrTypeName + "_array = Serializer.DeserializeSlow<object>(" +
                                  SerializerParserConstants.SERIALIZER_OBJ_NAME + ".ReadByteArray());");
                }
                else
                {
                    string strMethodName = GetSerializeMethodName(elementType);
                    sb.AppendLine(strArrTypeName + "_array = " + SerializerParserConstants.SERIALIZER_OBJ_NAME + "." +
                                  strMethodName + "();");
                }
            }
        }

        private static string GetSerializeMethodName(
            Type type)
        {
            if(type == typeof(string))
            {
                return "ReadStringArray";
            }
            if (type == typeof(double))
            {
                return "ReadDblArray";
            }
            if (type == typeof(int))
            {
                return "ReadInt32Array";
            }
            if (type == typeof(long))
            {
                return "ReadInt64Array";
            }
            if (type == typeof(DateTime))
            {
                return "ReadDateTimeArray";
            }
            if (type == typeof(bool))
            {
                return "ReadBooleanArray";
            }
            if (type == typeof(byte))
            {
                return "ReadByteArray";
            }
            if (type == typeof(char))
            {
                return "ReadCharArray";
            }
            if (type == typeof(Single))
            {
                return "ReadSingleArray";
            }
            if (type == typeof(Type))
            {
                return "ReadTypeArray";
            }
            throw new HCException("Primitive type not found");
        }


    }
}


