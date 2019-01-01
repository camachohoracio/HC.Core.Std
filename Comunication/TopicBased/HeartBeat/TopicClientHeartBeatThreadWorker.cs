#region

using System;
using System.Threading;
using HC.Core.Comunication.RequestResponseBased;
using HC.Core.Comunication.TopicBased.Contracts;
using HC.Core.Threading;

#endregion

namespace HC.Core.Comunication.TopicBased.HeartBeat
{
    public class TopicClientHeartBeatThreadWorker
    {
        #region Constants

        public const int CONNECTION_TIME_OUT_SECS = 45;
        private const int HEART_BEAT_TIME_SECS = 1;

        #endregion

        public DateTime LastTopicPingTime { get; private set; }
        
        
        public EnumReqResp ConnectionState { get; private set; }

        #region Members

        private readonly string m_strServerName;
        private readonly ConnectionStateDel m_connectionStateDel;
        private DateTime m_lastTopicCheckTime;
        private readonly ThreadWorker m_threadWorker;
        private DateTime m_diconnectedTimer;

        #endregion

        #region Constructor

        public TopicClientHeartBeatThreadWorker(
            string strServerName,
            ConnectionStateDel connectionStateDel)
        {
            m_strServerName = strServerName;
            m_connectionStateDel = connectionStateDel;
            m_lastTopicCheckTime = DateTime.Now;
            LastTopicPingTime = DateTime.Now;
            m_threadWorker = new ThreadWorker(ThreadPriority.Highest);
            m_threadWorker.OnExecute += HeartBeat;
            m_threadWorker.Work();
            TopicSubscriberCache.GetSubscriber(
                strServerName,
                TopicConstants.PUBLISHER_HEART_BEAT_PORT).Subscribe(
                typeof(TopicServerHeartBeat).Name,
                OnTopicPingEvent);

            TopicSubscriberCache.GetSubscriber(
                strServerName,
                TopicConstants.PUBLISHER_DEFAULT_PORT).Subscribe(
                typeof(TopicServerHeartBeat).Name,
                OnTopicPingEvent);
        }

        #endregion

        private void HeartBeat()
        {
            while (true)
            {
                bool blnIsTimeOut = (Clock.LastTime - m_lastTopicCheckTime).
                                        TotalSeconds >
                                    CONNECTION_TIME_OUT_SECS;
                if (blnIsTimeOut &&
                    (Clock.LastTime - m_diconnectedTimer).TotalSeconds >
                        CONNECTION_TIME_OUT_SECS)
                {
                    ConnectionState = EnumReqResp.Disconnected;
                    m_connectionStateDel(m_strServerName);
                    m_diconnectedTimer = DateTime.Now;
                }
                else if (!blnIsTimeOut)
                {
                    ConnectionState = EnumReqResp.Connected;
                }
                Thread.Sleep(HEART_BEAT_TIME_SECS * 1000);
            }
        }

        private void OnTopicPingEvent(TopicMessage topicMessage)
        {
            m_lastTopicCheckTime = DateTime.Now;
            LastTopicPingTime = DateTime.Now;
        }
    }
}