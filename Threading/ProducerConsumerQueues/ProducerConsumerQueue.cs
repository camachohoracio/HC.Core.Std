#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HC.Core.DynamicCompilation;
using HC.Core.Events;
using HC.Core.Exceptions;
using HC.Core.Helpers;
using HC.Core.Logging;
using HC.Core.Threading.ProducerConsumerQueues.Support;

#endregion

namespace HC.Core.Threading.ProducerConsumerQueues
{
    public class ProducerConsumerQueue<T> : IThreadedQueue<T> where T : IDisposable
    {
        #region Properties

        public int QueueSize
        {
            get { return m_taskQ.Count; }
        }

        public bool DoNotLogExceptions { get; set; }
        public int Threads { get; private set; }
        public int Capacity { get; private set; }
        public bool IsLifo { get; private set; }
        public string Id { get; set; }
        public int TasksInProgress { get { return m_intTasksInProgress; } }
        public int TasksDone { get { return m_intTasksDone; } }
        public BlockingCollection<WorkItem<T>> m_taskQ { get; private set; }

        #endregion

        #region Members

        private int m_intTasksInProgress;
        private bool m_blnDropItems;
        private readonly object m_lockObject = new object();
        private readonly List<Task> m_longRunningTasks;
        private bool m_blnIsDisposed;
        //private static int m_intLongRunningTasks;
        private static readonly object m_intLongRunningTasksLock = new object();
        //private string m_strQueueName;
        private ThreadWorker m_logWorker;
        private bool m_blnDoLogging;
        private int m_intTasksQueued;
        private readonly object m_logLock = new object();
        private static DateTime m_lastLoggedTime;
        private int m_intTasksDone;
        private readonly string m_strAssemblyName;
        private readonly string m_strClassName;
        private bool m_blnAutoDisposeTasks;

        #endregion

        #region Events & delegates

        public event WorkDelegate<T> OnWork;
        public event WorkDelegate<T> OnTaskInProgress;
        public event WorkDelegate<T> OnTaskCompleted;

        #endregion

        #region Constructors

