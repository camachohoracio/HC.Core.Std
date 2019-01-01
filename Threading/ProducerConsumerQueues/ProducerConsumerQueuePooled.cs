//#region

//using System;
//using System.Threading;
//using System.Threading.Tasks;
//using HC.Core.Logging;
//using HC.Core.Threading.ProducerConsumerQueues.Support;

//#endregion

//namespace HC.Core.Threading.ProducerConsumerQueues
//{
//    public class ProducerConsumerQueuePooled<T> : IThreadedQueue<T> where T : IDisposable
//    {
//        public int TasksInProgress { get { return m_intTasksInProgress; } }
//        public int TasksDone { get { return m_intTasksDone; } }

//        #region Events & delegates

//        public event WorkDelegate<T> OnWork;
//        public event WorkDelegate<T> OnTaskInProgress;
//        public event WorkDelegate<T> OnTaskCompleted;

//        #endregion

//        #region Properties

//        public int QueueSize
//        {
//            get { return m_customThreadPool.requestQueue.Count; }
//        }

//        public int Threads { get; private set; }

//        public string Id { get; set; }

//        #endregion

//        #region Members

//        private int m_intTasksDone;
//        //private readonly string m_strAssemblyName;
//        //private readonly string m_strClassName;
//        private readonly CustomThreadPool m_customThreadPool;
//        private DateTime m_lastLoggedTime;
//        private readonly object m_logLock = new object();
//        private string m_strQueueName;
//        //private ThreadWorker m_logWorker;
//        private bool m_blnDoLogging;
//        private int m_intTasksQueued;
//        private int m_intTasksInProgress;
//        private bool m_blnAutoDisposeTasks;

//        #endregion

//        #region Constructors

//        public ProducerConsumerQueuePooled(int intThreads)
//        {
//            Threads = intThreads;
//            m_customThreadPool = new CustomThreadPool(
//                1, 
//                intThreads,
//                "test");
//            m_customThreadPool.Start();
//            //m_strAssemblyName = GetType().Assembly.GetName().Name;
//            //m_strClassName = typeof(T).Name;
//            Id = Guid.NewGuid().ToString();
//        }

//        #endregion

//        public TaskWrapper EnqueueTask(T state)
//        {
//            Interlocked.Increment(ref m_intTasksQueued);
//            if (m_blnDoLogging)
//            {
//                DoLogging();
//            }

//            var tcs = new TaskCompletionSource<object>();
//            var taskWraper = new TaskWrapper(tcs.Task, state);
//            m_customThreadPool.QueueUserWorkItem(
//                currState => OnCallback(
//                    new WorkItem<T>(tcs, null, state, taskWraper)), state);
//            DoLogging();
//            return taskWraper;
//        }

//        private void OnCallback(WorkItem<T> task)
//        {
//            try
//            {
//                RunTask(task.State);
//                task.TaskSource.SetResult(task.State);
                
//                if(m_blnAutoDisposeTasks)
//                {
//                    task.TaskWrapper.Dispose();
//                }
//            }
//            catch (Exception ex)
//            {
//                Logger.Log(ex);
//            }
//        }

//        private void RunTask(object obj)
//        {
//            if (OnWork != null)
//            {
//                try
//                {
//                    Interlocked.Increment(ref m_intTasksInProgress);
//                    if (OnTaskInProgress != null)
//                    {
//                        OnTaskInProgress((T)obj);
//                    }
//                    OnWork((T)obj);
//                    if (OnTaskCompleted != null)
//                    {
//                        OnTaskCompleted((T)obj);
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Logger.Log(ex);
//                }
//                finally
//                {
//                    Interlocked.Increment(ref m_intTasksDone);
//                    Interlocked.Decrement(ref m_intTasksInProgress);
//                }
//            }
//        }

//        public void Dispose()
//        {
//            EventHandlerHelper.RemoveAllEventHandlers(this);
//            m_customThreadPool.Dispose();
//        }

//        public void LogQueuePerformance(string strQueueName)
//        {
//            m_strQueueName = strQueueName;
//            //m_logWorker = new ThreadWorker();
//            //m_logWorker.OnExecute += OnWorkerLog;
//            //m_logWorker.Work();
//            m_blnDoLogging = true;
//        }

//        public void Flush()
//        {
//            m_customThreadPool.Flush();
//        }

//        //private void OnWorkerLog()
//        //{
//        //    while (true)
//        //    {
//        //        DoLogging();
//        //        Thread.Sleep(60000);
//        //    }
//        //}

//        public void DoLogging()
//        {
//            if ((DateTime.Now - m_lastLoggedTime).TotalMinutes > 1)
//            {
//                lock (m_logLock)
//                {
//                    if ((DateTime.Now - m_lastLoggedTime).TotalMinutes > 1)
//                    {
//                        string strQueueName = string.IsNullOrEmpty(m_strQueueName)
//                                                  ? typeof(T).Name
//                                                  : m_strQueueName;
//                        m_lastLoggedTime = DateTime.Now;
//                        string strLog = "Queue [" + strQueueName +
//                            "]. Queue size [" + QueueSize +
//                            "]. Tasks queued [" + m_intTasksQueued +
//                            "]. Tasks in progress [" + m_intTasksInProgress +
//                            "]. Tasks done [" + m_intTasksDone +
//                            "]";
//                        Logger.Log(strLog);

//                        //var logClass = new SelfDescribingClass();
//                        //logClass.SetClassName(Id);
//                        //logClass.SetStrValue("Time", m_lastLoggedTime.ToString());
//                        //logClass.SetStrValue("QueueName", m_strQueueName);
//                        //logClass.SetIntValue("QueueSize", QueueSize);
//                        //logClass.SetIntValue("Tasksqueued", m_intTasksQueued);
//                        //logClass.SetIntValue("TasksInProgress", m_intTasksInProgress);
//                        //logClass.SetIntValue("TasksDone", m_intTasksDone);
//                        //logClass.SetIntValue("Threads", Threads);

//                        //LiveGuiPublisherEvent.PublishGrid(
//                        //    m_strAssemblyName,
//                        //    ReflectionHelper.GetTypeNameRecursive(GetType()),
//                        //    m_strClassName + "_" + Math.Abs(Id.GetHashCode()),
//                        //    m_strClassName,
//                        //    logClass,
//                        //    2,
//                        //    true);
//                    }
//                }
//            }
//        }

//        public void SetAutoDisposeTasks(bool blnValue)
//        {
//            m_blnAutoDisposeTasks = blnValue;
//        }

//    }
//}



