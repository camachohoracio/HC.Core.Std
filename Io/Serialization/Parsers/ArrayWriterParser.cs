#region

using System;
using System.Text;
using HC.Core.Io.Serialization.DataStructures;
using HC.Core.Io.Serialization.Types;

#endregion

namespace HC.Core.Io.Serialization.Parsers
{
    public static class ArrayWriterParser
    {
        public static void AddSerializeArray(
            StringBuilder sb,
            Type type)
        {
            AddSerializeArray0(sb, type, SerializerParserConstants.OBJECT_NAME);
        }

        public static void AddSerializeArrayComplexType(
            StringBuilder sb,
            string strProperty,
            Type propertyType)
        {
            AddSerializeArray0(sb, propertyType, strProperty);
        }

        private static void AddSerializeArray0(
            StringBuilder sb,
            Type type,
            string strObjectName)
        {
            WriterParser.AddSerializeNullType(sb);
            
            Type entryType = type.GetElementType();
            if (!entryType.IsValueType &&
                entryType != typeof(string) &&
                !entryType.IsArray)
            {
                //
                // write object type
                //
                sb.AppendLine(typeof(ArraySerializer).Name +
                          ".SerializeArray((Array)" +
                          strObjectName + "," +
                          SerializerParserConstants.SERIALIZER_OBJ_NAME + ");");
            }
            else
            {
                int intRank = type.GetArrayRank();
                bool blnIsMultiDimensional = intRank > 1 || (
                    entryType.IsArray &&
                    intRank == 1 &&
                    entryType.GetArrayRank() == 1);
                string strLine = SerializerParserConstants.SERIALIZER_OBJ_NAME + "." +
                SerializerParserConstants.WRITE_METHOD_NAME + "(" +
                blnIsMultiDimensional.ToString().ToLower() + ");";
                sb.AppendLine(strLine);
                
                if (blnIsMultiDimensional)
                {
                    strLine = SerializerParserConstants.SERIALIZER_OBJ_NAME + "." +
                        SerializerParserConstants.WRITE_METHOD_NAME + 
                            "(Serializer.SerializeSlow(" +
                            strObjectName + "));";
                    sb.AppendLine(strLine);
                }
                else
                {
                    string strType = ComplexTypeParser.ToStringType(entryType);
                    sb.AppendLine(
                        SerializerParserConstants.SERIALIZER_OBJ_NAME + "." +
                        SerializerParserConstants.WRITE_METHOD_NAME + "(" +
                        "(" + strType + "[])" + strObjectName + ");");
                }
            }
        }
    }
}



