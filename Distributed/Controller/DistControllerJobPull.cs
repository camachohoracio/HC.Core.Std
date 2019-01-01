#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HC.Core.Comunication.TopicBased;
using HC.Core.Comunication.TopicBased.Contracts;
using HC.Core.DynamicCompilation;
using HC.Core.Exceptions;
using HC.Core.Logging;
using HC.Core.Threading;
using HC.Core.Threading.ProducerConsumerQueues;

#endregion

namespace HC.Core.Distributed.Controller
{
    public class DistControllerJobPull : IDisposable
    {
        private const int PULL_WAIT_MILLS = 20;

        #region Properties

        public ConcurrentDictionary<string, ASelfDescribingClass> MapJobIdToWorkerId { get; private set; }
        public object JobsInProgressLock { get; private set; }
        public ConcurrentDictionary<string, int> MapWorkerToJobsDone { get; private set; }

        #endregion

        #region Members

        private DistController m_distController;
        private EfficientWorkerManager<ASelfDescribingClass> m_jobQueue;
        private ConcurrentDictionary<string, object> m_jobsToAck;
        private readonly ConcurrentDictionary<string, DateTime> m_pullIds;
        private readonly ConcurrentDictionary<string, object> m_jobsToPull; 
        private static readonly object m_toPullLock = new object();
        private ThreadWorker m_clockTickWorker;
        private readonly ThreadWorker m_pullIdsFlusherWorker;

        #endregion

        #region Constructors

