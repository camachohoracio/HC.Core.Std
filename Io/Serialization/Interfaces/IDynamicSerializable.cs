#region

using System;
using HC.Core.Io.Serialization.Readers;
using HC.Core.Io.Serialization.Writers;

#endregion

namespace HC.Core.Io.Serialization.Interfaces
{
    public interface IDynamicSerializable : ICloneable
    {
        void Serialize(
            object obj,
            ISerializerWriter serializer);
        object Deserialize(ISerializerReader serializer);
    }
}


