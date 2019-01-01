#region

using System;
using System.Threading;
using System.Threading.Tasks;
using HC.Core.Logging;
using HC.Core.Threading.ProducerConsumerQueues.Support;

#endregion

namespace HC.Core.Threading.ProducerConsumerQueues
{
    public class SingleThreadedQueue<T> : IThreadedQueue<T> where T : IDisposable
    {
        #region Events & delegates

        public event WorkDelegate<T> OnWork;
        public event WorkDelegate<T> OnTaskInProgress;
        public event WorkDelegate<T> OnTaskCompleted;

        #endregion

        #region Property

        public int TasksInProgress { get { return m_intTasksInProgress; } }
        public int TasksDone { get { return m_intTasksDone; } }

        public int QueueSize
        {
            get { return m_queue.Count; }
        }

        public int Threads
        {
            get { return 1; }
        }

        public string Id { get; set; }

        #endregion

        #region Members

        private bool m_blnAutoDisposeTasks;
        private int m_intTasksInProgress;
        private int m_intTasksDone;
        private readonly SingleThreadedQueueBase<WorkItem<T>> m_queue;

        #endregion

        #region Constructors

        public SingleThreadedQueue()
        {
            m_queue = new SingleThreadedQueueBase<WorkItem<T>>();
            m_queue.OnWork += OnCallback;
        }

        #endregion

        public TaskWrapper EnqueueTask(T state)
        {
            var tcs = new TaskCompletionSource<object>();
            var taskWrapper = new TaskWrapper(tcs.Task, state);
            m_queue.EnqueueTask(
                new WorkItem<T>(tcs, null, state, taskWrapper));
            return taskWrapper;
        }

        private void DoWork(object obj)
        {
            if (OnWork != null)
            {
                Interlocked.Increment(ref m_intTasksInProgress);
                if(OnTaskInProgress != null)
                {
                    OnTaskInProgress((T)obj);
                }
                OnWork((T)obj);
                if (OnTaskCompleted != null)
                {
                    OnTaskCompleted((T)obj);
                }
                Interlocked.Decrement(ref m_intTasksInProgress);
                Interlocked.Increment(ref m_intTasksDone);
            }
        }

        private void OnCallback(WorkItem<T> workItem)
        {
            try
            {
                DoWork(workItem.State);
                workItem.TaskSource.SetResult(workItem.State);
                if(m_blnAutoDisposeTasks)
                {
                    workItem.TaskWrapper.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public void LogQueuePerformance(string guiQueue)
        {

        }

        public void Flush()
        {
            m_queue.Clear();
        }

        public void SetAutoDisposeTasks(bool blnValue)
        {
            m_blnAutoDisposeTasks = blnValue;
        }

        public void Dispose()
        {
            EventHandlerHelper.RemoveAllEventHandlers(this);
            m_queue.Dispose();
        }
    }
}



