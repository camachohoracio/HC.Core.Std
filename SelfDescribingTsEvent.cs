#region

using System;
using HC.Core.DynamicCompilation;
using HC.Core.Exceptions;
using HC.Core.Io.Serialization;
using HC.Core.Io.Serialization.Readers;
using HC.Core.Io.Serialization.Writers;
using HC.Core.Logging;

#endregion

namespace HC.Core
{
    [Serializable]
    public class SelfDescribingTsEvent : ASelfDescribingClass, ITsEvent
    {
        #region Properties

        public DateTime Time { get; set; }


        #endregion

        public SelfDescribingTsEvent()
            : this(string.Empty)
        {
        }

        public SelfDescribingTsEvent(Enum enumValue)
            : this(enumValue.ToString())
        {
        }

        public SelfDescribingTsEvent(string strClassName)
            : base(strClassName)
        {
        }

        #region Public

        public override void Serialize(ISerializerWriter writerBase)
        {
            try
            {
                if (writerBase == null)
                {
                    return;
                }
                writerBase.Write(m_strClassName);
                writerBase.Write(typeof (SelfDescribingTsEvent));
                ISerializerWriter serializer = SerializeProperties();
                writerBase.Write(serializer.GetBytes());

            }
            catch(Exception ex)
            {
                Logger.Log(ex);
                Logger.Log(
                    new HCException("Error when serializing selfDescribingTsEvent " + 
                    m_strClassName));
            }
        }

        public override object Deserialize(byte[] bytes)
        {
            var selfDescribingClass = new SelfDescribingTsEvent();
            var serializationReader = Serializer.GetReader(bytes);
            string strClassName = serializationReader.ReadString();
            selfDescribingClass.SetClassName(strClassName);
            // read event type
            Type type = serializationReader.ReadType();
            if(type == null)
            {
                throw new HCException("Null type on " + strClassName);
            }
            byte[] propertyBytes = serializationReader.ReadByteArray();
            DeserializeProperties(selfDescribingClass, new SerializerReader(propertyBytes));
            return selfDescribingClass;
        }

        #endregion

        public bool DeleteStrValue(string strPropertyName)
        {
            return m_strValues.Remove(strPropertyName);
        }
    }
}