        public DistControllerJobPull(DistController distController)
        {
            try
            {
                m_jobsToPull = new ConcurrentDictionary<string, object>();
                m_pullIds = new ConcurrentDictionary<string, DateTime>();
                m_jobsToAck = new ConcurrentDictionary<string, object>();
                m_distController = distController;

                MapWorkerToJobsDone = new ConcurrentDictionary<string, int>();
                JobsInProgressLock = new object();
                m_distController = distController;
                MapJobIdToWorkerId = new ConcurrentDictionary<string, ASelfDescribingClass>();

                SetupClockTick();

                if (string.IsNullOrEmpty(m_distController.GridTopic))
                {
                    throw new HCException("Empty grid topic");
                }


                m_jobQueue = new EfficientWorkerManager<ASelfDescribingClass>(5);
                    // define how many jobs will be pulled at the same time
                m_jobQueue.OnWork += OnQueueConsume;
                TopicSubscriberCache.GetSubscriber(distController.ServerName).Subscribe(
                    distController.GridTopic + EnumDistributed.TopicWorkerToControllerPullJob.ToString(),
                    OnTopicWorkerToControllerPullJob);
                TopicSubscriberCache.GetSubscriber(distController.ServerName).Subscribe(
                    distController.GridTopic + EnumDistributed.TopicWorkerToControllerPullJobAck.ToString(),
                    OnTopicWorkerToControllerPullJobAck);


                m_pullIdsFlusherWorker = new ThreadWorker();
                m_pullIdsFlusherWorker.OnExecute += () =>
                {
            		try{
	            		while(true){
	            			
	            			try{
	            				
	            				//
	            				// flush pull ids
	            				//
		            			if(m_pullIds.Count > 10e3){
		            				
		            				var pullIdsToDelete = new List<String>(); 
			            			foreach(var kvp in m_pullIds){
			            				double dblTotalMins = (DateTime.Now - kvp.Value).TotalMinutes;
			            				if(dblTotalMins > 30){
			            					String strKeyId = kvp.Key;
			            					pullIdsToDelete.Add(strKeyId);
			            					String strMessage = "Deleting old pull id [" +
			            							strKeyId + "]";
			            					Logger.Log(strMessage);
			            					Console.WriteLine(strMessage);
			            				}
			            			}
			            			if(pullIdsToDelete.Count > 0){
			            				foreach(string strPullId in pullIdsToDelete)
			            				{
			            				    DateTime dummy;
			            				    m_pullIds.TryRemove(strPullId, out dummy);
			            				}
			            			}
		            			}
		            			//
		            			// flush jobs done
		            			//
		            			if(m_distController.JobsDoneMap.Count > 10e3){
		            				var jobsDoneToDelete = new List<String>(); 
		            				
		            				foreach(var kvp in m_distController.JobsDoneMap){
		            					JobDoneWrapper jobDoneWrapper = kvp.Value;
			            				
		            					double dblTotalMins = (
		            							DateTime.Now - jobDoneWrapper.DateCreated).TotalMinutes;
			            				if(dblTotalMins > 30){
			            					String strKeyId = kvp.Key;
			            					jobsDoneToDelete.Add(strKeyId);
			            					String strMessage = "Deleting old job done id [" +
			            							strKeyId + "]";
			            					Logger.Log(strMessage);
			            					Console.WriteLine(strMessage);
			            				}
		            					
		            				}
		            				
			            			if(jobsDoneToDelete.Count > 0){
			            				foreach(string strJobId in jobsDoneToDelete)
			            				{
			            				    JobDoneWrapper dummy;
			            				    m_distController.JobsDoneMap.TryRemove(strJobId, out dummy);
			            				}
			            			}
		            			}
	            			}
	            			catch(Exception ex){
	            				Logger.Log(ex);
	            			}
	            			Thread.Sleep(60*1000);
	            		}
            		}
            		catch(Exception ex){
            			Logger.Log(ex);
            		}
            	};

            m_pullIdsFlusherWorker.Work();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        #endregion

        private void SetupClockTick()
        {
            try
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
                                                           Thread.Sleep(DistConstants.JOB_ADVERTISE_TIME_SECS*1000);
                                                       }
                                                   };
                m_clockTickWorker.Work();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public void SendJobDoneAck(
            string strJobId,
            string strWorkerId)
        {
            try
            {
                ASelfDescribingClass doWorkClass = new SelfDescribingClass();
                doWorkClass.SetClassName(EnumDistributed.JobDoneAck);
                doWorkClass.SetStrValue(EnumDistributed.JobId,
                                        strJobId);
                doWorkClass.SetStrValue(EnumDistributed.WorkerId,
                                        strWorkerId);

                TopicPublisherCache.GetPublisher(m_distController.ServerName).SendMessageImmediately(
                    doWorkClass,
                    m_distController.GridTopic + EnumDistributed.TopicControllerToWorkerResultConfirm.ToString());
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private void OnClockTick()
        {
            try
            {
                //AdvertiseJobs();
                DistGuiHelper.PublishControllerGui(m_distController);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private void OnTopicWorkerToControllerPullJobAck(
            TopicMessage topicmessage)
        {
            try
            {
                var strPullId = (string) topicmessage.EventData;
                if (m_jobsToAck.ContainsKey(strPullId))
                {
                    string strMessage = "Successfuly acked pullid [" + strPullId + "]";
                    Verboser.WriteLine(strMessage);
                    Logger.Log(strMessage);
                    object dummy;
                    m_jobsToAck.TryRemove(strPullId, out dummy);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        /// <summary>
        /// Method should be thread safe
        /// </summary>
        /// <param name="pullParams"></param>
        private void OnQueueConsume(ASelfDescribingClass pullParams)
        {
            //Interlocked.Increment(ref m_intQueueConsumeSize);

            try
            {
                //if (m_intQueueConsumeSize  > m_distController.)
                //{
                //    throw new HCException("This method is not thread-safe");
                //}

                string strPullId;
                if (!pullParams.TryGetStrValue(EnumDistributed.PullId, out strPullId) ||
                    string.IsNullOrEmpty(strPullId))
                {
                    throw new HCException("Pull id [" + strPullId + "] not found");
                }

                lock (LockObjectHelper.GetLockObject(strPullId + "_worker"))
                { 
                    string strMessage;
                    if (m_pullIds.ContainsKey(strPullId))
                    {
                        //strMessage = "PullId [" + strPullId + "] already pulled";
                        //Console.WriteLine(strMessage);
                        //Logger.Log(strMessage);
                        return;
                    }

                    strMessage = "Pulling job from pull id [" + strPullId + "]...";
                    Verboser.WriteLine(strMessage);
                    Logger.Log(strMessage);

                    //
                    // loop until a job to do is found
                    //
                    ASelfDescribingClass jobParams = PullJob(strPullId);

                    string strWorkerId;
                    if (!pullParams.TryGetStrValue(EnumDistributed.WorkerId, out strWorkerId) ||
                        string.IsNullOrEmpty(strWorkerId))
                    {
                        throw new HCException("Worker id not found");
                    }

                    string strJobId;
                    if (!jobParams.TryGetStrValue(EnumDistributed.JobId, out strJobId) ||
                        string.IsNullOrEmpty(strJobId))
                    {
                        throw new HCException("Job id not found");
                    }

                    //
                    // wait for worker to confirm ack
                    //
                    if (!WaitForWorkerToConfirm(
                        jobParams,
                        strWorkerId,
                        strPullId))
                    {
                        object dummyObj;
                        m_jobsToPull.TryRemove(strJobId, out dummyObj);
                        strMessage = "***Worker is disconnected";
                        Console.WriteLine(strMessage);
                        Logger.Log(strMessage);
                        return;
                    }

                    SelfDescribingClass jobLog = DistControllerJobLogger.GetJobLog(
                        strWorkerId,
                        strJobId,
                        strPullId);

                    if (MapJobIdToWorkerId.ContainsKey(strJobId))
                    {
                        throw new HCException("Job id already assigned to a worker");
                    }

                    MapJobIdToWorkerId[strJobId] = jobLog;
                    object dummy;
                    m_jobsToPull.TryRemove(strJobId, out dummy);

                    DistGuiHelper.PublishJobLog(
                        m_distController,
                        strWorkerId,
                        strJobId,
                        jobLog);

                    m_pullIds[strPullId] = DateTime.Now;

                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private ASelfDescribingClass PullJob(
            string strPullId)
        {
            try
            {
                ASelfDescribingClass jobParams = null;
                bool blnFoundJob = false;
                int intLogTimeCounter = 0;
                string strMessage;
                while (!blnFoundJob)
                {
                    if (m_distController.JobsToDoMap.Count > 0)
                    {
                        List<KeyValuePair<string, ASelfDescribingClass>> jobsUnassigned =
                            (from n in
                                 m_distController.JobsToDoMap
                             where !MapJobIdToWorkerId.ContainsKey(n.Key) &&
                                   !m_jobsToPull.ContainsKey(n.Key)
                             select n).ToList();

                        if (jobsUnassigned.Count > 0)
                        {
                            lock (m_toPullLock)
                            {
                                jobsUnassigned =
                                    (from n in
                                         m_distController.JobsToDoMap
                                     where !MapJobIdToWorkerId.ContainsKey(n.Key) &&
                                           !m_jobsToPull.ContainsKey(n.Key)
                                     select n).ToList();

                                if (jobsUnassigned.Count > 0)
                                {
                                    var firstKvp = jobsUnassigned.First();
                                    jobParams = firstKvp.Value;
                                    m_jobsToPull[firstKvp.Key] = null;
                                    blnFoundJob = true;
                                }
                            }
                        }
                    }
                    
                    //if (intLogTimeCounter > 30000)
                    //{
                    //    intLogTimeCounter = 0;
                    //    strMessage = "Finding job. Jobs to do [" + m_distController.JobsToDoMap.Count + "]";
                    //    Console.WriteLine(strMessage);
                    //    //Logger.Log(strMessage);
                    //}
                    Thread.Sleep(100);
                    intLogTimeCounter += 100;
                }

                if (jobParams == null)
                {
                    throw new HCException("job params not found");
                }
                strMessage = "Successfuly pulled job. Pull id [" + strPullId + "]";
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

        private bool WaitForWorkerToConfirm(
            ASelfDescribingClass jobParams,
            string strWorkerId,
            string strPullId)
        {
            try
            {
                //
                // check if worker is still alive
                //
                if (!m_distController.DistControllerToWorkerHeartBeat.IsWorkerConnected(
                    strWorkerId))
                {
                    return false;
                }
                string strMessage = "Sending job ready to worker. Pull id [" +
                                    strPullId + "]...";
                Verboser.WriteLine(strMessage);
                Logger.Log(strMessage);

                jobParams.SetStrValue(EnumDistributed.PullId,
                                      strPullId);
                jobParams.SetStrValue(EnumDistributed.WorkerId,
                                      strWorkerId);
                jobParams.SetStrValue(EnumDistributed.ControllerId,
                                      m_distController.ControllerId);
                jobParams.SetBlnValue(EnumDistributed.DoWorkAnswer, true);
                TopicPublisherCache.GetPublisher(m_distController.ServerName).SendMessage(
                    jobParams,
                    m_distController.GridTopic +
                    EnumDistributed.TopicControllerToWorkerPullJob.ToString() +
                    strWorkerId,
                    true);

                m_jobsToAck[strPullId] = null;
                int intWaitCounter = 0;
                while (m_jobsToAck.ContainsKey(strPullId))
                {
                    Thread.Sleep(PULL_WAIT_MILLS);
                    if (intWaitCounter > 5000)
                    {
                        //
                        // check if worker is still alive
                        //
                        if (!m_distController.DistControllerToWorkerHeartBeat.IsWorkerConnected(
                            strWorkerId))
                        {
                            return false;
                        }

                        intWaitCounter = 0;
                        //
                        // resend answer to worker
                        //
                        TopicPublisherCache.GetPublisher(m_distController.ServerName).SendMessage(
                            jobParams,
                            m_distController.GridTopic +
                            EnumDistributed.TopicControllerToWorkerPullJob.ToString() +
                            strWorkerId,
                            true);
                    }
                    intWaitCounter += PULL_WAIT_MILLS;
                }
                
                strMessage = "Successfuly sent job ready to worker. Pull id [" +
                                    strPullId + "]";
                Verboser.WriteLine(strMessage);
                Logger.Log(strMessage);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return false;
        }

        private void OnTopicWorkerToControllerPullJob(TopicMessage topicmessage)
        {
            try
            {
                var pullParams = (ASelfDescribingClass) topicmessage.EventData;
                string strPullId;
                if (!pullParams.TryGetStrValue(EnumDistributed.PullId, out strPullId) ||
                    string.IsNullOrEmpty(strPullId))
                {
                    throw new HCException("Pull id not found");
                }

                if (m_jobQueue.ContainsKey(strPullId))
                {
                    string strWorkerId;
                    if (!pullParams.TryGetStrValue(EnumDistributed.WorkerId, out strWorkerId) ||
                        string.IsNullOrEmpty(strWorkerId))
                    {
                        throw new HCException("Worker id not found");
                    }
                    //
                    // send "no" to worker. This will avoid being pinged too often
                    //
                    pullParams.SetBlnValue(EnumDistributed.DoWorkAnswer, false);
                    pullParams.SetStrValue(EnumDistributed.ControllerId,
                                           m_distController.ControllerId);

                    TopicPublisherCache.GetPublisher(m_distController.ServerName).SendMessage(
                        pullParams,
                        m_distController.GridTopic +
                        EnumDistributed.TopicControllerToWorkerPullJob.ToString() +
                        strWorkerId,
                        true);

                    string strMessage = "Sent [NO] to worker [" + strWorkerId + "]";
                    Verboser.WriteLine(strMessage);
                    Logger.Log(strMessage);
                }
                else
                {
                    m_jobQueue.AddItem(strPullId, pullParams);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public void Dispose()
        {
            try
            {
                m_distController = null;
                if (m_jobQueue != null)
                {
                    m_jobQueue.Dispose();
                    m_jobQueue = null;
                }
                if (m_jobsToAck != null)
                {
                    m_jobsToAck.Clear();
                    m_jobsToAck = null;
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