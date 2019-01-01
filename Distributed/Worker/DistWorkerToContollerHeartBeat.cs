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
using HC.Core.Threading;

#endregion

namespace HC.Core.Distributed.Worker
{
    public class DistWorkerToContollerHeartBeat : IDisposable
    {
        public delegate void ControllerDiconnectedDel(string strControllerId);

        public ControllerDiconnectedDel OnControllerDiconnected;

        #region Properties

        public ConcurrentDictionary<string, string> ControllerStatus { get; private set; }
        public ConcurrentDictionary<string, DateTime> ControllerPingTimes { get; private set; }

        #endregion

        #region Members

        private readonly string m_strWorkerId;
        private DistWorker m_distWorker;
        private ThreadWorker m_pingWorker;

        #endregion

        #region Constructors

        public DistWorkerToContollerHeartBeat(DistWorker distWorker)
        {
            try
            {
                if (string.IsNullOrEmpty(distWorker.GridTopic))
                {
                    throw new HCException("Emtpy topic");
                }

                m_distWorker = distWorker;
                m_strWorkerId = distWorker.WorkerId;
                ControllerPingTimes = new ConcurrentDictionary<string, DateTime>();
                ControllerStatus = new ConcurrentDictionary<string, string>();
                TopicSubscriberCache.GetSubscriber(
                    distWorker.ServerName,
                    TopicConstants.PUBLISHER_HEART_BEAT_PORT).Subscribe(
                     m_distWorker.GridTopic + EnumDistributed.TopicControllerToWorkerHeartBeat.ToString(),
                    OnTopicControllerToWorkerHeartBeat);

                m_pingWorker = new ThreadWorker(
                    ThreadPriority.Highest);
                m_pingWorker.OnExecute += () =>
                                              {
                                                  while (true)
                                                  {
                                                      try
                                                      {
                                                          OnClockTick();
                                                          Thread.Sleep(
                                                              DistConstants.PING_CONTROLLER_TIME_SECS*1000);
                                                      }
                                                      catch (Exception ex)
                                                      {
                                                          Logger.Log(ex);
                                                          //
                                                          // slow down
                                                          //
                                                          Thread.Sleep( 
                                                              5000);
                                                      }
                                                  }
                                              };
                m_pingWorker.Work();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        #endregion

        #region Private

        private void OnTopicControllerToWorkerHeartBeat(TopicMessage topicmessage)
        {
            try
            {
                var controllerMessage = (ASelfDescribingClass)(topicmessage.EventData);
                string strControllerId = controllerMessage.GetStrValue(EnumDistributed.ControllerId);
                PingBackController(controllerMessage);
                var now = DateTime.Now;
                ControllerPingTimes[strControllerId] = now;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public bool IsControllerDisconnected(
            string strControllerId)
        {
            try
            {
                string strControllerStatus;
                if (ControllerStatus.TryGetValue(
                        strControllerId, 
                        out strControllerStatus) &&
                    !string.IsNullOrEmpty(strControllerStatus) &&
                    strControllerStatus.Contains(EnumDistributed.Disconnected.ToString()))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return false;
        }

        private void OnClockTick()
        {
            try
            {
                CheckAliveController();

                var calcParams = new SelfDescribingClass();
                calcParams.SetClassName(EnumDistributed.HeartBeatWorkerClass);
                calcParams.SetStrValue(
                    EnumDistributed.ControllerId,
                    "unknown");
                calcParams.SetDateValue(
                    EnumDistributed.TimeControllerToWorker,
                    DateTime.Now);
                calcParams.SetDateValue(
                    EnumDistributed.Time,
                    DateTime.Now);
                PingBackController(calcParams);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private void CheckAliveController()
        {
            try
            {
                DateTime now = DateTime.Now;
                foreach (var kvp in ControllerPingTimes)
                {
                    if ((now - kvp.Value).TotalSeconds > DistConstants.ALIVE_CONTROLLER_TIME_SECS)
                    {
                        SetControllerAsDisconnected(kvp);
                    }
                    else
                    {
                        if (!ControllerStatus.ContainsKey(kvp.Key))
                        {
                            DistGuiHelper.PublishWorkerLog(
                                m_distWorker, 
                                "Connected controller [" +
                                    kvp.Key + "]", Guid.NewGuid().ToString());
                        }
                        ControllerStatus[kvp.Key] = EnumDistributed.Connected.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private void SetControllerAsDisconnected(KeyValuePair<string, DateTime> kvp)
        {
            try
            {
                string strControllerId = kvp.Key;
                if(!IsControllerDisconnected(strControllerId))
                {
                    DistGuiHelper.PublishWorkerLog(m_distWorker, "disconnected controller [" +
                        strControllerId + "]", Guid.NewGuid().ToString());
                    ControllerStatus[strControllerId] = EnumDistributed.Disconnected.ToString();
                    if(OnControllerDiconnected != null)
                    {
                        OnControllerDiconnected(strControllerId);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private void PingBackController(ASelfDescribingClass controllerMessage)
        {
            try
            {
                if (controllerMessage== null)
                {
                    return;
                }
                controllerMessage.SetStrValue(
                    EnumDistributed.WorkerId,
                    m_strWorkerId);
                controllerMessage.SetDateValue(
                    EnumDistributed.Time,
                    DateTime.Now);
                
                while (TopicPublisherCache.GetPublisher(
                    m_distWorker.ServerName,
                    TopicConstants.SUBSCRIBER_HEART_BEAT_PORT) == null)
                {
                    Thread.Sleep(50);
                }
                TopicPublisherCache.GetPublisher(
                    m_distWorker.ServerName,
                    TopicConstants.SUBSCRIBER_HEART_BEAT_PORT).SendMessageImmediately(
                    controllerMessage,
                    m_distWorker.GridTopic + EnumDistributed.TopicWorkerToControllerHeartBeat.ToString());

            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        #endregion

        public void Dispose()
        {
            if(ControllerStatus != null)
            {
                ControllerStatus.Clear();
                ControllerStatus = null;
            }

            if(ControllerPingTimes != null)
            {
                ControllerPingTimes.Clear();
                ControllerPingTimes = null;
            }
            if(m_pingWorker != null)
            {
                m_pingWorker.Dispose();
                m_pingWorker = null;
            }
            m_distWorker = null;
            HC.Core.EventHandlerHelper.RemoveAllEventHandlers(this);
        }
    }
}

