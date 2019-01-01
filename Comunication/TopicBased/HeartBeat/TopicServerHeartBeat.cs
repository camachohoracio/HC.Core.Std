#region

using System;
using HC.Core.Threading;
using System.Threading;
using HC.Core.Logging;
using HC.Core.DynamicCompilation;

#endregion

namespace HC.Core.Comunication.TopicBased.HeartBeat
{
    public static class TopicServerHeartBeat
    {
        #region Constants

        private const int HEART_BEAT_TIME = 1000;

        #endregion

        #region Members

        private static readonly object m_lockObject = new object();
        private static ThreadWorker m_threadWorker;
        private static string m_strServerName;

        #endregion

        public static void StartHeartBeat(string strServerName)
        {
            if (m_threadWorker == null)
            {
                lock (m_lockObject)
                {
                    if (m_threadWorker == null)
                    {
                        m_strServerName = strServerName;
                        m_threadWorker = new ThreadWorker(ThreadPriority.Highest);
                        m_threadWorker.OnExecute += Work;
                        m_threadWorker.Work();
                        Logger.Log("Started " + typeof(TopicServerHeartBeat).Name);
                    }
                }
            }
        }

        private static void Work()
        {
            while(!TopicServer.IsInitialized)
            {
                Thread.Sleep(100);
            }
            while (true)
            {
                string strTopic = typeof(TopicServerHeartBeat).Name;
                var selfDescribingClass = new SelfDescribingClass();
                selfDescribingClass.SetDateValue("Time", DateTime.Now);
                selfDescribingClass.SetClassName(strTopic);
                TopicPublisherCache.GetPublisher(
                    m_strServerName,
                    TopicConstants.SUBSCRIBER_HEART_BEAT_PORT).SendMessage(
                    selfDescribingClass,
                    strTopic, // topic name
                    true); // wait
                TopicPublisherCache.GetPublisher(
                    m_strServerName,
                    TopicConstants.SUBSCRIBER_DEFAULT_PORT).SendMessage(
                    selfDescribingClass,
                    strTopic, // topic name
                    true); // wait
                Thread.Sleep(HEART_BEAT_TIME);
            }
        }
    }
}



