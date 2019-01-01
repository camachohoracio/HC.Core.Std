using System;
using System.Collections;
using System.Collections.Generic;

namespace HC.Core.Threading.Buffer
{
    public class BufferItem : IEquatable<BufferItem>,
        IComparable<BufferItem>,
        IComparer<BufferItem>,
        IComparer,
        IEqualityComparer<BufferItem>
    {
        public DateTime Date { get; set; }
        public long Age { get; set; }

        public bool Equals(BufferItem other)
        {
            return Age == other.Age;
        }

        public int CompareTo(BufferItem other)
        {
            return Compare(this, other);
        }

        public int Compare(BufferItem x, BufferItem y)
        {
            return x.Age.CompareTo(y.Age);
        }

        public override int GetHashCode()
        {
            return Age.GetHashCode();
        }

        public int Compare(object x, object y)
        {
            return Compare((BufferItem)x, (BufferItem)y);
        }

        public bool Equals(BufferItem x, BufferItem y)
        {
            return x.Age == y.Age;
        }

        public int GetHashCode(BufferItem obj)
        {
            return obj.Age.GetHashCode();
        }
    }
}



