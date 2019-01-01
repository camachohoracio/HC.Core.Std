#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using HC.Core.DynamicCompilation;
using HC.Core.Io.KnownObjects.KnownTypes;
using HC.Core.Io.Serialization;
using HC.Core.Io.Serialization.Interfaces;
using HC.Core.Io.Serialization.Readers;
using HC.Core.Io.Serialization.Writers;
using HC.Core.Logging;
using HC.Core.Reflection;

#endregion

namespace HC.Core.Comunication.TopicBased.Contracts
{
    [DataContract]
    [KnownType("GetKnownTypes")]
    [Serializable]
    public class TopicMessage : ASerializable, IDisposable
    {
        #region Properties

        [DataMember]
        public string TopicName { get; set; }

        [DataMember]
        public object EventData { get; set; }

        [DataMember]
        public string PublisherName { get; set; }

        #endregion

        #region Members

        private static CreateInstanceHelper<TopicMessage> m_createInstanceHelper;
        private string m_strConnectionName;

        #endregion

        #region Constructors

        static TopicMessage()
        {
            m_createInstanceHelper = new CreateInstanceHelper<TopicMessage>();
        }

        #endregion

        #region Public

        /// <summary
        /// Note: Do not delete this method, it is used by the attribute above
        /// </summary>
        /// <returns></returns>
        private static Type[] GetKnownTypes()
        {
            var knowTypes = new List<Type>(from n in KnownTypesCache.KnownTypes.Values
                select n.Type);
            //
            // remove known types not liked by wcf
            //
            knowTypes.Remove(typeof (Array));
            knowTypes.Remove(typeof(ASelfDescribingClass));
            return knowTypes.ToArray();
        }

        public void SetConnectionName(string strConnectionName)
        {
            m_strConnectionName = strConnectionName;
        }

        public string GetConnectionName()
        {
            return m_strConnectionName;
        }

        public override object Deserialize(byte[] bytes)
        {
            return DeserializeStatic(bytes);
        }

        public void Dispose()
        {
            TopicName = null;
            EventData = null;
            PublisherName = null;
            m_createInstanceHelper = null;
            m_strConnectionName = null;
        }

        public static TopicMessage DeserializeStatic(byte[] bytes)
        {
            try
            {
                ISerializerReader serializerReader = Serializer.GetReader(bytes);
                string strCurrTopic = serializerReader.ReadString();
                var topicMessage = new TopicMessage
                {
                    TopicName = strCurrTopic,
                    PublisherName = serializerReader.ReadString(),
                    EventData = serializerReader.ReadObject()
                };
                return topicMessage;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return new TopicMessage();
        }

        public override byte[] GetByteArr()
        {
            try
            {
                ISerializerWriter serializerWriter = Serializer.GetWriter();
                serializerWriter.Write(TopicName);
                serializerWriter.Write(PublisherName);
                serializerWriter.Write(EventData);
                byte[] bytes = serializerWriter.GetBytes();
                return bytes;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        #endregion
    }
}