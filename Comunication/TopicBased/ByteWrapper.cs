using System;

namespace HC.Core.Comunication.TopicBased
{
    public class ByteWrapper : IDisposable
    {
        public byte[] Bytes { get; set; }

        public void Dispose()
        {
            Bytes = null;
        }
    }
}
