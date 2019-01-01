#region

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using HC.Core.Exceptions;
using HC.Core.Io.Serialization.Types;
using HC.Core.Logging;
using HC.Core.Threading.ProducerConsumerQueues.Support;

#endregion

namespace HC.Core.Threading.ProducerConsumerQueues
{
    public class EfficientWorkerManager<T> : IDisposable
    {
        #region Events & delegates

        public event WorkDelegate<T> OnWork;

        #endregion

        #region Properties

        public int WaitMillSec { get; set; }
        public int Threads { get; private set; }
        public int QueueSize { get { return m_producerConsumerQueue.QueueSize; }}
        public int TasksInProgress { get { return m_producerConsumerQueue.TasksInProgress; } }
        public int TasksDone { get { return m_producerConsumerQueue.TasksDone; } }
        public string Id { get; set; }

        #endregion

        #region Members

        private IThreadedQueue<EfficientWorkerItem> m_producerConsumerQueue;
        private ConcurrentDictionary<string, T> m_keyLookup;

        #endregion

        public EfficientWorkerManager(
            int intThreads,
            string strName)
            : this(intThreads, 0, strName) { }

        public EfficientWorkerManager(
            int intThreads) : this(intThreads,0) { }

        public EfficientWorkerManager(
            int intThreads,
            int intCapacity)
            : this(intThreads,
                intCapacity,
                string.Empty)
        {
        }

        public EfficientWorkerManager(
            int intThreads,
            int intCapacity,
            string strName)
        {
            try
            {
                Threads = intThreads;
                m_producerConsumerQueue =
                    new ProducerConsumerQueue<EfficientWorkerItem>(
                            intThreads,
                            intCapacity,
                            false,
                            false,
                            strName);
                m_producerConsumerQueue.OnWork += ProducerConsumerOnWork;
                m_keyLookup = new ConcurrentDictionary<string, T>();
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public TaskWrapper AddItem(string strKey, T obj)
        {
            try
            {
                if(string.IsNullOrEmpty(strKey))
                {
                    throw new HCException("Null key");
                }
                if (m_keyLookup.ContainsKey(strKey))
                {
                    //
                    // set task as completed
                    //
                    var tcs = new TaskCompletionSource<object>();
                    tcs.SetResult(obj);
                    return new TaskWrapper(tcs.Task, null);
                }
                m_keyLookup.TryAdd(strKey, obj);
                return m_producerConsumerQueue.EnqueueTask(
                    new EfficientWorkerItem
                        {
                            Str = strKey,
                            Item = obj
                        });
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        private void ProducerConsumerOnWork(EfficientWorkerItem strKey)
        {
            try
            {
                T obj;
                if (m_keyLookup.TryRemove(strKey.Str, out obj))
                {
                    WorkDelegate<T> o = OnWork;
                    if (o != null)
                    {
                        try
                        {
                            o(obj);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(ex);
                        }
                        if (WaitMillSec > 0)
                        {
                            Thread.Sleep(WaitMillSec);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }


        public void SetAutoDisposeTasks(bool blnValue)
        {
            m_producerConsumerQueue.SetAutoDisposeTasks(blnValue);
        }

        public void LogQueuePerformance(string mailQueue)
        {
        }

        public string GetLog()
        {
            string strQueueName = ComplexTypeParser.ToStringType(GetType());
            strQueueName = string.IsNullOrEmpty(strQueueName)
                                      ? typeof(T).Name
                                      : strQueueName;
            int intTasksDone = Math.Min(5000, TasksDone);

            int intCounter = (intTasksDone + QueueSize + TasksInProgress) + 1;

            string strLog = "Queue [" + strQueueName +
                            "][" + Id +
                            "]. Queue size [" + QueueSize +
                            "]. Tasks queued [" + QueueSize +
                            "]. Tasks in progress [" + TasksInProgress +
                            "]. Tasks done [" + TasksDone +
                            "]. [" + (100 * intTasksDone / intCounter) +
                            "]%";
            return strLog;
        }

        public void Dispose()
        {
            try
            {
                if (m_producerConsumerQueue != null)
                {
                    m_producerConsumerQueue.Dispose();
                    m_producerConsumerQueue = null;
                }
                if (m_keyLookup != null)
                {
                    m_keyLookup.Clear();
                    m_keyLookup = null;
                }
                EventHandlerHelper.RemoveAllEventHandlers(this);
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public bool ContainsKey(string strKey)
        {
            T valueInMap;
            return TryGetValue(strKey, out valueInMap);
        }

        public bool TryGetValue(string strKey, out T valueInMap)
        {
            try
            {
                return m_keyLookup.TryGetValue(strKey, out valueInMap);
            }
            catch(Exception ex)
            {
                valueInMap = default(T);
                Logger.Log(ex);
            }
            return false;
        }

        public void Flush()
        {
            try
            {
                m_keyLookup.Clear();
                m_producerConsumerQueue.Flush();
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

    }
}