        static ProducerConsumerQueue()
        {
            try
            {
                //
                // set number of threads
                //
                int intWorkerThreads;
                int intCompletionPortThreads;
                ThreadPool.GetMaxThreads(
                    out intWorkerThreads,
                    out intCompletionPortThreads);
                //
                // limit the number of threads
                //
                ThreadPool.SetMaxThreads(
                    Math.Min(intWorkerThreads, 2000),
                    Math.Min(intCompletionPortThreads, 1000));
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public ProducerConsumerQueue(
            int intThreads,
            string strName)
            : this(
                intThreads,
                0,
                false,
                false,
                strName)
        {
        }

        public ProducerConsumerQueue(
            int intThreads)
            : this(
                intThreads,
                0,
                false,
                false,
                string.Empty)
        {
        }

        public ProducerConsumerQueue(
            int intThreads,
            int capacity,
            bool isLifo,
            bool blnDropItems)
            : this(
                intThreads,
                capacity,
                isLifo,
                blnDropItems,
                string.Empty)
        {
        }

        public ProducerConsumerQueue(
            int intThreads,
            int capacity,
            bool isLifo,
            bool blnDropItems,
            string strName)
        {
            try
            {
                m_longRunningTasks = new List<Task>();
                Threads = intThreads;
                Capacity = capacity;
                IsLifo = isLifo;
                m_blnDropItems = blnDropItems;

                GetQueue(capacity, isLifo);
                if (!string.IsNullOrEmpty(strName))
                {
                    Id = strName;
                }
                LoadWorkers(intThreads);
                m_strAssemblyName = GetType().Assembly.GetName().Name;
                m_strClassName = typeof(T).Name;
                Id = Guid.NewGuid().ToString();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public ProducerConsumerQueue(
            int intThreads,
            int intCapacity)
            : this(
                intThreads,
                intCapacity,
                false,
                false,
                string.Empty) { }

        ~ProducerConsumerQueue()
        {
            Dispose();
        }

        #endregion

        #region Public

        public void LogQueuePerformance(string strQueueName)
        {
            try
            {
                if (m_logWorker != null)
                {
                    throw new HCException("Queue log worker already loaded!");
                }
                //
                // keep logging forever
                //
                Id = strQueueName;
                m_logWorker = new ThreadWorker();
                m_logWorker.OnExecute += OnWorkerLog;
                m_logWorker.Work();
                m_blnDoLogging = true;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public void Flush()
        {
            try
            {
                while (m_taskQ.Count > 0)
                {
                    WorkItem<T> item;
                    m_taskQ.TryTake(out item);
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private void OnWorkerLog()
        {
            while (true)
            {
                try
                {
                    DoLogging();
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
                finally
                {
                    Thread.Sleep(60000);
                }
            }
        }

        private void LoadWorkers(
            int intThreads)
        {
            var workerNames =
                new string[intThreads];

            for (int i = 0; i < intThreads; i++)
            {
                workerNames[i] = Guid.NewGuid().ToString();
            }

            //
            // Create and start a serparate task for each consumer
            //
            for (int i = 0; i < intThreads; i++)
            {
                m_longRunningTasks.Add(
                    Task.Factory.StartNew(
                        state =>
                        {
                            if (!string.IsNullOrEmpty(Id))
                            {
                                Thread.CurrentThread.Name = Id;
                            }
                            Consume(state);
                        },
                        workerNames[i],
                        TaskCreationOptions.LongRunning));
                
                //m_longRunningTasks.Add(task);
                //lock (m_intLongRunningTasksLock)
                //{
                //    m_intLongRunningTasks++;
                //}
            }
        }

        private void GetQueue(int capacity, bool isLifo)
        {
            if (capacity == 0)
            {
                //
                // unlimited capacity
                //
                m_taskQ = new BlockingCollection<WorkItem<T>>(1000000);
            }
            else
            {
                if (isLifo)
                {
                    m_taskQ = new BlockingCollection<WorkItem<T>>(
                        new ConcurrentStack<WorkItem<T>>(),
                        capacity);
                }
                else
                {
                    m_taskQ = new BlockingCollection<WorkItem<T>>(capacity);
                }
            }
        }

        public void Dispose()
        {
            try
            {
                EventHandlerHelper.RemoveAllEventHandlers(this);
                if (!m_blnIsDisposed)
                {
                    m_blnIsDisposed = true;
                    //foreach (Task longRunningTask in m_longRunningTasks)
                    //{
                    //    longRunningTask.Dispose();
                    //}
                    var t = m_taskQ;
                    if (t != null)
                    {
                        t.CompleteAdding();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public TaskWrapper EnqueueTask(T state)
        {
            TaskWrapper task = EnqueueTask(null, state);
            return task;
        }

        public TaskWrapper EnqueueTask(
            CancellationToken? cancelToken,
            T state)
        {
            try
            {
                lock (m_intLongRunningTasksLock)
                {
                    m_intTasksQueued++;
                }
                if (m_blnDoLogging)
                {
                    DoLogging();
                }

                if (!(Capacity > 0 &&
                      m_blnDropItems))
                {
                    return EnqueueTask1(cancelToken, state);
                }
                lock (m_lockObject)
                {
                    if (Capacity > 0 &&
                        QueueSize >= Capacity)
                    {
                        if (m_blnDropItems)
                        {
                            //
                            // drop one elment from the queue
                            //
                            WorkItem<T> workItem;
                            while (m_taskQ.TryTake(out workItem))
                            {
                                //string strMessage = "Dropped task = " + workItem.State;
                                //Logger.Log(strMessage);
                                //PrintToScreen.WriteLine(strMessage);
                            }
                        }
                        Console.WriteLine("Queue is full for " + typeof(T).Name);
                    }
                    return EnqueueTask1(cancelToken, state);
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public void SetDropItems(bool blnDropItems)
        {
            m_blnDropItems = blnDropItems;
        }

        public void DoLogging()
        {
            return;
            try
            {
                if ((DateTime.Now - m_lastLoggedTime).TotalMinutes > 1)
                {
                    lock (m_logLock)
                    {
                        if ((DateTime.Now - m_lastLoggedTime).TotalMinutes > 1)
                        {
                            m_lastLoggedTime = DateTime.Now;
                            string strLog = GetLog();
                            Logger.Log(strLog);

                            var logClass = new SelfDescribingClass();
                            logClass.SetClassName(m_strClassName);
                            logClass.SetStrValue("Time", m_lastLoggedTime.ToString());
                            logClass.SetStrValue("QueueName", Id);
                            logClass.SetIntValue("QueueSize", m_taskQ.Count);
                            logClass.SetIntValue("Tasksqueued", m_intTasksQueued);
                            logClass.SetIntValue("TasksInProgress", TasksInProgress);
                            logClass.SetIntValue("TasksDone", m_intTasksDone);
                            logClass.SetIntValue("Threads", Threads);

                            LiveGuiPublisherEvent.PublishGrid(
                                m_strAssemblyName,
                                ReflectionHelper.GetTypeNameRecursive(GetType()),
                                Id,
                                m_strClassName,
                                logClass,
                                2,
                                true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public string GetLog()
        {
            try
            {
                string strQueueName = string.IsNullOrEmpty(Id)
                                          ? typeof(T).Name
                                          : Id;
                int intTasksDone = Math.Min(5000, m_intTasksDone);

                int intCounter = (intTasksDone + QueueSize + TasksInProgress) + 1;

                string strLog = "Queue [" + strQueueName +
                                "][" + Id +
                                "]. Queue size [" + QueueSize +
                                "]. Tasks queued [" + m_intTasksQueued +
                                "]. Tasks in progress [" + TasksInProgress +
                                "]. Tasks done [" + m_intTasksDone +
                                "]. [" + (100 * intTasksDone / intCounter) +
                                "]%";
                return strLog;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return string.Empty;
        }

        private TaskWrapper EnqueueTask1(CancellationToken? cancelToken, T state)
        {
            try
            {
                var tcs = new TaskCompletionSource<object>();
                var taskWrapper = new TaskWrapper(tcs.Task, state);
                m_taskQ.Add(new WorkItem<T>(tcs, cancelToken, state, taskWrapper));
                return taskWrapper;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        #endregion

        #region Private

        private void Consume(object state)
        {
            try
            {
                foreach (WorkItem<T> workItem in m_taskQ.GetConsumingEnumerable())
                {
                    try
                    {
                        if (workItem.CancelToken.HasValue &&
                            workItem.CancelToken.Value.IsCancellationRequested)
                        {
                            workItem.TaskSource.SetCanceled();
                        }
                        else
                        {
                            try
                            {
                                Work(workItem.State);
                                workItem.TaskSource.SetResult(workItem.State);
                                if (m_blnAutoDisposeTasks)
                                {
                                    workItem.TaskWrapper.Dispose();
                                }
                            }
                            catch (Exception ex)
                            {
                                if (DoNotLogExceptions)
                                {
                                    Console.WriteLine(ex);
                                }
                                else
                                {
                                    Logger.Log(ex);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (DoNotLogExceptions)
                        {
                            Console.WriteLine(ex);
                        }
                        else
                        {
                            Logger.Log(ex);
                        }
                    }
                }

            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private void Work(T state)
        {
            lock (m_intLongRunningTasksLock)
            {
                m_intTasksInProgress++;
            }

            if (OnTaskInProgress != null)
            {
                OnTaskInProgress.Invoke(state);
            }
            var o = OnWork;

            if (o != null &&
                o.GetInvocationList().Length > 0)
            {
                o.Invoke(state);
            }
            if (OnTaskCompleted != null)
            {
                OnTaskCompleted.Invoke(state);
            }

            lock (m_intLongRunningTasksLock)
            {
                m_intTasksDone++;
                m_intTasksInProgress--;
            }
            if (m_blnDoLogging)
            {
                DoLogging();
            }
        }

        public void SetAutoDisposeTasks(bool blnValue)
        {
            m_blnAutoDisposeTasks = blnValue;
        }

        #endregion

        public void ResetQueueCounter()
        {
            m_intTasksDone = 0;
        }
    }
}



