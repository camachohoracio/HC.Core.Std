#region

using System;
using System.Collections.Generic;
using System.Threading;
using HC.Core.Exceptions;

#endregion

namespace HC.Core.Threading.ProducerConsumerQueues.Support
{
    public class SingleThreadedQueueBase<T> : IDisposable
    {
        #region Events & delegates

        public event WorkDelegate<T> OnWork;

        #endregion

        #region Members

        private readonly object m_syncRoot = new object();
        private readonly EventWaitHandle m_stopEvent = new AutoResetEvent(false);
        private List<T> m_data;
        private List<T> m_swapData;
        private readonly IWorkItemDispatcher m_dispatcher;
        private bool m_blnThreadActive;
        private bool m_blnStopProcessing;
        private bool m_blnDisposed;

        #endregion

        #region Properties

        public int Count
        {
            get
            {
                lock (m_syncRoot)
                {
                    return m_data.Count;
                }
            }
        }

        #endregion

        #region Constructors

        public SingleThreadedQueueBase() : this(null){}

        public SingleThreadedQueueBase(IWorkItemDispatcher dispatcher) : this(dispatcher,8){}

        public SingleThreadedQueueBase(IWorkItemDispatcher dispatcher, int initialCapacity)
        {
            if(initialCapacity <0)
            {
                throw new HCException("invalid capacity");
            }
            if(dispatcher == null)
            {
                dispatcher = new DefaultWorkItemDispatcher();
            }
            m_dispatcher = dispatcher;
            m_data =new List<T>();
            m_swapData = new List<T>(initialCapacity);
        }

        #endregion

        #region Public

        public void EnqueueTask(T item)
        {
            lock (m_syncRoot)
            {
                DoEnqueue(item);
            }
        }
        
        public void Dispose()
        {
            EventHandlerHelper.RemoveAllEventHandlers(this);
            bool shouldWait = false;
            lock (m_syncRoot)
            {
                if (m_blnDisposed)
                {
                    return;
                }

                m_blnStopProcessing = true;
                m_blnDisposed = true;

                if (m_blnThreadActive)
                {
                    shouldWait = true;
                }

                if (shouldWait)
                {
                    int intCounter = 0;
                    while (m_blnThreadActive &&
                        intCounter >= 20)
                    {
                        Thread.Sleep(100);
                        intCounter++;
                    }
                    //m_stopEvent.WaitOne();
                }
                m_stopEvent.Close();
            }
        }

        public void Clear()
        {
            lock (m_syncRoot)
            {
                m_data.Clear();
            }
        }

        public void Close()
        {
            Dispose();
        }

        #endregion

        #region Private

        private void DoEnqueue(T item)
        {
            if(m_blnDisposed)
            {
                throw new HCException("Queue already disposed");
            }
            m_data.Add(item);

            if(m_blnThreadActive == false)
            {
                m_blnThreadActive = true;
                m_dispatcher.QueueUserWorkItem(ProcessQueue);
            }
        }

        private void ProcessQueue(object state)
        {
            lock (m_syncRoot)
            {
                try
                {
                    while (m_data.Count != 0 && m_blnStopProcessing == false)
                    {
                        var temp = m_data;
                        m_data = m_swapData;
                        m_swapData = temp;

                        Monitor.Exit(m_syncRoot);

                        try
                        {
                            ProcessItems(m_swapData);
                        }
                        finally 
                        {
                            m_swapData.Clear();
                            Monitor.Enter(m_syncRoot);
                        }
                    }
                }
                finally
                {
                    m_blnThreadActive = false;
                    if (m_blnStopProcessing && !m_blnDisposed)
                    {
                        ProcessItems(m_data);
                        m_stopEvent.Set();
                    }
                }
            }
        }
        
        private void ProcessItems(List<T> items)
        {
            if (OnWork == null)
            {
                return;
            }
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                OnWork(item);
            }
        }

        //protected abstract void Process(T data);

        #endregion

        public void LogQueuePerformance(string name)
        {
            
        }
    }
}



