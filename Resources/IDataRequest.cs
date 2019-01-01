using System;
using System.Collections;
using System.Collections.Generic;
using HC.Core.Io.Serialization.Interfaces;

namespace HC.Core.Resources
{
    public interface IDataRequest : 
        IEquatable<IDataRequest>,
        IComparable<IDataRequest>, 
        IComparer<IDataRequest>,
        IComparer,
        IEqualityComparer<IDataRequest>,
        ISerializable,
        IDisposable
    {
        string Name { get; }
    }
}



