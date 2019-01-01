#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using HC.Core.Comunication;
using HC.Core.Comunication.TopicBased;
using HC.Core.Comunication.TopicBased.Contracts;
using HC.Core.ConfigClasses;
using HC.Core.DynamicCompilation;
using HC.Core.Exceptions;
using HC.Core.Logging;
using HC.Core.Threading;

#endregion

namespace HC.Core.Distributed.Worker
{
    public class DistWorker : IDisposable
    {
        #region Properties
        public static int ThreadsStatic { get; set; }

        public int Threads { get; private set; }
        public string GridTopic { get; private set; }
        public int JobsInProgress { get; private set; }
        public string ServerName { get; private set; }
        public DistTopicQueue DistTopicQueue { get; set; }

        public int JobsCompleted
        {
            get { return JobsCompletedMap.Count; }
        }

        public string WorkerId { get; private set; }

        public DistWorkerToContollerHeartBeat DistWorkerToContollerHeartBeat { get; private set; }

        public ConcurrentDictionary<string, DateTime> JobsCompletedMap { get; private set; }
        public HashSet<string> CalcTypesSet { get; private set; }
        
        #endregion

        #region Members

        private ConcurrentDictionary<string, ASelfDescribingClass> m_jobsToDo;
        private static readonly object m_connectionLock = new object();
        private DistWorkerResultSender m_distWorkerResultSender;
        private readonly object m_progressLock = new object();
        private ThreadWorker m_clockTickWorker;
        private DistWorkerJobPull m_distWorkerJobPull;
        private static int m_intWorkerCounter;
        private readonly ThreadWorker m_jobsDoneFlusherWorker;

        #endregion

        #region Properties

        public static DistWorker OwnInstance { get; private set; }

        #endregion

        #region Constructors

