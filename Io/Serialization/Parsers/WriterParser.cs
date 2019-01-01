#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HC.Core.DynamicCompilation;
using HC.Core.Exceptions;
using HC.Core.Io.KnownObjects.KnownTypes;
using HC.Core.Io.Serialization.DataStructures;
using HC.Core.Io.Serialization.Types;
using HC.Core.Io.Serialization.Writers;
using HC.Core.Logging;

#endregion

namespace HC.Core.Io.Serialization.Parsers
{
    public static class WriterParser
    {
        #region Private & protected

        private static void GetPropertyNameTypesSerialize(
            Type reflectedType,
            string strObjName,
            out List<Type> propertyTypes,
            out List<string> propertyNames)
        {
            propertyTypes = null;
            propertyNames = null;
            try
            {
                SerializerParserHelper.GetPropertyNameTypes(
                reflectedType,
                strObjName,
                out propertyTypes,
                out propertyNames);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public static void AddWriterMethod(
            Type reflectedType,
            SelfDescribingClassFactory selfDescribingClassFactory)
        {
            try
            {
                //
                // parse serialize method
                //
                var sb = new StringBuilder();
                if (reflectedType == typeof(object))
                {
                    AddSerializeObjectType(sb);
                }
                else if (typeof(ASelfDescribingClass).IsAssignableFrom(reflectedType))
                {
                    AddSerializeSelfDescribingClass(sb);
                }
                else if (reflectedType.IsValueType ||
                    reflectedType == typeof(string))
                {
                    AddSerializeValueType(sb);
                }
                else if (reflectedType.IsArray)
                {
                    ArrayWriterParser.AddSerializeArray(sb, reflectedType);
                }
                else if (typeof(IList).IsAssignableFrom(reflectedType))
                {
                    AddSerializeList(sb);
                }
                else if (typeof(IDictionary).IsAssignableFrom(reflectedType))
                {
                    AddSerializeDictionary(sb);
                }
                else
                {
                    AddSerializeComplexType(reflectedType, sb);
                }

                //
                // add serializer method
                //
                AddReturnMethodSerialize(sb, selfDescribingClassFactory);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void AddSerializeSelfDescribingClass(StringBuilder sb)
        {
            try
            {
                AddSerializeNullType(sb);
                sb.AppendLine("((" + typeof(ASelfDescribingClass).Name + ")" +
                              SerializerParserConstants.OBJECT_NAME + ").Serialize(" +
                              SerializerParserConstants.SERIALIZER_OBJ_NAME + ");");
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void AddSerializeObjectType(StringBuilder sb)
        {
            try
            {
                sb.Append(SerializerParserConstants.SERIALIZER_OBJ_NAME + "." +
                          SerializerParserConstants.WRITE_METHOD_NAME + "(" + SerializerParserConstants.OBJECT_NAME + ");");
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void AddSerializeValueType(StringBuilder sb)
        {
            try
            {
                sb.AppendLine("serializer.Write(" + SerializerParserConstants.OBJECT_NAME + ");");
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void AddSerializeDictionary(
            StringBuilder sb)
        {
            try
            {
                AddSerializeNullType(sb);
                sb.AppendLine(typeof(DictionarySerializer).Name +
                              ".SerializeDictionary((IDictionary)" +
                              SerializerParserConstants.OBJECT_NAME + "," +
                              SerializerParserConstants.SERIALIZER_OBJ_NAME + ");");
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }


        private static void AddSerializeList(StringBuilder sb)
        {
            try
            {
                AddSerializeNullType(sb);
                sb.AppendLine(typeof(ListSerializer).Name +
                              ".SerializeList((IList)" +
                              SerializerParserConstants.OBJECT_NAME + "," +
                              SerializerParserConstants.SERIALIZER_OBJ_NAME + ");");
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void AddSerializeComplexType(
            Type reflectedType,
            StringBuilder sb)
        {
            try
            {
                List<Type> propertyTypes;
                List<string> propertyNames;
                GetPropertyNameTypesSerialize(reflectedType,
                                              SerializerParserConstants.SERIALIZE_OBJECT_NAME,
                                              out propertyTypes,
                                              out propertyNames);

                AddSerializeMethodComplexType(
                    ComplexTypeParser.ToStringType(reflectedType),
                    sb,
                    propertyNames,
                    propertyTypes);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void AddReturnMethodSerialize(
            StringBuilder sb,
            SelfDescribingClassFactory selfDescribingClassFactory)
        {
            try
            {
                var methodParams = new[] 
                                   { 
                                       new KeyValuePair<string, Type>(
                                           SerializerParserConstants.OBJECT_NAME, 
                                           typeof(object)),
                                       new KeyValuePair<string, Type>(
                                           SerializerParserConstants.SERIALIZER_OBJ_NAME, 
                                           typeof(ISerializerWriter))
                                   }.ToList();
                selfDescribingClassFactory.AddMethod(
                    true,
                    false,
                    SerializerParserConstants.SERIALIZE_METHOD_NAME,
                    null,
                    methodParams,
                    sb.ToString());
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void AddSerializeMethodComplexType(
            string strReflectedTypeName,
            StringBuilder sb,
            List<string> propertyNames,
            List<Type> propertyTypes)
        {
            try
            {
                AddSerializeNullType(sb);
                //
                // non-null type
                //
                sb.AppendLine("var " + SerializerParserConstants.SERIALIZE_OBJECT_NAME + " = (" +
                              strReflectedTypeName + ")" + SerializerParserConstants.OBJECT_NAME + ";");
                for (int i = 0; i < propertyNames.Count; i++)
                {
                    string strPropertyName = propertyNames[i];
                    Type currPropertyType = propertyTypes[i];
                    AddSerializeMethodComplexType(sb, strPropertyName, currPropertyType);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public static void AddSerializeNullType(StringBuilder sb)
        {
            try
            {
                sb.AppendLine("if(" + SerializerParserConstants.OBJECT_NAME + " == null)");
                sb.AppendLine("{");
                sb.AppendLine("serializer.Write((byte)EnumSerializedType.NullType);");
                sb.AppendLine("return;");
                sb.AppendLine("}");
                sb.AppendLine("serializer.Write((byte)EnumSerializedType.NonNullType);");
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void AddSerializeMethodComplexType(
            StringBuilder sb,
            string strProperty,
            Type currPropertyType)
        {
            try
            {
                if (currPropertyType == null)
                {
                    return;
                }
                if (currPropertyType.IsArray)
                {
                    ArrayWriterParser.AddSerializeArrayComplexType(sb, strProperty, currPropertyType);
                }
                else if (typeof(IList).IsAssignableFrom(currPropertyType))
                {
                    AddSerializeListComplexType(sb, strProperty);
                }
                else if (typeof(IDictionary).IsAssignableFrom(currPropertyType))
                {
                    AddSerializeDictionaryComplexType(sb, strProperty);
                }
                else if (typeof(Enum).IsAssignableFrom(currPropertyType))
                {
                    AddSerializeEnumComplexType(sb, strProperty, currPropertyType);
                }
                else if (!currPropertyType.IsValueType &&
                         currPropertyType != typeof(string) &&
                         currPropertyType != typeof(Type))
                {
                    AddSerializeObjectComplexType(sb, currPropertyType, strProperty);
                }
                else
                {
                    sb.AppendLine(
                        SerializerParserConstants.SERIALIZER_OBJ_NAME + "." +
                        SerializerParserConstants.WRITE_METHOD_NAME + "(" +
                        strProperty + ");");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void AddSerializeDictionaryComplexType(
            StringBuilder sb,
            string strProperty)
        {
            try
            {
                sb.AppendLine(typeof(DictionarySerializer).Name +
                              ".SerializeDictionary(" +
                              strProperty + "," +
                              SerializerParserConstants.SERIALIZER_OBJ_NAME + ");");
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void AddSerializeListComplexType(
            StringBuilder sb,
            string strProperty)
        {
            try
            {
                AddSerializeNullType(sb);
                sb.AppendLine(typeof(ListSerializer).Name +
                              ".SerializeList((IList)" +
                              strProperty + "," +
                              SerializerParserConstants.SERIALIZER_OBJ_NAME + ");");
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void AddSerializeEnumComplexType(
            StringBuilder sb,
            string strPropertyName,
            Type currPropertyType)
        {
            try
            {
                int intTypeIndex = KnownTypesCache.GetTypeId(currPropertyType);
                if (intTypeIndex == -1)
                {
                    //
                    // if type is not found
                    //
                    sb.AppendLine(
                        SerializerParserConstants.SERIALIZER_OBJ_NAME + "." +
                        SerializerParserConstants.WRITE_METHOD_NAME + "(-1);");
                    sb.AppendLine(
                        SerializerParserConstants.SERIALIZER_OBJ_NAME + "." +
                        SerializerParserConstants.WRITE_METHOD_NAME + "(" +
                        '"' + currPropertyType.FullName + '"' + ");");
                    sb.AppendLine(
                        SerializerParserConstants.SERIALIZER_OBJ_NAME + "." +
                        SerializerParserConstants.WRITE_METHOD_NAME + "(" +
                        '"' + currPropertyType.Assembly.GetName().Name + '"' + ");");
                }

                Type underlyingType = Enum.GetUnderlyingType(currPropertyType);

                string strCastCode = GetStrCastCode(underlyingType);

                sb.AppendLine(
                    SerializerParserConstants.SERIALIZER_OBJ_NAME + "." +
                    SerializerParserConstants.WRITE_METHOD_NAME + "(" +
                    intTypeIndex + ");");
                sb.AppendLine(
                    SerializerParserConstants.SERIALIZER_OBJ_NAME + "." +
                    SerializerParserConstants.WRITE_METHOD_NAME + "(" + strCastCode +
                        strPropertyName + ");");
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static string GetStrCastCode(Type underlyingType)
        {
            try
            {
                string strCastCode;
                switch (Type.GetTypeCode(underlyingType))
                {
                    case TypeCode.String:
                        strCastCode = "(string)";
                        break;
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                        strCastCode = "(int)";
                        break;
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        strCastCode = "(long)";
                        break;
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                        strCastCode = "(byte)";
                        break;
                    case TypeCode.Int16:
                        strCastCode = "(short)";
                        break;
                    default:
                        throw new HCException( "Type not found [" +
                            underlyingType.Name + "]");
                }
                return strCastCode;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        private static void AddSerializeObjectComplexType(
            StringBuilder sb,
            Type currPropertyType,
            string strProperty)
        {
            try
            {
                //
                // check for null objects
                //
                sb.AppendLine("if(" +
                              strProperty + " == null){ " +
                              SerializerParserConstants.SERIALIZER_OBJ_NAME + "." +
                              SerializerParserConstants.WRITE_METHOD_NAME + "((byte)EnumSerializedType.NullType);}");
                sb.AppendLine("else { " +
                              SerializerParserConstants.SERIALIZER_OBJ_NAME + "." +
                              SerializerParserConstants.WRITE_METHOD_NAME + "((byte)EnumSerializedType.NonNullType);");

                //
                // serialize an object
                //
                string strSerializeObj = typeof(SerializerCache).Name +
                                         ".GetSerializer(typeof(" +
                                         ComplexTypeParser.ToStringType(
                                            (currPropertyType.IsAbstract || currPropertyType.IsInterface) ? 
                                         typeof(object) : currPropertyType) +
                                         ")).Serialize(" +
                                         strProperty + "," +
                                         SerializerParserConstants.SERIALIZER_OBJ_NAME + ");}";
                sb.AppendLine(strSerializeObj);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        #endregion
    }
}



