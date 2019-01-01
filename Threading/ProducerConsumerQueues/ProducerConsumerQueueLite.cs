#region

using System;
using System.Collections;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using HC.Core.DynamicCompilation;
using HC.Core.Events;
using HC.Core.Helpers;
using HC.Core.Logging;
using HC.Core.Threading.ProducerConsumerQueues.Support;

#endregion

namespace HC.Core.Threading.ProducerConsumerQueues
{

    public class ProducerConsumerQueueLite<T> : IThreadedQueue<T>, IEnumerable where T : IDisposable
    {
        private readonly int m_intCapacity;
        public int TasksInProgress { get { return m_intTasksInProgress; } }
        public int TasksDone { get { return m_intTasksDone; } }

        #region Events & delegates

        public event WorkDelegate<T> OnWork;
        public event WorkDelegate<T> OnTaskInProgress;
        public event WorkDelegate<T> OnTaskCompleted;

        #endregion

        #region Members

        private string m_strQueueName;
        private ThreadWorker m_logWorker;
        private bool m_blnDoLogging;
        private static DateTime m_lastLoggedTime;
        private readonly object m_logLock = new object();
        private int m_intTasksInProgress;
        private int m_intTasksQueued;
        private int m_intTasksDone;
        private readonly string m_strAssemblyName;
        private readonly string m_strClassName;
        private bool m_blnAutoDisposeTasks;

        #endregion

        #region Properties

        public string Id { get; set; }

        public int QueueSize
        {
            get { return m_itemQ.Count; }
        }

        public int Threads
        {
            get { return m_workers.Length; }
        }

        #endregion

        #region Private

        readonly object m_locker = new object();
        readonly Thread[] m_workers;
        readonly Queue<WorkItem<T>> m_itemQ;
        private readonly object m_enqueueLock = new object();

        #endregion

        public ProducerConsumerQueueLite(
            int workerCount) : this(workerCount,0)
        {
        }


        public ProducerConsumerQueueLite(
            int workerCount,
            int intCapacity)
        {
            m_intCapacity = intCapacity;
            m_itemQ = new Queue<WorkItem<T>>();
            Id = Guid.NewGuid().ToString();
            m_strAssemblyName = GetType().Assembly.GetName().Name;
            m_strClassName = GetType().Name +"_" + typeof(T).Name;
            m_workers = new Thread[workerCount];

            // Create and start a separate thread for each worker
            for (int i = 0; i < workerCount; i++)
            {
                (m_workers[i] = new Thread(Consume)).Start();
            }
        }

        public void Shutdown(bool waitForWorkers)
        {
            //
            // Enqueue one null item per worker to make each exit.
            //
            for (int i = 0; i < m_workers.Length; i++)
            {
                EnqueueTask(default(T));
            }
            //
            // Wait for workers to finish
            //
            if (waitForWorkers)
            {
                foreach (Thread worker in m_workers)
                {
                    worker.Join();
                }
            }
        }

        public TaskWrapper EnqueueTask(T item)
        {
            lock (m_enqueueLock)
            {
                if (m_intCapacity > 0)
                {
                    while (m_itemQ.Count >= m_intCapacity)
                    {
                        Thread.Sleep(10);
                    }
                }
                lock (m_locker)
                {
                    Interlocked.Increment(ref m_intTasksQueued);
                    if (m_blnDoLogging)
                    {
                        DoLogging();
                    }

                    var tcs = new TaskCompletionSource<object>();
                    var taskWrapper = new TaskWrapper(tcs.Task, item);
                    
                    m_itemQ.Enqueue(new WorkItem<T>(tcs, null, item, taskWrapper));
                    Monitor.Pulse(m_locker); // changing a blocking condition.
                    return taskWrapper;
                }
            }
        }

        public void LogQueuePerformance(string strQueueName)
        {
            //
            // keep logging forever
            //
            m_strQueueName = strQueueName;
            m_logWorker = new ThreadWorker();
            m_logWorker.OnExecute += OnWorkerLog;
            m_logWorker.Work();
            m_blnDoLogging = true;
        }

        public void Flush()
        {
            m_itemQ.Clear();
        }

        private void OnWorkerLog()
        {
            while (true)
            {
                DoLogging();
                Thread.Sleep(60000);
            }
        }

        public void DoLogging()
        {
            if ((DateTime.Now - m_lastLoggedTime).TotalMinutes > 1)
            {
                lock (m_logLock)
                {
                    if ((DateTime.Now - m_lastLoggedTime).TotalMinutes > 1)
                    {
                        m_lastLoggedTime = DateTime.Now;
                        string strQueueName = string.IsNullOrEmpty(m_strQueueName)
                                                  ? typeof(T).Name
                                                  : m_strQueueName;
                        string strLog = "Queue [" + strQueueName +
                            "]. Queue size [" + QueueSize +
                            "]. Tasks queued [" + m_intTasksQueued +
                            "]. Tasks in progress [" + m_intTasksInProgress +
                            "]. Tasks done [" + m_intTasksDone +
                            "]";
                        Logger.Log(strLog);

                        var logClass = new SelfDescribingClass();
                        logClass.SetClassName(m_strClassName);
                        logClass.SetStrValue("Time", m_lastLoggedTime.ToString());
                        logClass.SetStrValue("QueueName", m_strQueueName);
                        logClass.SetIntValue("QueueSize", QueueSize);
                        logClass.SetIntValue("Tasksqueued", m_intTasksQueued);
                        logClass.SetIntValue("TasksInProgress", m_intTasksInProgress);
                        logClass.SetIntValue("TasksDone", m_intTasksDone);
                        logClass.SetIntValue("Threads", Threads);

                        LiveGuiPublisherEvent.PublishGrid(
                            m_strAssemblyName,
                            ReflectionHelper.GetTypeNameRecursive(GetType()),
                            m_strClassName + "_" + Math.Abs(Id.GetHashCode()),
                            m_strClassName,
                            logClass,
                            2,
                            true);
                    }
                }
            }
        }

        void Consume()
        {
            while (true) // Keep consuming until
            {
                // told otherwise.
                try
                {
                    WorkItem<T> workItem;
                    lock (m_locker)
                    {
                        while (m_itemQ.Count == 0) Monitor.Wait(m_locker);
                        workItem = m_itemQ.Dequeue();
                    }
                    if (workItem == null) return; // This signals our exit.

                    Interlocked.Increment(ref m_intTasksInProgress);
                    DoWork(workItem.State);
                    workItem.TaskSource.SetResult(workItem.State);

                    Interlocked.Increment(ref m_intTasksDone);
                    Interlocked.Decrement(ref m_intTasksInProgress);
                    if (m_blnDoLogging)
                    {
                        DoLogging();
                    }
                    if (m_blnAutoDisposeTasks)
                    {
                        workItem.TaskWrapper.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                    Thread.Sleep(1000);
                }
            }
        }

        private void DoWork(T obj)
        {
            if (OnWork != null &&
                OnWork.GetInvocationList().Length > 0)
            {
                if (OnTaskInProgress != null)
                {
                    OnTaskInProgress(obj);
                }
                OnWork(obj);
                if (OnTaskCompleted != null)
                {
                    OnTaskCompleted(obj);
                }
            }
        }

        public void Dispose()
        {
            EventHandlerHelper.RemoveAllEventHandlers(this);
            Shutdown(false);
        }

        public void SetAutoDisposeTasks(bool blnValue)
        {
            m_blnAutoDisposeTasks = blnValue;
        }

        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}


