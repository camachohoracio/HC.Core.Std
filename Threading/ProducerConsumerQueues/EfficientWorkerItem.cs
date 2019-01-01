using System;

namespace HC.Core.Threading.ProducerConsumerQueues
{
    public class EfficientWorkerItem : IDisposable
    {
        public void Dispose()
        {
            Str = null;
            Item = null;
        }

        public string Str { get; set; }

        public object Item { get; set; }
    }
}