        private DistWorker(
            string strServerName,
            Assembly callingAssembly)
        {
            ServerName = strServerName;
            JobsCompletedMap = new ConcurrentDictionary<string, DateTime>();

            Threads = Config.GetCalcThreads();
            string strTopicFromConfig = Config.GetCalcTopic(
                callingAssembly);

            CalcTypesSet =
                new HashSet<string>(
                    Config.GetCalcTypes());
            if (string.IsNullOrEmpty(strTopicFromConfig))
            {
                throw new HCException("Invalid grid topic");
            }
            else
            {
                GridTopic = strTopicFromConfig;
            }
            if (string.IsNullOrEmpty(GridTopic))
            {
                throw new HCException("Empty grid topic");
            }

            Interlocked.Increment(ref m_intWorkerCounter);

            WorkerId = "Worker_" +
                       HCConfig.ClientUniqueName + "_" +
                       GridTopic + "_" +
                       m_intWorkerCounter;

            m_jobsToDo = new ConcurrentDictionary<string, ASelfDescribingClass>();

            DistTopicQueue = new DistTopicQueue(strServerName);
            SubscribeToTopics();
            DistWorkerToContollerHeartBeat = new DistWorkerToContollerHeartBeat(this);
            SetupClockTick();
            m_distWorkerResultSender = new DistWorkerResultSender(this);
            m_distWorkerJobPull = new DistWorkerJobPull(this);

            m_jobsDoneFlusherWorker = new ThreadWorker();
            m_jobsDoneFlusherWorker.OnExecute += () =>
                                                     {
                                                         try
                                                         {
                                                             while (true)
                                                             {
                                                                 try
                                                                 {
                                                                     if (JobsCompletedMap.Count > (int) 10e3)
                                                                     {
                                                                         var jobsToRemove = new List<String>();
                                                                         foreach (
                                                                             KeyValuePair<string, DateTime> kvp in
                                                                                 JobsCompletedMap)
                                                                         {
                                                                             double intMins =
                                                                                 (DateTime.Now - kvp.Value).TotalMinutes;
                                                                             if (intMins > 30)
                                                                             {
                                                                                 jobsToRemove.Add(kvp.Key);
                                                                             }
                                                                         }

                                                                         if (jobsToRemove.Count > 0)
                                                                         {
                                                                             foreach (string strJobId in jobsToRemove)
                                                                             {
                                                                                 DateTime dummy;
                                                                                 JobsCompletedMap.TryRemove(
                                                                                     strJobId,
                                                                                     out dummy);
                                                                             }
                                                                         }
                                                                     }
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
                                                         catch (Exception ex)
                                                         {
                                                             Logger.Log(ex);
                                                         }
                                                     };
            m_jobsDoneFlusherWorker.Work();

            //
            // log connection
            //
            string strMessage = GetType().Name + " is connected to [" + strServerName +
                                "] via topic [" + strTopicFromConfig + "]";
            Console.WriteLine(strMessage);
            Logger.Log(strMessage);
        }

        private void SetupClockTick()
        {
            m_clockTickWorker = new ThreadWorker(ThreadPriority.Highest);
            m_clockTickWorker.OnExecute += () =>
                                               {
                                                   while (true)
                                                   {
                                                       try
                                                       {
                                                           OnClockTick();
                                                       }
                                                       catch (Exception ex)
                                                       {
                                                           Logger.Log(ex);
                                                       }
                                                       Thread.Sleep(DistConstants.WORKER_PUBLISH_GUI_TIME_SECS*1000);
                                                   }
                                               };
            m_clockTickWorker.Work();
        }

        #endregion

        #region Public

        public static void Connect(string strServerName)
        {
            try
            {
                if (NetworkHelper.IsADistWorkerConnected)
                {
                    return;
                }
                lock (m_connectionLock)
                {
                    if (NetworkHelper.IsADistWorkerConnected)
                    {
                        return;
                    }
                    OwnInstance = new DistWorker(
                        strServerName,
                        Assembly.GetCallingAssembly());
                    NetworkHelper.IsADistWorkerConnected = true;
                    Logger.Log(OwnInstance.GetType().Name + " is now connected. Id = " +
                               OwnInstance.WorkerId);
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        #endregion

        #region Private

        private void OnClockTick()
        {
            DistGuiHelper.PublishWorkerStats(this);
        }

        
        private void SubscribeToTopics()
        {
            try
            {
                TopicSubscriberCache.GetSubscriber(ServerName).Subscribe(
                    GridTopic + EnumDistributed.JobsDoneTopic.ToString(),
                    OnTopicControllerJobsDone);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private void OnTopicControllerJobsDone(
            TopicMessage topicmessage)
        {
            var strJobId = (string) topicmessage.EventData;
            JobsCompletedMap[strJobId] = DateTime.Now;
        }

        public void DoJob(
            string strJobId,
            ASelfDescribingClass selfDescribingClass)
        {
            try
            {
                DateTime workTimeLog = DateTime.Now;
                lock (m_progressLock)
                {
                    JobsInProgress++;
                }

                //DistGuiHelper.PublishWorkerLog(this, "Running job params [" +
                //                                     selfDescribingClass + "]",
                //                                     strJobId);
                string strValue;
                if(selfDescribingClass.TryGetStrValue("MethodClassName", out strValue))
                {
                    Console.WriteLine(
                        Environment.NewLine +
                        typeof(DistWorker).Name + ". Running [" +
                        strValue + "]");
                }

                string strParentId = selfDescribingClass.GetStrValue(EnumDistributed.ControllerId);
                List<ITsEvent> events = CalcDataProvider.OwnInstance.GetCalcs(
                    selfDescribingClass).ToList();
                if (events.Count == 0)
                {
                    string strMessage = "Calc returned empty results [" +
                                        strJobId + "]";
                    DistGuiHelper.PublishWorkerLog(this, strMessage, strJobId);
                    throw new HCException(strMessage);
                }
                //
                // send result
                //
                bool blnResultIsSent = m_distWorkerResultSender.SendResult(
                    events,
                    strParentId,
                    strJobId);

                if (blnResultIsSent)
                {
                    DistGuiHelper.PublishWorkerLog(this, "Sent result. JobId [" +
                                                         strJobId + "]", strJobId);
                    JobsCompletedMap[strJobId] = DateTime.Now;
                }
                else
                {
                    DateTime intDummy;
                    JobsCompletedMap.TryRemove(strJobId, out intDummy);
                    DistGuiHelper.PublishWorkerLog(this, "Could not send result for [" +
                                                         strJobId + "]", strJobId);
                }

                ASelfDescribingClass dummyVal;
                m_jobsToDo.TryRemove(strJobId, out dummyVal);

                lock (m_progressLock)
                {
                    JobsInProgress--;
                }

                DistGuiHelper.PublishWorkerLog(this, "Job [" +
                                                     strJobId + "] done in [" +
                                                     (DateTime.Now - workTimeLog).TotalMinutes + "] mins", strJobId);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        #endregion

        public void Dispose()
        {
            if(DistTopicQueue != null)
            {
                DistTopicQueue.Dispose();
                DistTopicQueue = null;
            }

            if(DistWorkerToContollerHeartBeat != null)
            {
                DistWorkerToContollerHeartBeat.Dispose();
                DistWorkerToContollerHeartBeat = null;
            }

            if(JobsCompletedMap != null)
            {
                JobsCompletedMap.Clear();
                JobsCompletedMap = null;
            }

            if(CalcTypesSet != null)
            {
                CalcTypesSet.Clear();
                CalcTypesSet = null;
            }

            if(m_jobsToDo != null)
            {
                m_jobsToDo.Clear();
                m_jobsToDo = null;
            }

            if(m_distWorkerResultSender != null)
            {
                m_distWorkerResultSender.Dispose();
                m_distWorkerResultSender = null;
            }

            if(m_distWorkerJobPull != null)
            {
                m_distWorkerJobPull.Dispose();
                m_distWorkerJobPull = null;
            }

            if(m_clockTickWorker != null)
            {
                m_clockTickWorker.Dispose();
                m_clockTickWorker = null;
            }

            if(m_distWorkerJobPull != null)
            {
                m_distWorkerJobPull.Dispose();
                m_distWorkerJobPull = null;
            }
            HC.Core.EventHandlerHelper.RemoveAllEventHandlers(this);
        }
    }
}