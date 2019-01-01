#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using HC.Core.Comunication.TopicBased;
using HC.Core.Comunication.TopicBased.Contracts;
using HC.Core.DynamicCompilation;
using HC.Core.Exceptions;
using HC.Core.Logging;

#endregion

namespace HC.Core.Distributed.Worker
{
    public class DistWorkerResultSender : IDisposable
    {
        #region Constants

        const int TOTAL_TRIALS = 15;
        const int DISCONNECTION_TRIALS = 10;

        #endregion

        #region Members

        private ConcurrentDictionary<string, ASelfDescribingClass> m_jobsToSend;
        private DistWorker m_distWorker;

        #endregion

        #region Constructors

        public DistWorkerResultSender(
            DistWorker distWorker)
        {
            m_distWorker = distWorker;
            m_jobsToSend = new ConcurrentDictionary<string, ASelfDescribingClass>();
            TopicSubscriberCache.GetSubscriber(distWorker.ServerName).Subscribe(
                distWorker.GridTopic + EnumDistributed.TopicControllerToWorkerResultConfirm.ToString(),
                OnTopicControllerToWorkerConfirm);
        }

        #endregion

        #region Public

        public bool SendResult(
            List<ITsEvent> events, 
            string strParentId, 
            string strJobId)
        {
            try
            {
                SelfDescribingClass resultParams;
                GetResultParams(
                    strParentId,
                    strJobId,
                    events, 
                    out resultParams);
                return SendResult(resultParams,
                    strJobId);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                return false;
            }
        }

        #endregion

        #region Private

        private void GetResultParams(
            string strParentId,
            string strJobId,
            List<ITsEvent> events,
            out SelfDescribingClass resultParams)
        {
            resultParams = new SelfDescribingClass();
            resultParams.SetClassName(
                EnumDistributed.Result + "_" + typeof(DistWorker).Name);
            resultParams.SetDateValue(
                EnumCalcCols.TimeStamp,
                DateTime.Now);
            resultParams.SetObjValueToDict(
                EnumCalcCols.TsEvents,
                events);
            resultParams.SetStrValue(
                EnumCalcCols.WorkerId,
                m_distWorker.WorkerId);
            //string strParentId = calcParams.GetStrValue(EnumDistributed.ControllerId);
            resultParams.SetStrValue(
                EnumDistributed.ControllerId,
                strParentId);
            //strJobId = calcParams.GetStrValue(EnumDistributed.JobId);
            resultParams.SetStrValue(
                EnumDistributed.JobId,
                strJobId);
        }

        private bool SendResult(
            ASelfDescribingClass resultObj,
            string strJobId)
        {
            ASelfDescribingClass response = null;
            try
            {
                SendResult(resultObj);
                DateTime prevTime = DateTime.Now;
                DateTime sentStart = DateTime.Now;
                int intDisconnectedTrials = 0;
                int intTrials = 0;
                int intResent = 0;
                while (response == null)
                {
                    m_jobsToSend.TryGetValue(strJobId, out response);

                    if ((DateTime.Now - prevTime).TotalSeconds > 10 &&
                        response == null)
                    {
                        string strMessage = "DistWorker is trying to send result to controller. Total time (secs) = [" +
                                            (DateTime.Now - sentStart).TotalSeconds + "]. JobId [" +
                                            strJobId + "] Tials [" + (intTrials++) + "]/" +
                                            "[" + TOTAL_TRIALS + "]";
                        Console.WriteLine(strMessage);
                        //DistGuiHelper.PublishWorkerLog(
                        //    m_distWorker,
                        //    strMessage, 
                        //    strJobId);
                        
                        //
                        // check if controller is disconnected
                        //
                        try
                        {
                            string strControllerId = resultObj.GetStrValue(EnumDistributed.ControllerId);
                            if (m_distWorker.DistWorkerToContollerHeartBeat
                                .IsControllerDisconnected(strControllerId))
                            {
                                strMessage = "DistWorker cannot send result due to controller disconnection. Job [" +
                                    strJobId + "]. Keep trying [" + intDisconnectedTrials + "]...";
                                Console.WriteLine(strMessage);
                                //DistGuiHelper.PublishWorkerLog(
                                //    m_distWorker,
                                //    strMessage, strJobId);
                                intDisconnectedTrials++;
                            }
                            else
                            {
                                if (intDisconnectedTrials > 0)
                                {
                                    //
                                    // controller is reconnected. Send result again
                                    //
                                    SendResult(resultObj);
                                    strMessage = "DistWorker re-sent result due to controller re-connection. Job [" +
                                        strJobId + "]";
                                    Console.WriteLine(strMessage);
                                    //DistGuiHelper.PublishWorkerLog(
                                    //    m_distWorker,
                                    //    strMessage, strJobId);
                                }
                                intDisconnectedTrials = 0;
                            }
                            if (intDisconnectedTrials > DISCONNECTION_TRIALS ||
                                intTrials > TOTAL_TRIALS)
                            {
                                if (intDisconnectedTrials > DISCONNECTION_TRIALS)
                                {
                                    strMessage =
                                        "DistWorker failed to send result due to controller disconnection. Job [" +
                                        strJobId + "]";
                                    Console.WriteLine(strMessage);
                                    return false;
                                }
                                else
                                {
                                    strMessage =
                                        "DistWorker failed to send result due to long wait and no response. Job [" +
                                        strJobId + "]";
                                }
                                Console.WriteLine(strMessage);
                                Console.WriteLine("+++++++++++resending [" + intResent++ + "]");
                                SendResult(resultObj);
                                intTrials = 0;
                                intDisconnectedTrials = 0;
                                //DistGuiHelper.PublishWorkerLog(
                                //    m_distWorker,
                                //    strMessage, strJobId);
                                //return false;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(ex);
                        }

                        prevTime = DateTime.Now;
                    }
                    Thread.Sleep(20);
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                string strMessage = "DistWorker failed to send result due to exception. Job [" +
                    strJobId + "]";
                Console.WriteLine(strMessage);
                //DistGuiHelper.PublishWorkerLog(
                //    m_distWorker,
                //    strMessage, strJobId);
                return false;
            }
            finally
            {
                m_jobsToSend.TryRemove(strJobId, out response);
            }
        }

        private void OnTopicControllerToWorkerConfirm(
            TopicMessage topicmessage)
        {
            try
            {
                var selfDescribingClass = (ASelfDescribingClass) topicmessage.EventData;

                string strWorkerId;
                if (!selfDescribingClass.TryGetStrValue(EnumDistributed.WorkerId, out strWorkerId) ||
                    string.IsNullOrEmpty(strWorkerId))
                {
                    throw new HCException("Worker id not found");
                }

                if (!m_distWorker.WorkerId.Equals(strWorkerId))
                {
                    //
                    // this message was sent to another worker
                    //
                    return;
                }

                string strJobId = selfDescribingClass.GetStrValue(EnumDistributed.JobId);
                Console.WriteLine("Worker got confirmation from controller that result job [" +
                                  strJobId + "] has been recieved");
                m_jobsToSend[strJobId] = selfDescribingClass;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private void SendResult(object resultParams)
        {
            TopicPublisherCache.GetPublisher(m_distWorker.ServerName).SendMessageImmediately(
                resultParams,
                m_distWorker.GridTopic + EnumDistributed.TopicWorkerToControllerResult.ToString());
        }

        #endregion

        public void Dispose()
        {
            if(m_jobsToSend != null)
            {
                m_jobsToSend.Clear();
                m_jobsToSend = null;
            }
            m_distWorker = null;
            HC.Core.EventHandlerHelper.RemoveAllEventHandlers(this);
        }
    }
}