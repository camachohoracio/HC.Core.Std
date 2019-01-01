#region

using System;
using System.Runtime.Serialization;
using System.Xml;
using HC.Core.Logging;
using HC.Core.Reflection;
using ISerializable = HC.Core.Io.Serialization.Interfaces.ISerializable;

#endregion

namespace HC.Core.Io.Serialization
{
    public class HCSerializer : XmlObjectSerializer
    {
        #region Constants

        private const string MY_PREFIX = "#";

        #endregion

        #region Members

        private readonly ISerializable m_objectFactory;
        private readonly Type m_type;

        #endregion

        #region Constructors

        public HCSerializer(Type type)
        {
            Logger.Log("Created [" + GetType().Name + "] instance for type: " + type.Name);
            m_type = type;
            m_objectFactory = (ISerializable)ReflectorCache.GetReflector(m_type).CreateInstance();
        }

        #endregion

        #region Public

        public override void WriteStartObject(XmlDictionaryWriter writer, object graph)
        {
            try
            {
                writer.WriteStartElement(MY_PREFIX);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public override void WriteObjectContent(XmlDictionaryWriter writer, object graph)
        {
            try
            {
                var bytes = ((ISerializable) graph).GetByteArr();
                writer.WriteBase64(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public override void WriteEndObject(XmlDictionaryWriter writer)
        {
            try
            {
                writer.WriteEndElement();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public override object ReadObject(XmlDictionaryReader reader, bool verifyObjectName)
        {
            try
            {
                return m_objectFactory.Deserialize(reader.ReadElementContentAsBase64());
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public override bool IsStartObject(XmlDictionaryReader reader)
        {
            return true;
        }

        #endregion
    }
}


