#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HC.Core.Cache;
using HC.Core.Distributed.Worker;
using HC.Core.DynamicCompilation;
using HC.Core.Exceptions;
using HC.Core.Helpers;
using HC.Core.Logging;
using HC.Core.Reflection;
using HC.Core.Threading;
using HC.Core.Threading.ProducerConsumerQueues;
using HC.Core.Threading.ProducerConsumerQueues.Support;

#endregion

namespace HC.Core.Distributed
{
    public class CalcDataProvider
    {
        #region Members

        private static readonly string m_strCalcPath;
        private readonly int m_intThreads;
        private readonly ThreadWorker m_logworker;
        private readonly IThreadedQueue<ITsCalcWorker> m_workQueue;
        private int m_intTasksInProgress;
        private int m_intTotalTasksDone;
        private int m_intTotalTasksRequested;

        #endregion

        #region Properties

        public static CalcDataProvider OwnInstance { get; private set; }

        #endregion

        #region Constructors

        static CalcDataProvider()
        {
            try
            {
                m_strCalcPath = Core.Config.GetDefaultCacheDataPath();
                OwnInstance =
                    new CalcDataProvider(
                        Core.Config.GetCalcThreads());
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private CalcDataProvider(int intThreads)
        {
            m_intThreads = intThreads;
            m_workQueue =
                new ProducerConsumerQueueLite<ITsCalcWorker>(
                    intThreads);
            m_workQueue.OnWork +=
                WorkQueueOnWork;
            m_logworker = new ThreadWorker();
            m_logworker.OnExecute += OnLogger;
            m_logworker.Work();
        }

        #endregion

        #region Public

        public List<ITsEvent> GetCalcs(
            ASelfDescribingClass calcParams)
        {
            //
            // create instance of the requested worker
            //
            ITsCalcWorker tsCalcWorker = LoadCalcWorker(calcParams);

            //
            // safely enqueue tasks for its future consumption
            // the queue will be consumed according to the number of threads specified
            //
            using (TaskWrapper currentTask = m_workQueue.EnqueueTask(
                tsCalcWorker))
            {
                //
                // wait for all the tasks to complete
                //
                currentTask.Wait();

                //
                // collect calc events from worker
                //
                List<ITsEvent> calcTsEvents =
                    GetCalcTsEvents(tsCalcWorker);

                return calcTsEvents;
            }
        }

        private void OnLogger()
        {
            var intTaksInProgress = 0;
            var intQueueSize = 0;

            while (true)
            {
                if (intQueueSize != m_workQueue.QueueSize ||
                    intTaksInProgress != m_intTasksInProgress)
                {
                    UpdateStatusLog(
                        out intTaksInProgress,
                        out intQueueSize);
                }
                Thread.Sleep(10000);
            }
        }

        private void UpdateStatusLog(
            out int intTasksInProgress,
            out int intQueueSize)
        {
            var strMessage = GetType().Name +
                             ": Workers = " +
                             m_intThreads +
                             ". Tasks in the queue " +
                             m_workQueue.QueueSize +
                             ". Tasks in progress = " +
                             m_intTasksInProgress +
                             ". Total tasks requested = " +
                             m_intTotalTasksRequested +
                             ". Total taks done = " +
                             m_intTotalTasksDone;
            Logger.Log(strMessage);
            PrintToScreen.WriteLine(strMessage);
            intQueueSize = m_workQueue.QueueSize;
            intTasksInProgress = m_intTasksInProgress;
        }

        private static ITsCalcWorker LoadCalcWorker(ASelfDescribingClass calcParams)
        {
            var strClassName =
                calcParams.GetStrValue(
                    EnumCalcCols.ClassName);
            var strCalcName =
                strClassName.Split('.').Last();
            var strAssemblyName =
                calcParams.GetStrValue(
                    EnumCalcCols.AssemblyName);
            
            bool blnCsvCache;
                calcParams.TryGetBlnValue(
                    EnumCalcCols.CsvCache, 
                    out blnCsvCache);

            Type calcType = Type.GetType(
                strClassName + "," +
                strAssemblyName);

            var tsCalcWorker =
                ReflectorCache.GetReflector(calcType).CreateInstance()
                as ITsCalcWorker;

            if (tsCalcWorker == null)
            {
                throw new HCException("Unable to load calc worker");
            }
            tsCalcWorker.Params = calcParams;
            var strResourceName =
                tsCalcWorker.GetResourceName();
            tsCalcWorker.Resource = strResourceName;

            //
            // add cache operator
            //
            bool blnDoCache;
            calcParams.TryGetBlnValue(
                EnumCalcCols.DoCache,
                out blnDoCache);
            if (blnDoCache)
            {
                CacheDictionary<string, List<ITsEvent>> cache =
                    GetCacheDictionary(
                        strCalcName,
                        blnCsvCache);
                tsCalcWorker.Cache = cache;
            }

            tsCalcWorker.TsEvents = new List<ITsEvent>();
            return tsCalcWorker;
        }

        private static CacheDictionary<string, List<ITsEvent>> GetCacheDictionary(
            string strSymbol,
            bool blnCsvCache)
        {
            var strPath = m_strCalcPath + @"\" +
                          strSymbol;

            CacheDictionary<string, List<ITsEvent>> dictionaryCache;
            try
            {
                var strCacheName = blnCsvCache
                                       ? "TsCsvCache"
                                       : typeof (StdCache).Name;
                dictionaryCache =
                    new CacheDictionary<string, List<ITsEvent>>(
                        strCacheName,
                        string.Empty,
                        strPath,
                        typeof(CalcDataProvider).Name,
                        false);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                //
                // the path is too large
                //
                strPath = Math.Abs(strPath.GetHashCode()).ToString();
                dictionaryCache =
                    new CacheDictionary<string, List<ITsEvent>>(
                        strPath,
                        false);
            }
            return dictionaryCache;
        }

        private static List<ITsEvent> GetCalcTsEvents(
            ITsCalcWorker tsCalcWorker)
        {
            var calcTsEvents = new List<ITsEvent>();
            calcTsEvents.AddRange(tsCalcWorker.TsEvents);
            calcTsEvents.Sort(
                (a,b) => a.Time.CompareTo(b.Time));
            return calcTsEvents;
        }

        private void WorkQueueOnWork(ITsCalcWorker worker)
        {
            try
            {
                var lockObject = LockObjectHelper.GetLockObject(
                    worker.Resource);
                var logStartTime = DateTime.Now;
                PrintToScreen.WriteLine("Requesting task [" + worker.Resource + "]...");

                //
                // update progress
                //
                Interlocked.Increment(ref m_intTasksInProgress);
                Interlocked.Increment(ref m_intTotalTasksRequested);
                int intQueueSize;
                int intTasksInProgress;
                UpdateStatusLog(
                    out intTasksInProgress,
                    out intQueueSize);

                //
                // the lock will prevent two processes to request the same item
                // at the same time
                //
                lock (lockObject)
                {
                    if (!worker.DoCache ||
                        (worker.DoCache && !worker.Cache.ContainsKey(worker.Resource)))
                    {
                        worker.Work();

                        if (worker.DoCache)
                        {
                            //
                            // add to cache
                            //
                            worker.Cache.Add(
                                worker.Resource,
                                worker.TsEvents);
                        }
                    }
                    else if (worker.DoCache)
                    {
                        worker.TsEvents = worker.Cache[worker.Resource];
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }

                Interlocked.Decrement(ref m_intTasksInProgress);
                Interlocked.Increment(ref m_intTotalTasksDone);

                PrintToScreen.WriteLine("Finish loading task  [" + worker.Resource + "] in " +
                                        (DateTime.Now - logStartTime).TotalSeconds + " seconds ");
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        #endregion
    }
}
