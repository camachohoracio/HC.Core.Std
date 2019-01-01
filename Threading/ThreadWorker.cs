#region

using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
//using System.Windows.Forms;
using HC.Core.Exceptions;
using HC.Core.Threading.ProducerConsumerQueues.Support;

#endregion

namespace HC.Core.Threading
{
    public class ThreadWorker : IDisposable
    {
        public delegate void DoWorkDel<T>(T state);
        
        #region Events

        #region Delegates

        public delegate void CancelAllThreads();

        public delegate void DelegateThreadExecute();

        public delegate void OnDelegateThreadFinished();

        public delegate void OnDelegateThreadDisposed();
        

        #endregion

        // Worker thread sets this event when it is finished
        private readonly ManualResetEvent m_eventFinished;
        public static event CancelAllThreads m_onCancelAllThreads;
        public event DelegateThreadExecute OnExecute;
        public event OnDelegateThreadFinished m_delegateThreadFinished;
        public event OnDelegateThreadDisposed m_delegateThreadDisposed;

        #endregion

        #region Properties

        public Thread WorkerThread { get; private set; }

        public bool IsAlive
        {
            get { return WorkerThread.IsAlive; }
        }

        public bool WaitForExit { get; set; }

        #endregion

        #region Members

        protected static readonly object m_lockObject = new object();
        private readonly bool m_blnRunMethod;
        private MethodInfo m_methodInfo;
        private readonly object m_obj;
        private readonly object[] m_parameters;

        #endregion

        #region Constructors

        public ThreadWorker() : this(ThreadPriority.Normal)
        {
        }

        public ThreadWorker(ThreadPriority threadPriority)
        {
            //
            // ensure all threads are closed
            //
            m_onCancelAllThreads +=
                ThreadWorker_OnCancelAllThreads;
            m_eventFinished = new ManualResetEvent(false);
            BuildThread(threadPriority);
        }

        public static void StartTaskAsync(DelegateThreadExecute task)
        {
            string strMessage = "Started async task[" + task + "]";
            Console.WriteLine(strMessage);
            task.BeginInvoke(null, null);
        }

        public ThreadWorker(
            MethodInfo methodInfo,
            object obj,
            object[] parameters) : this(methodInfo,obj,parameters, ThreadPriority.Normal)
        {
        }

        public ThreadWorker(
            MethodInfo methodInfo,
            object obj,
            object[] parameters,
            ThreadPriority threadPriority)
        {
            //
            // ensure all threads are closed
            //
            m_onCancelAllThreads +=
                ThreadWorker_OnCancelAllThreads;
            WaitForExit = true;
            m_eventFinished = new ManualResetEvent(false);
            m_methodInfo = methodInfo;
            m_obj = obj;
            m_parameters = parameters;
            m_blnRunMethod = true;
            BuildThread(threadPriority);

        }

        #endregion

        private void ThreadWorker_OnCancelAllThreads()
        {
            StopThread();
        }

        public static TaskWrapper RunTask<T>(DoWorkDel<T> workDel, T item)
        {
            var task = new Task(state => workDel((T)state), item);
            task.Start();
            return new TaskWrapper(task, null);
        }

        [STAThread]
        public virtual void Work()
        {
            try
            {
                WorkerThread.Start();


                if (WaitForExit)
                {
                    //WorkerThread.Join();

                    while (WorkerThread.IsAlive)
                    {
                        // We cannot use here infinite wait because our thread
                        // makes syncronous calls to main form, this will cause
                        // deadlock.
                        // Instead of this we wait for event some appropriate time
                        // (and by the way give time to worker thread) and
                        // process events. These events may contain Invoke calls.
                        if (WaitHandle.WaitAll(
                            (new[] {m_eventFinished}),
                            100,
                            true))
                        {
                            break;
                        }

                        //Application.DoEvents();
                    }
                }
            }
            catch (HCException e)
            {
                Console.WriteLine(e + Environment.NewLine +
                    e.StackTrace);
                ////lc.Write(e);
            }
            finally
            {
                FinishThread();
            }
        }

        private void BuildThread(ThreadPriority threadPriority)
        {
            //
            // decide which type of thread to run
            //
            ThreadStart job = !m_blnRunMethod
                                  ?
                                      InvokeExecuteMethod
                                  :
                                      new ThreadStart(RunMethod);

            WorkerThread = new Thread(job);
            WorkerThread.Priority = threadPriority;
            WorkerThread.SetApartmentState(ApartmentState.STA);
        }

        private void InvokeExecuteMethod()
        {
            try
            {
                if (OnExecute != null)
                {
                    if (OnExecute.GetInvocationList().Count() > 0)
                    {
                        OnExecute.Invoke();
                    }
                }
            }
            catch (HCException e)
            {
                //Debugger.Break();
                Console.WriteLine(e + Environment.NewLine +
                    e.StackTrace);
                ////lc.Write(e);
            }
            finally
            {
                FinishThread();
            }
        }

        private void RunMethod()
        {
            RunMethod2();
        }

        private void RunMethod2()
        {
            try
            {
                m_methodInfo.Invoke(
                    m_obj,
                    m_parameters);
            }
            catch (HCException e)
            {
                Console.WriteLine(e + Environment.NewLine +
                    e.StackTrace);
                ////lc.Write(e);
            }
            finally
            {
                FinishThread();
            }
        }


        // Stop worker thread if it is running.
        // Called when user presses Stop button or form is closed.
        public void StopThread()
        {
            if(m_delegateThreadDisposed != null)
            {
                if (m_delegateThreadDisposed.GetInvocationList().Any())
                {
                    m_delegateThreadDisposed.Invoke();
                }
            }

            WorkerThread.Abort();
            FinishThread();
        }

        private void FinishThread()
        {
            if (m_delegateThreadFinished != null)
            {
                if (m_delegateThreadFinished.GetInvocationList().Count() > 0)
                {
                    m_delegateThreadFinished.Invoke();
                }
            }
            if (WaitForExit)
            {
                m_eventFinished.Set();
            }
        }

        public static void InvokeCancelAllThreads()
        {
            if (m_onCancelAllThreads != null)
            {
                if (m_onCancelAllThreads.GetInvocationList().Count() > 0)
                {
                    m_onCancelAllThreads.Invoke();
                }
            }
        }

        public void Dispose()
        {
            m_methodInfo = null;
            StopThread();
            WorkerThread = null;
            EventHandlerHelper.RemoveAllEventHandlers(this);
        }
    }
}


