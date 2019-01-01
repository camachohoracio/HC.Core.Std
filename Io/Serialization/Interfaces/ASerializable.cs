#region

using System;
using HC.Core.Io.Serialization.Writers;

#endregion

namespace HC.Core.Io.Serialization.Interfaces
{
    [Serializable]
    public abstract class ASerializable : ISerializable
    {
        //
        // used for serializaiton
        //
        public ASerializable()
        {}
        #region ISerializable Members

        public virtual byte[] GetByteArr()
        {
            ISerializerWriter serializer = Serializer.GetWriter();
            SerializerCache.GetSerializer(GetType()).Serialize(
                this, serializer);
            return serializer.GetBytes();
        }

        public virtual object Deserialize(byte[] bytes)
        {
            var serializer = Serializer.GetReader(bytes);
            var obj = SerializerCache.GetSerializer(GetType()).Deserialize(
                serializer);
            return obj;
        }

        public virtual object Clone()
        {
            return MemberwiseClone();
        }

        #endregion
    }
}


