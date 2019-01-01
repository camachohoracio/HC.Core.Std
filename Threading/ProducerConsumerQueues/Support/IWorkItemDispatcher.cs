using System.Threading;

namespace HC.Core.Threading.ProducerConsumerQueues.Support
{
    public interface IWorkItemDispatcher
    {
        bool QueueUserWorkItem(WaitCallback processQueue);
    }
}


