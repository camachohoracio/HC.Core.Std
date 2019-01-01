#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HC.Core.Comunication.TopicBased;
using HC.Core.Comunication.TopicBased.Contracts;
using HC.Core.DynamicCompilation;
using HC.Core.Logging;
using HC.Core.Threading;

#endregion

namespace HC.Core.Distributed.Controller
{
    public class DistControllerToWorkerHeartBeat : IDisposable
    {
        #region Properties

        public ConcurrentDictionary<string, string> WorkersStatus { get; private set; }
        //public RollingWindowStdDev PingLatencySecs { get; private set; }
        public ConcurrentDictionary<string, DateTime> WorkersPingTimes { get; private set; }

        #endregion

        #region Members

        private readonly string m_strControllerId;
        private DistController m_distController;
        private ThreadWorker m_clockThreadWorker;

        #endregion

        #region Constructors

        public DistControllerToWorkerHeartBeat(DistController distController)
        {
            try
            {
                m_distController = distController;
                m_strControllerId = distController.ControllerId;
                //PingLatencySecs = new RollingWindowStdDev(20);
                WorkersPingTimes = new ConcurrentDictionary<string, DateTime>();
                WorkersStatus = new ConcurrentDictionary<string, string>();
                string strTopic = m_distController.GridTopic + EnumDistributed.TopicWorkerToControllerHeartBeat.ToString();
                TopicSubscriberCache.GetSubscriber(
                    distController.ServerName,
                    TopicConstants.PUBLISHER_HEART_BEAT_PORT).Subscribe(
                    strTopic,
                    OnTopicWorkerToControllerHeartBeat);
                m_clockThreadWorker = new ThreadWorker(ThreadPriority.Highest);
                m_clockThreadWorker.OnExecute += OnClockTick;
                m_clockThreadWorker.Work();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        #endregion

        #region Private

        private void OnTopicWorkerToControllerHeartBeat(TopicMessage topicmessage)
        {
            try
            {
                var workerResponse = (ASelfDescribingClass)(topicmessage.EventData);
                string strWorkerId = workerResponse.GetStrValue(EnumDistributed.WorkerId);
                DateTime timeSent = workerResponse.GetDateValue(EnumDistributed.TimeControllerToWorker);
                var now = DateTime.Now;
                //PingLatencySecs.Update((now - timeSent).TotalSeconds);
                
                if (!WorkersPingTimes.ContainsKey(strWorkerId))
                {
                    string strMessage = "Connected worker [" + strWorkerId + "]";
                    Console.WriteLine(strMessage);
                    //DistGuiHelper.PublishControllerLog(m_distController, strMessage);
                }
                WorkersPingTimes[strWorkerId] = now;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private void OnClockTick()
        {
            DistGuiHelper.PublishControllerLog(m_distController, "Started worker pinger...");
            while (true)
            {
                try
                {
                    PingWorker();
                    CheckAliveWorkers();
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
                Thread.Sleep(1000 * DistConstants.PING_WORKER_TIME_SECS);
            }
        }

        private void CheckAliveWorkers()
        {
            try
            {
                DateTime now = DateTime.Now;
                foreach (var kvp in WorkersPingTimes.ToArray())
                {
                    int intTotalSeconds = (int) (now - kvp.Value).TotalSeconds;
                    if (intTotalSeconds > DistConstants.ALIVE_WORKER_TIME_SECS)
                    {
                        RemoveWorker(kvp, intTotalSeconds);
                        DateTime dummy;
                        WorkersPingTimes.TryRemove(kvp.Key, out dummy);
                    }
                    else
                    {
                        WorkersStatus[kvp.Key] = EnumDistributed.Connected.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public void RemoveJobsInProgressFromRequestor(string strRequestorName)
        {
            try
            {
                KeyValuePair<string, ASelfDescribingClass>[] jobsInProgressArr;
                lock (m_distController.DistControllerJobPull.JobsInProgressLock)
                {
                    jobsInProgressArr = m_distController.JobsToDoMap.ToArray();
                }
                for (int i = 0; i < jobsInProgressArr.Length; i++)
                {
                    bool blnDoRemove = false;
                    string strJobId = jobsInProgressArr[i].Key;
                    ASelfDescribingClass currParams = jobsInProgressArr[i].Value;
                    string strCurrRequestorName;
                    if (currParams.TryGetStrValue(EnumDistributed.RequestorName,
                                                  out strCurrRequestorName) &&
                        !string.IsNullOrEmpty(strCurrRequestorName))
                    {
                        if (strCurrRequestorName.Equals(strRequestorName))
                        {
                            blnDoRemove = true;
                        }
                    }
                    else
                    {
                        blnDoRemove = true;
                    }
                    if (blnDoRemove)
                    {
                        lock (m_distController.DistControllerJobPull.JobsInProgressLock)
                        {
                            ASelfDescribingClass resultTsEv;
                            if (m_distController.JobsToDoMap.TryRemove(
                                jobsInProgressArr[i].Key,
                                out resultTsEv))
                            {
                                string strMessage = "Calc engine successfully flushed job [" + strJobId +
                                    "] from client [" + strRequestorName + "]";
                                var resultObj = new SelfDescribingClass();
                                resultObj.SetClassName(GetType().Name + "_ResultFlush");
                                DistGuiHelper.PublishControllerLog(
                                    m_distController, 
                                    strMessage);
                                resultObj.SetBlnValue(
                                    EnumCalcCols.IsClientDisconnected,
                                    true);
                                resultObj.SetStrValue(
                                    EnumCalcCols.Error,
                                    strMessage);
                                resultTsEv.SetObjValueToDict(
                                    EnumCalcCols.Result,
                                    resultObj);
                            }
                            ASelfDescribingClass jobLog;
                            m_distController.DistControllerJobPull.MapJobIdToWorkerId.TryRemove(
                                strJobId,
                                out jobLog);
                            DistGuiHelper.PublishJobLogStatus(
                                m_distController,
                                jobLog,
                                "Removed");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public bool IsWorkerConnected(string strWorkerId)
        {
            string workerStatus;
            if(!WorkersStatus.TryGetValue(strWorkerId, out workerStatus))
            {
                return true;
            }
            return workerStatus.Equals(EnumDistributed.Connected.ToString());
        }

        private void RemoveWorker(KeyValuePair<string, DateTime> kvp, int intTotalSeconds)
        {
            try
            {
                string strWorkerId = kvp.Key;
                DistGuiHelper.PublishControllerLog(
                    m_distController,
                    "Disconnected worker[" +
                        strWorkerId + "][" + intTotalSeconds + "]secs");

                WorkersStatus[strWorkerId] = EnumDistributed.Disconnected.ToString();
                List<string> assignedJobs = (from n in m_distController.DistControllerJobPull.MapJobIdToWorkerId
                                   where DistControllerJobLogger.GetWorkerId(n.Value).Equals(strWorkerId)
                                   select n.Key).ToList();
                foreach (string strJobId in assignedJobs)
                {
                    ASelfDescribingClass jobLog;
                    m_distController.DistControllerJobPull.MapJobIdToWorkerId.TryRemove(
                        strJobId, out jobLog);
                    DistGuiHelper.PublishJobLogStatus(
                        m_distController,
                        jobLog,
                        "ClientDisconnected");
                    DistGuiHelper.PublishControllerLog(m_distController,
                        "Removed worker[" +
                        strWorkerId + "]. Job id [" +
                        strJobId +"]");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private void PingWorker()
        {
            try
            {
                if(m_distController.DistTopicQueue == null)
                {
                    return;
                }
                var calcParams = new SelfDescribingClass();
                calcParams.SetClassName(EnumDistributed.HeartBeatWorkerClass);
                calcParams.SetStrValue(
                    EnumDistributed.ControllerId,
                    m_strControllerId);
                calcParams.SetDateValue(
                    EnumDistributed.TimeControllerToWorker,
                    DateTime.Now);
                calcParams.SetDateValue(
                    EnumDistributed.Time,
                    DateTime.Now);
                string strTopic = m_distController.GridTopic + 
                                EnumDistributed.TopicControllerToWorkerHeartBeat.ToString();
                TopicPublisherCache.GetPublisher(
                    m_distController.ServerName,
                    TopicConstants.SUBSCRIBER_HEART_BEAT_PORT).SendMessageImmediately(
                    calcParams,
                    strTopic);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        #endregion

        public void Dispose()
        {
            if(WorkersStatus != null)
            {
                WorkersStatus.Clear();
                WorkersStatus = null;
            }
            //if(PingLatencySecs != null)
            //{
            //    PingLatencySecs.Dispose();
            //    PingLatencySecs = null;
            //}

            if(WorkersPingTimes != null)
            {
                WorkersPingTimes.Clear();
                WorkersPingTimes = null;
            }
             m_distController = null;

            if(m_clockThreadWorker != null)
            {
                m_clockThreadWorker.Dispose();
                m_clockThreadWorker = null;
            }
            HC.Core.EventHandlerHelper.RemoveAllEventHandlers(this);
        }
    }
}