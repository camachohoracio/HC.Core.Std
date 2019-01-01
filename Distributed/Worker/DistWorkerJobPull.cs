#region

using System;
using System.Collections.Concurrent;
using System.Threading;
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
    public class DistWorkerJobPull : IDisposable
    {
        private const int PULL_WAIT_MILLS = 20;

        #region Members

        private DistWorker m_distWorker;
        private ConcurrentDictionary<string, ASelfDescribingClass> m_jobsToPull;
        private readonly ConcurrentDictionary<string, string> m_jobsToResendPull;
        private int m_intJobsPulled;
        private static int m_intPullIdCounter;

        #endregion

        #region Constructors

        public DistWorkerJobPull(
            DistWorker distWorker)
        {
            try
            {
                m_distWorker = distWorker;
                m_jobsToPull = new ConcurrentDictionary<string, ASelfDescribingClass>();
                m_jobsToResendPull = new ConcurrentDictionary<string, string>();

                //
                // initialize pull workers
                //
                for (int i = 0; i < distWorker.Threads; i++)
                {
                    var pullWorker = new ThreadWorker(ThreadPriority.Highest);
                    pullWorker.OnExecute += PullLoop;
                    pullWorker.Work();
                }

                //
                // subscribe to jobs being pulled
                //
                TopicSubscriberCache.GetSubscriber(distWorker.ServerName).Subscribe(
                    distWorker.GridTopic +
                    EnumDistributed.TopicControllerToWorkerPullJob.ToString() +
                    distWorker.WorkerId,
                    OnTopicControllerToWorkerPullJob);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        #endregion

        private void OnTopicControllerToWorkerPullJob(TopicMessage topicmessage)
        {
            try
            {
                string strMessage;
                var jobParams = (ASelfDescribingClass) topicmessage.EventData;
                string strWorkerId;
                if (!jobParams.TryGetStrValue(EnumDistributed.WorkerId, out strWorkerId) ||
                    string.IsNullOrEmpty(strWorkerId))
                {
                    throw new HCException("Empty worker id");
                }

                if (!m_distWorker.WorkerId.Equals(strWorkerId))
                {
                    //
                    // message sent to another worker
                    //
                    return;
                }

                string strPullId;
                if (!jobParams.TryGetStrValue(EnumDistributed.PullId, out strPullId) ||
                    string.IsNullOrEmpty(strPullId))
                {
                    //
                    // pull id not found
                    //
                    throw new HCException("Pull id not found");
                }

                string strControllerId;
                if (!jobParams.TryGetStrValue(EnumDistributed.ControllerId, out strControllerId) ||
                    string.IsNullOrEmpty(strControllerId))
                {
                    throw new HCException("Controller id not found");
                }

                //
                // the controller has the request in its queue, so stop pinging it
                //
                m_jobsToResendPull[strPullId] = strControllerId;

                //
                // check controller's answer
                //
                bool blnPullAnswer;
                if (!jobParams.TryGetBlnValue(
                    EnumDistributed.DoWorkAnswer, 
                    out blnPullAnswer) ||
                    !blnPullAnswer)
                {
                    //
                    // The controller should now have our request in its queue
                    // He will call me back once there is some work to do
                    //
                    strMessage = "Controller said [NO] at [" + DateTime.Now + "]";
                    Console.WriteLine(strMessage);
                    //Logger.Log(strMessage);

                    return;
                }

                //
                // Controller Said [YES]
                // Avoid leaving the controller waiting forever. Always ack the job back to the controller
                //
                TopicPublisherCache.GetPublisher(
                    m_distWorker.ServerName).SendMessageImmediately(
                    strPullId,
                    m_distWorker.GridTopic + 
                        EnumDistributed.TopicWorkerToControllerPullJobAck);

                strMessage = "Controller said [YES]";
                Console.WriteLine(strMessage);
                //
                // the job has been pulled
                //
                if (m_jobsToPull.ContainsKey(strPullId))
                {
                    m_jobsToPull[strPullId] = jobParams;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private void PullLoop()
        {
            while (true)
            {
                try
                {
                    //
                    // pull job
                    //
                    ASelfDescribingClass jobParams = PullJob();

                    Interlocked.Increment(ref m_intJobsPulled);
                    //
                    // do work
                    //
                    DoWork(jobParams);

                    Interlocked.Decrement(ref m_intJobsPulled);

                    if (m_intJobsPulled < 0 || m_intJobsPulled > m_distWorker.Threads)
                    {
                        throw new HCException("Invalid number of jobs pulled");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                    Thread.Sleep(5000); // slow down
                }
            }
        }

        private void DoWork(ASelfDescribingClass jobParams)
        {
            try
            {
                if (jobParams == null)
                {
                    return;
                }

                string strJobId = jobParams.GetStrValue(EnumDistributed.JobId);
                DateTime intCounter;
                if (m_distWorker.JobsCompletedMap.TryGetValue(strJobId, out intCounter))
                {
                    string strMessage = "Job igored. " + typeof (DistWorkerJobPull).Name +
                                        " job [" +
                                        strJobId + "] already done. DateTime [" +
                                        intCounter + "]";
                    Console.WriteLine(strMessage);
                    Logger.Log(strMessage);
                    return;
                }

                string strCalcType;
                jobParams.TryGetStrValue(
                    EnumCalcCols.CalcType,
                    out strCalcType);
                if (m_distWorker.CalcTypesSet.Count > 0 &&
                    !m_distWorker.CalcTypesSet.Contains(strCalcType))
                {
                    //
                    // calc type not found
                    //
                    return;
                }

                lock (LockObjectHelper.GetLockObject(strJobId))
                {
                    if (m_distWorker.JobsCompletedMap.ContainsKey(strJobId))
                    {
                        string strMessage = "Job igored. " + typeof (DistWorkerJobPull).Name +
                                            " job [" +
                                            strJobId + "] already done. Counter [" +
                                            intCounter + "]";
                        Console.WriteLine(strMessage);
                        Logger.Log(strMessage);
                        return;
                    }
                    m_distWorker.DoJob(strJobId, jobParams);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private ASelfDescribingClass PullJob()
        {
            try
            {
                SelfDescribingClass pullParams;
                string strPullId = GetPullParams(out pullParams);

                string strMessage = "Pulling job...";
                Console.WriteLine(strMessage);
                Logger.Log(strMessage);
                m_jobsToPull[strPullId] = null;
                m_jobsToResendPull[strPullId] = null;
                ASelfDescribingClass jobParams;
                int intWaitCounter = 0;

                while (TopicPublisherCache.GetPublisher(m_distWorker.ServerName) == null)
                {
                    strMessage = GetType().Name + " is waiting for topic publisher to connect...";
                    Console.WriteLine(strMessage);
                    Logger.Log(strMessage);
                    Thread.Sleep(1000);
                }

                //
                // send pull request for the first time
                //
                TopicPublisherCache.GetPublisher(m_distWorker.ServerName).SendMessage(
                    pullParams,
                    m_distWorker.GridTopic + EnumDistributed.TopicWorkerToControllerPullJob.ToString(),
                    true);

                //
                // look untill a job is pulled
                //
                int intLongWait = 0;
                while (m_jobsToPull.TryGetValue(strPullId, out jobParams) &&
                       jobParams == null)
                {
                    intWaitCounter += PULL_WAIT_MILLS;
                    intLongWait += PULL_WAIT_MILLS;
                    if (intLongWait > 60000)
                    {
                        //
                        // ping job again. Controller may have lost it...
                        //
                        m_jobsToResendPull[strPullId] = null;
                        intLongWait = 0;
                    }
                    if (intWaitCounter > 5000)
                    {
                        intWaitCounter = 0;
                        string strControllerId;
                        if (m_jobsToResendPull.TryGetValue(strPullId, out strControllerId))
                        {
                            if (string.IsNullOrEmpty(strControllerId))
                            {
                                //
                                // maybe the controller did not get the message
                                // resend request
                                //
                                TopicPublisherCache.GetPublisher(m_distWorker.ServerName).SendMessage(
                                    pullParams,
                                    m_distWorker.GridTopic +
                                    EnumDistributed.TopicWorkerToControllerPullJob.ToString(),
                                    true);
                                strMessage = "Ping pull job...";
                                Console.WriteLine(strMessage);
                                Logger.Log(strMessage);
                            }
                            else if (m_distWorker.DistWorkerToContollerHeartBeat
                                .IsControllerDisconnected(strControllerId))
                            {
                                //
                                // Controller is disconnected.
                                // We need to start pinging again the controller
                                //
                                strMessage = "Controller disconnected job id [" + strPullId + "]";
                                Console.WriteLine(strMessage);
                                Logger.Log(strMessage);
                                m_jobsToResendPull[strPullId] = null;
                            }
                        }
                    }
                    Thread.Sleep(PULL_WAIT_MILLS);
                }

                if (jobParams == null)
                {
                    throw new HCException("Null jobs params");
                }
                //
                // the pull map is no longer required
                //
                ASelfDescribingClass dummy;
                m_jobsToPull.TryRemove(strPullId, out dummy);
                string strDummy;
                m_jobsToResendPull.TryRemove(strPullId, out strDummy);

                strMessage = "Successfully pulled job id [" + strPullId + "]";
                Verboser.WriteLine(strMessage);
                Logger.Log(strMessage);

                return jobParams;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        private string GetPullParams(out SelfDescribingClass pullParams)
        {
            try
            {
                Interlocked.Increment(ref m_intPullIdCounter);
                string strPullId = "pull_" + m_intPullIdCounter + "_" + HCConfig.ClientUniqueName + "_" + Guid.NewGuid().ToString();
                pullParams = new SelfDescribingClass();
                pullParams.SetClassName(GetType().Name + EnumDistributed.PullParams);
                pullParams.SetStrValue(EnumDistributed.WorkerId, m_distWorker.WorkerId);
                pullParams.SetStrValue(EnumDistributed.PullId, strPullId);
                return strPullId;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            pullParams = null;
            return string.Empty;
        }

        public void Dispose()
        {
            try
            {
                m_distWorker = null;
                if (m_jobsToPull != null)
                {
                    m_jobsToPull.Clear();
                    m_jobsToPull = null;
                }
                if (m_jobsToResendPull != null)
                {
                    m_jobsToResendPull.Clear();
                }
                HC.Core.EventHandlerHelper.RemoveAllEventHandlers(this);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }
    }
}
