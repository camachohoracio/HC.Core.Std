using System.Collections;
using System.Collections.Generic;

namespace HC.Core.Resources
{
    public class PoolDataComparer :
        IEqualityComparer<IDataRequest>,
        IEqualityComparer
    {
        public bool Equals(IDataRequest x, IDataRequest y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(IDataRequest obj)
        {
            return obj.Name.GetHashCode();
        }

        public bool Equals(object x, object y)
        {
            return Equals((IDataRequest) x,
                          (IDataRequest) y);
        }

        public int GetHashCode(object obj)
        {
            return ((IDataRequest) obj).Name.GetHashCode();
        }
    }
}



