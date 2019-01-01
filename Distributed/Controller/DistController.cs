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
//using HC.Analytics.TimeSeries;

#endregion

namespace HC.Core.Distributed.Controller
{
    public class DistController : IDisposable
    {
        #region Properties

        public ConcurrentDictionary<string, ASelfDescribingClass> JobsToDoMap { get; private set; }
        public string ServerName { get; private set; }
        public string ControllerId { get; private set; }
        public string GridTopic { get; private set; }
        private static ConcurrentDictionary<string, DistController> Instances { get; set; }
        public int JobsDone
        {
            get { return m_intJobsDone; }
        }

        public DistControllerToWorkerHeartBeat DistControllerToWorkerHeartBeat { get; private set; }

        public DistTopicQueue DistTopicQueue { get; private set; }
        public bool IsReady { get; private set; }

        public ConcurrentDictionary<string, JobDoneWrapper> JobsDoneMap { get; private set; }

        public DistControllerJobPull DistControllerJobPull { get; private set; }
        
        #endregion

        #region Members

        private static readonly object m_connectionLock = new object();
        private int m_intJobsDone;
        private readonly object m_jobReadyLock = new object();
        private static int m_intControllerCounter;

        #endregion

        #region Constructors

        static DistController()
        {
            Instances = new ConcurrentDictionary<string, DistController>();
        }

