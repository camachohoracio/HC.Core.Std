using System;

namespace HC.Core.DataStructures
{
    public class IntWrapper : IDisposable
    {
        public IntWrapper(){}

        public IntWrapper(int i)
        {
            Int = i;
        }

        public int Int { get; set; }

        public void Dispose()
        {
            
        }
    }
}