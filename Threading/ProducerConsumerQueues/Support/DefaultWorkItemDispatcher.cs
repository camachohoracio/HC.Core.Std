using System.Threading;

namespace HC.Core.Threading.ProducerConsumerQueues.Support
{
    public class DefaultWorkItemDispatcher : IWorkItemDispatcher
    {
        public bool QueueUserWorkItem(WaitCallback waitCallback)
        {
            return ThreadPool.QueueUserWorkItem(waitCallback);
        }
    }
}


