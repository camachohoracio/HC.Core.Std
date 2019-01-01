#region

using System;
using HC.Core.Io.KnownObjects.KnownTypes;

#endregion

namespace HC.Core.Io.Serialization.Interfaces
{
    [IsAKnownTypeAttr]
    public interface ISerializable : ICloneable
    {
        byte[] GetByteArr();
        object Deserialize(byte[] bytes);
    }
}