        private DistController(string strGridTopic)
        {
            try
            {
                if (string.IsNullOrEmpty(strGridTopic))
                {
                    throw new HCException("Null grid topic");
                }

                JobsDoneMap = new ConcurrentDictionary<string, JobDoneWrapper>();
                DistGuiHelper.ResetGui();
                //
                // get params first
                //
                Interlocked.Increment(ref m_intControllerCounter);
                JobsToDoMap = new ConcurrentDictionary<string, ASelfDescribingClass>();
                ControllerId = 
                    "Controller_" +
                    HCConfig.ClientUniqueName + "_" +
                    GridTopic + "_" +
                    m_intControllerCounter;
                ServerName = Core.Config.GetTopicServerName();
                GridTopic = strGridTopic;

                //
                // set objects
                //
                DistControllerToWorkerHeartBeat = new DistControllerToWorkerHeartBeat(this);
                TopicSubscriberCache.GetSubscriber(ServerName).Subscribe(
                    GridTopic + EnumDistributed.TopicWorkerToControllerResult.ToString(),
                    OnTopicWorkerToControllerResult);

                DistTopicQueue = new DistTopicQueue(ServerName);
                DistControllerJobPull = new DistControllerJobPull(this);
                IsReady = true;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        #endregion

        #region Public

        public static DistController GetController(string strTopic)
        {
            try
            {
                DistController distController;
                if (Instances.TryGetValue(strTopic, out distController))
                {
                    return distController;
                }
                lock (m_connectionLock)
                {
                    if (Instances.TryGetValue(strTopic, out distController))
                    {
                        return distController;
                    }
                    distController = new DistController(strTopic);
                    Instances[strTopic] = distController;
                    Logger.Log(Instances.GetType().Name + " is now connected. Id = " +
                        Instances[strTopic].ControllerId);
                    return distController;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public ASelfDescribingClass DoWork(ASelfDescribingClass paramsClass)
        {
            try
            {
                if(string.IsNullOrEmpty(paramsClass.GetClassName()))
                {
                    throw new HCException("Empty class name");
                }

                string strJobId;
                if (!paramsClass.TryGetStrValue(EnumDistributed.JobId, out strJobId) ||
                    string.IsNullOrEmpty(strJobId))
                {
                    throw new HCException("Job id not found");
                }
                lock (LockObjectHelper.GetLockObject(strJobId))
                {
                    string strMessage;
                    JobDoneWrapper dummy;
                    if(JobsDoneMap.TryGetValue(strJobId, out dummy))
                    {
                        strMessage = "Job[" + strJobId + "] already done. Found in map";
                        Console.WriteLine(strMessage);
                        Logger.Log(strMessage);
                        return null;
                    }
                    paramsClass.SetStrValue(
                        EnumDistributed.ControllerId,
                        ControllerId);

                    //
                    // check flush task
                    //
                    bool blnFlushJobs;
                    if (paramsClass.TryGetBlnValue(
                        EnumCalcCols.FlushJobs,
                        out blnFlushJobs) &&
                        blnFlushJobs)
                    {
                        SelfDescribingClass resultTsEv = DistControllerJobFlush.DoFlushJobs(
                            this,
                            paramsClass,
                            strJobId,
                            DistControllerJobPull,
                            JobsToDoMap);
                        return resultTsEv;
                    }

                    //
                    // this is the job to be pulled by the workers
                    //
                    JobsToDoMap[strJobId] = paramsClass; // make sure this is set before advertising it!

                    //
                    // wait until job is done
                    //
                    int intWaitCounter = 0;
                    object results = null;
                    while (JobsToDoMap.ContainsKey(strJobId) && // if it is not here, then it has been flushed
                        (!DistControllerJobPull.MapJobIdToWorkerId.ContainsKey(strJobId) ||
                           (!paramsClass.TryGetObjValue(
                               EnumDistributed.Result,
                               out results) ||
                            results == null)))
                    {
                        intWaitCounter += 100;
                        if (intWaitCounter > 5000)
                        {
                            intWaitCounter = 0;
                            ASelfDescribingClass workLog;
                            if (!DistControllerJobPull.MapJobIdToWorkerId.TryGetValue(strJobId, out workLog))
                            {
                                strMessage = "Controller is wating for any worker to pickup JobId [" +
                                             strJobId + "]";
                                Verboser.WriteLine(strMessage);
                                Logger.Log(strMessage);
                            }
                            else
                            {
                                if(JobsDoneMap.ContainsKey(strJobId))
                                {
                                    strMessage = "Job[" + strJobId + "] already done";
                                    Console.WriteLine(strMessage);
                                    Logger.Log(strMessage);
                                    return null;
                                }
                                string strWorkerId = workLog.GetStrValue(EnumDistributedGui.WorkerId);
                                strMessage = "Controller is wating for worker [" + strWorkerId + "] to send result";
                                Verboser.WriteLine(strMessage);
                                Logger.Log(strMessage);
                            }
                        }
                        Thread.Sleep(100);
                    }
                    strMessage = "-Result found for jobID [" +
                                 strJobId + "]";
                    Verboser.WriteLine(strMessage);
                    Logger.Log(strMessage);

                    //
                    // get rid of the job
                    //
                    lock (m_jobReadyLock)
                    {
                        bool blnSucessDone = true;
                        ASelfDescribingClass resultsDummy;
                        if (!JobsToDoMap.TryRemove(strJobId, out resultsDummy))
                        {
                            strMessage = "Job in progress not found: " + strJobId;
                            DistGuiHelper.PublishControllerLog(this, strMessage);
                            blnSucessDone = false;
                        }
                        ASelfDescribingClass jobLog;
                        lock (DistControllerJobPull.JobsInProgressLock)
                        {
                            if (!DistControllerJobPull.MapJobIdToWorkerId.TryRemove(
                                strJobId, out jobLog))
                            {
                                strMessage = "JobId to worker not found: " + strJobId;
                                DistGuiHelper.PublishControllerLog(this, strMessage);
                                blnSucessDone = false;
                            }
                        }
                        if (blnSucessDone && results == null)
                        {
                            strMessage = "Null result for job[ " + strJobId + "]";
                            DistGuiHelper.PublishControllerLog(this, strMessage);
                            blnSucessDone = false;
                        }
                        if (blnSucessDone)
                        {
                            strMessage = "**-- Success job done [" + strJobId + "]";
                            Verboser.WriteLine(strMessage);
                            Logger.Log(strMessage);
                            string strWorkerId = DistControllerJobLogger.GetWorkerId(jobLog);
                            int intJobsDone;
                            DistControllerJobPull.MapWorkerToJobsDone.TryGetValue(
                                strWorkerId,
                                out intJobsDone);
                            intJobsDone++;
                            DistControllerJobPull.MapWorkerToJobsDone[strWorkerId] = intJobsDone;
                            DistGuiHelper.PublishJobLogDone(this, jobLog);
                        }

                        ASelfDescribingClass dummy2;
                        DistControllerJobPull.MapJobIdToWorkerId.TryRemove(
                            strJobId,
                            out dummy2);

                        JobsDoneMap[strJobId] = new JobDoneWrapper
                                                    {
                                                        SucessDone = blnSucessDone
                                                    };
                        return (ASelfDescribingClass) results;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            finally
            {
                //
                // the reult flag is no longer needed
                //
                var objValues = paramsClass.GetObjValues();
                if (objValues != null)
                {
                    objValues.Remove(EnumCalcCols.Result.ToString());
                }
                Interlocked.Increment(ref m_intJobsDone);
            }
            throw new HCException("Null result");
        }

        #endregion

        #region Private

        private void OnTopicWorkerToControllerResult(TopicMessage topicmessage)
        {
            try
            {
                var results = (ASelfDescribingClass)(topicmessage.EventData);
                string strControllerId = results.GetStrValue(EnumDistributed.ControllerId);
                if (!strControllerId.Equals(ControllerId))
                {
                    //
                    // this result was sent to another parent
                    //
                    return;
                }
                string strJobId = results.GetStrValue(EnumDistributed.JobId);

                bool blnJobFound = false;
                ASelfDescribingClass resultWaiting;
                if (!JobsToDoMap.TryGetValue(strJobId, out resultWaiting) ||
                    resultWaiting == null)
                {
                    string strMessage = "Result not found. JobId [" + 
                        strJobId + "]";
                    DistGuiHelper.PublishControllerLog(this,strMessage);
                }
                else
                {
                    string strMessage = "Found result. JobId [" +
                        strJobId + "]";
                    Verboser.WriteLine(strMessage);
                    Logger.Log(strMessage);
                    blnJobFound = true;
                    resultWaiting.SetObjValueToDict(
                        EnumDistributed.Result,
                        results);
                }
                string strWorkerId;
                if(!results.TryGetStrValue(EnumDistributed.WorkerId, out strWorkerId) ||
                    string.IsNullOrEmpty(strWorkerId))
                {
                    Console.WriteLine( "Worker id not found");
                }

                //
                // acknowledge the result
                //
                DistControllerJobPull.SendJobDoneAck(
                    strJobId,
                    strWorkerId);
                
                //
                // publish done to all other workers
                //
                if (blnJobFound)
                {
                    TopicMessage topicMessage = TopicPublisher.PrepareTopicMessage(
                        strJobId,
                        GridTopic + EnumDistributed.JobsDoneTopic.ToString());

                    TopicPublisherCache.GetPublisher(ServerName).SendMessage(
                        false,
                        topicMessage);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        #endregion

        public void Dispose()
        {
            if(JobsToDoMap != null)
            {
                JobsToDoMap.Clear();
                JobsToDoMap = null;
            }

            if(DistControllerToWorkerHeartBeat != null)
            {
                DistControllerToWorkerHeartBeat.Dispose();
                DistControllerToWorkerHeartBeat = null;
            }

            if(DistTopicQueue != null)
            {
                DistTopicQueue.Dispose();
                DistTopicQueue = null;
            }

            if (DistControllerJobPull != null)
            {
                DistControllerJobPull.Dispose();
                DistControllerJobPull = null;
            }
            HC.Core.EventHandlerHelper.RemoveAllEventHandlers(this);
        }
    }
}
