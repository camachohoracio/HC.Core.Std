using System;

namespace HC.Core.Threading.ProducerConsumerQueues.Support
{
    public interface IThreadedQueue<T> : IDisposable where T : IDisposable
    {
        #region Events

        event WorkDelegate<T> OnWork;
        event WorkDelegate<T> OnTaskInProgress;
        event WorkDelegate<T> OnTaskCompleted;

        #endregion

        void SetAutoDisposeTasks(bool blnValue);
        int QueueSize { get; }
        int TasksInProgress { get; }
        int TasksDone { get; }
        int Threads { get; }
        string Id { get; set; }
        TaskWrapper EnqueueTask(T state);
        void LogQueuePerformance(string name);
        void Flush();
    }
}


