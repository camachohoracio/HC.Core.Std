using System;
using HC.Core.Io.Serialization.Interfaces;

namespace HC.Core.DataStructures
{
    public class StringWrapper : ASerializable, IDisposable
    {
        public String Str { get; set; }
        
        public StringWrapper(){}

        public StringWrapper(string str)
        {
            Str = str;
        }

        public void Dispose()
        {
            Str = null;
        }

        public override string ToString()
        {
            return Str;
        }

    }
}