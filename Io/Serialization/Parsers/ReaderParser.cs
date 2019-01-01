#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HC.Core.DynamicCompilation;
using HC.Core.Exceptions;
using HC.Core.Io.Serialization.DataStructures;
using HC.Core.Io.Serialization.Interfaces;
using HC.Core.Io.Serialization.Types;
using HC.Core.Logging;

#endregion

namespace HC.Core.Io.Serialization.Parsers
{
    public static class ReaderParser
    {
        public static void AddReaderMethod(
            Type serializerType,
            Type reflectedType,
            SelfDescribingClassFactory selfDescribingClassFactory)
        {
            try
            {
                if (reflectedType == typeof(object))
                {
                    AddDeserializeObjectType(
                        serializerType,
                        selfDescribingClassFactory);
                }
                else if (typeof(ASelfDescribingClass).IsAssignableFrom(reflectedType))
                {
                    AddDeserializeSelfDescribingClass(
                        serializerType,
                        selfDescribingClassFactory);
                }
                else if (reflectedType.IsValueType ||
                         reflectedType == typeof(string))
                {
                    AddDeserializeValueType(serializerType, selfDescribingClassFactory);
                }
                else if (reflectedType.IsArray)
                {
                    ArrayReaderParser.AddDeserializeArray(reflectedType, serializerType, selfDescribingClassFactory);
                }
                else if (typeof(IList).IsAssignableFrom(reflectedType))
                {
                    AddDeserializeList(reflectedType, serializerType, selfDescribingClassFactory);
                }
                else if (typeof(IDictionary).IsAssignableFrom(reflectedType))
                {
                    AddDeserializeDictionary(reflectedType, serializerType, selfDescribingClassFactory);
                }
                else
                {
                    AddDeserializeComplexType(reflectedType, serializerType, selfDescribingClassFactory);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public static void AddReturnMethodDeserialize(
            StringBuilder sb,
            Type serializerType,
            SelfDescribingClassFactory selfDescribingClassFactory)
        {
            try
            {
                var methodParams = new[]
                                   {
                                       new KeyValuePair<string, Type>(
                                           SerializerParserConstants.SERIALIZER_OBJ_NAME,
                                           serializerType)
                                   }.ToList();
                selfDescribingClassFactory.AddMethod(
                    true,
                    false,
                    SerializerParserConstants.DESERIALIZE_METHOD_NAME,
                    typeof(object),
                    methodParams,
                    sb.ToString());
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void AddDeserializeDictionaryComplexObj(
            StringBuilder sb,
            string strProperty,
            Type currPropertyType)
        {
            try
            {
                string strKeyType;
                string strValueType;
                string strType = ComplexTypeParser.ToStringMap(
                    currPropertyType,
                    out strKeyType,
                    out strValueType);

                string strUniqueEnumTypeName = "map_" + SerializerParserHelper.GetUniqueTypeName();

                sb.AppendLine("var " + strUniqueEnumTypeName + " = new " + strType + "();");

                sb.AppendLine(
                    typeof(DictionarySerializer).Name +
                    ".DeserializeDictionary(" + strUniqueEnumTypeName + ", typeof(" +
                    strKeyType + "), typeof(" +
                    strValueType + ")," +
                    SerializerParserConstants.SERIALIZER_OBJ_NAME + ");");
                sb.AppendLine(strProperty + " = " + strUniqueEnumTypeName + ";");
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void AddDeserializeListComplexObj(
            StringBuilder sb,
            string strProperty,
            Type currPropertyType)
        {
            try
            {
                string strListName;
                AddDeserializeListParse(
                    currPropertyType,
                    sb,
                    out strListName);
                sb.AppendLine(strProperty + " = " + strListName + ";");
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }

        }

        private static void AddDeserializeObjectComplexObj(
            StringBuilder sb,
            string strProperty,
            Type currPropertyType)
        {
            try
            {
                string strReadIsNull = "(" +
                    typeof(EnumSerializedType).Name + ")" + SerializerParserConstants.SERIALIZER_OBJ_NAME + ".ReadByte()";
                sb.AppendLine(strProperty + " = (" +
                              strReadIsNull + " != " + typeof(EnumSerializedType).Name + ".NullType ? ");

                string strSerializeObj =
                    "(" + ComplexTypeParser.ToStringType(currPropertyType) + ")" +
                    typeof(SerializerCache).Name +
                    ".GetSerializer(typeof(" +
                    ComplexTypeParser.ToStringType(
                        (currPropertyType.IsAbstract || currPropertyType.IsInterface) ? 
                        typeof(object) : currPropertyType) +
                    ")).Deserialize(" +
                    SerializerParserConstants.SERIALIZER_OBJ_NAME + ")";
                sb.AppendLine(strSerializeObj);
                //
                // else set current property value
                //
                sb.AppendLine(" : null);");
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void AddDeserializeEnumComplexObj(
            StringBuilder sb,
            string strProperty,
            Type currPropertyType)
        {
            try
            {
                string strReadMethod;
                Type underlyingType = Enum.GetUnderlyingType(currPropertyType);
                switch (Type.GetTypeCode(underlyingType))
                {
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                        strReadMethod = ".ReadInt32()";
                        break;
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        strReadMethod = ".ReadInt64()";
                        break;
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                        strReadMethod = ".ReadIntByte()";
                        break;
                    case TypeCode.Int16:
                        strReadMethod = ".ReadInt16()";
                        break;
                    default:
                        throw new HCException("Undelying type not found ["  + 
                                              underlyingType.Name  + "]");
                }

                string strReadParsed = SerializerParserConstants.SERIALIZER_OBJ_NAME + strReadMethod;
                string strUniqueEnumTypeName = "enumType_" + 
                                               SerializerParserHelper.GetUniqueTypeName();
                sb.AppendLine("var " + strUniqueEnumTypeName + " = " +
                              typeof(ComplexTypeSerializer).Name + 
                              ".ReadEnumType(" + SerializerParserConstants.SERIALIZER_OBJ_NAME +
                              ");");

                string strUniqueInnerTypeName = "intInnerTypeIndex_" + 
                                                SerializerParserHelper.GetUniqueTypeName();
                sb.AppendLine("var " + strUniqueInnerTypeName + " = " + strReadParsed + ";");
                string strUniqueTypeNameEnumValue = "enumValue_" + 
                                                    SerializerParserHelper.GetUniqueTypeName();
                sb.AppendLine("var " + strUniqueTypeNameEnumValue + " = " + "(" +
                              currPropertyType.Name + ")Enum.ToObject(" + strUniqueEnumTypeName +
                              "," + strUniqueInnerTypeName + ");");
                sb.AppendLine(strProperty + " = " + strUniqueTypeNameEnumValue + ";");
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }


        private static void SetPropertyValueRefType(
            Type currPropertyType,
            StringBuilder sb,
            string strProperty)
        {
            try
            {
                sb.AppendLine(strProperty + "=" +
                              SerializerParserConstants.SERIALIZER_OBJ_NAME + "." +
                              SerializerParserHelper.ReadMethods[currPropertyType] + "();");
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public static void GetPropertyNameTypesDeserialize(
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

        private static void AddDeserializeComplexType(
            Type reflectedType,
            Type serializerType,
            SelfDescribingClassFactory selfDescribingClassFactory)
        {
            try
            {
                List<Type> propertyTypes;
                List<string> propertyNames;
                GetPropertyNameTypesDeserialize(
                    reflectedType,
                    SerializerParserConstants.DESERIALIZE_OBJECT_NAME,
                    out propertyTypes,
                    out propertyNames);
                AddDeserializeMethodComplexType(ComplexTypeParser.ToStringType(reflectedType),
                                                propertyTypes,
                                                propertyNames,
                                                serializerType,
                                                selfDescribingClassFactory);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void AddDeserializeValueType(
            Type serializerType,
            SelfDescribingClassFactory selfDescribingClassFactory)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("var " + SerializerParserConstants.DESERIALIZE_OBJECT_NAME + "=" + SerializerParserConstants.SERIALIZER_OBJ_NAME + ".ReadObject();");
                sb.AppendLine("return " + SerializerParserConstants.DESERIALIZE_OBJECT_NAME + ";");
                AddReturnMethodDeserialize(sb, serializerType, selfDescribingClassFactory);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void AddDeserializeObjectType(Type serializerType, SelfDescribingClassFactory selfDescribingClassFactory)
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append("return " + SerializerParserConstants.SERIALIZER_OBJ_NAME + ".ReadObject();");

                //
                // add serializer method. By default, return an object
                //
                AddReturnMethodDeserialize(sb, serializerType, selfDescribingClassFactory);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void AddDeserializeDictionary(
            Type reflectedType,
            Type serializerType,
            SelfDescribingClassFactory selfDescribingClassFactory)
        {
            try
            {
                var sb = new StringBuilder();
                AddDeseserializeNullType(sb);
                string strKeyType;
                string strValueType;
                string strType = ComplexTypeParser.ToStringMap(
                    reflectedType,
                    out strKeyType,
                    out strValueType);
                string strUniqueEnumTypeName = "map_" + SerializerParserHelper.GetUniqueTypeName();
                sb.AppendLine("var " + strUniqueEnumTypeName + " = new " + strType + "();");

                sb.AppendLine(
                    typeof(DictionarySerializer).Name +
                    ".DeserializeDictionary(" + strUniqueEnumTypeName + ", typeof(" +
                    strKeyType + "), typeof(" +
                    strValueType + ")," +
                    SerializerParserConstants.SERIALIZER_OBJ_NAME + ");");
                sb.AppendLine("return " + "(" + strType + ")" + strUniqueEnumTypeName + ";");

                //
                // add serializer method. By default, return an object
                //
                AddReturnMethodDeserialize(sb, serializerType, selfDescribingClassFactory);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void AddDeserializeList(
            Type reflectedType,
            Type serializerType,
            SelfDescribingClassFactory selfDescribingClassFactory)
        {
            try
            {
                var sb = new StringBuilder();
                string strListName;
                AddDeserializeListParse(reflectedType, sb, out strListName);
                sb.AppendLine("return " + strListName + ";");

                //
                // add serializer method. By default, return an object
                //
                AddReturnMethodDeserialize(sb, serializerType, selfDescribingClassFactory);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void AddDeserializeSelfDescribingClass(
            Type serializerType,
            SelfDescribingClassFactory selfDescribingClassFactory)
        {
            try
            {
                var sb = new StringBuilder();
                AddDeseserializeNullType(sb);
                sb.AppendLine(
                    "var " + SerializerParserConstants.DESERIALIZE_OBJECT_NAME + " = (" +
                    typeof(ASelfDescribingClass).Name + ")" +
                    "ASelfDescribingClass.DeserializeStatic(" +
                    SerializerParserConstants.SERIALIZER_OBJ_NAME + ");");
                sb.AppendLine("return " + SerializerParserConstants.DESERIALIZE_OBJECT_NAME + ";");

                //
                // add serializer method. By default, return an object
                //
                AddReturnMethodDeserialize(sb, serializerType, selfDescribingClassFactory);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void AddDeserializeListParse(
            Type reflectedType,
            StringBuilder sb,
            out string strListName)
        {
            strListName = null;
            try
            {
                AddDeseserializeNullType(sb);
                string strListType;
                string strList = ComplexTypeParser.ToStringList(reflectedType, out strListType);
                //
                // create a list with the specified type
                //
                string strUniqueTypeName = "intListSize_" + SerializerParserHelper.GetUniqueTypeName();
                sb.AppendLine("int " + strUniqueTypeName + " = " + SerializerParserConstants.SERIALIZER_OBJ_NAME + ".ReadInt32();");

                strListName = "list_" + SerializerParserHelper.GetUniqueTypeName();
                sb.AppendLine("var " + strListName + " = new " + strList + "(" + strUniqueTypeName + " + 1);");
                sb.AppendLine(typeof(ListSerializer).Name +
                              ".DeserializeList(" +
                              SerializerParserConstants.SERIALIZER_OBJ_NAME + ", " + strListName + ", "
                              + strUniqueTypeName + ", typeof(" + strListType + "));");
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public static void AddDeseserializeNullType(StringBuilder sb)
        {
            try
            {
                sb.AppendLine("if((EnumSerializedType)serializer.ReadByte() == EnumSerializedType.NullType) return null;");
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void AddDeserializeMethodComplexType(
            string strReflectedType,
            List<Type> propertyTypes,
            List<string> propertyNames,
            Type serializerType,
            SelfDescribingClassFactory selfDescribingClassFactory)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("try{");

                //
                // create casted instance
                //
                AddDeseserializeNullType(sb);
                sb.AppendLine("var " + SerializerParserConstants.DESERIALIZE_OBJECT_NAME + " = new " +
                              strReflectedType + "();");
                for (int i = 0; i < propertyNames.Count; i++)
                {
                    string strProperty = propertyNames[i];
                    AddDeserializeMethodComplexType(
                        sb, 
                        strProperty,
                        propertyTypes[i],
                        serializerType,
                        selfDescribingClassFactory);
                }
                sb.AppendLine("return " + SerializerParserConstants.DESERIALIZE_OBJECT_NAME + ";");
                sb.AppendLine("}catch(Exception ex){Logger.Log(ex);} return new " +
                              strReflectedType + "();");

                //
                // add serializer method. By default, return an object
                //
                AddReturnMethodDeserialize(sb, serializerType, selfDescribingClassFactory);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public static void AddDeserializeMethodComplexType(
            StringBuilder sb,
            string strProperty,
            Type currPropertyType,
            Type serializerType,
            SelfDescribingClassFactory selfDescribingClassFactory)
        {
            try
            {
                if (currPropertyType == null)
                {
                    return;
                }
                if (typeof(Array).IsAssignableFrom(currPropertyType))
                {
                    ArrayReaderParser.AddDeserializeArrayComplexObj(sb, strProperty, currPropertyType);
                }
                else if (typeof(IList).IsAssignableFrom(currPropertyType))
                {
                    AddDeserializeListComplexObj(sb, strProperty, currPropertyType);
                }
                else if (typeof(IDictionary).IsAssignableFrom(currPropertyType))
                {
                    AddDeserializeDictionaryComplexObj(sb, strProperty, currPropertyType);
                }
                else if (typeof(Enum).IsAssignableFrom(currPropertyType))
                {
                    AddDeserializeEnumComplexObj(sb, strProperty, currPropertyType);
                }
                else if (!currPropertyType.IsValueType &&
                         currPropertyType != typeof(string) &&
                         currPropertyType != typeof(Type))
                {
                    AddDeserializeObjectComplexObj(sb, strProperty, currPropertyType);
                }
                else if (currPropertyType == typeof(Type))
                {
                    sb.AppendLine(strProperty + "=" +
                                  SerializerParserConstants.SERIALIZER_OBJ_NAME + ".ReadType();");
                }
                else if(SerializerParserHelper.ReadMethods.ContainsKey(currPropertyType))
                {
                    SetPropertyValueRefType(currPropertyType, sb, strProperty);
                }
                else
                {
                    //
                    // do all over again!
                    //
                    sb.AppendLine(strProperty + "=" +
                        "(" + currPropertyType.Name + ")SerializerCache.GetSerializer(typeof(" +
                        currPropertyType.Name + ")).Deserialize(" +
                        SerializerParserConstants.SERIALIZER_OBJ_NAME + ");");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }
    }
}



