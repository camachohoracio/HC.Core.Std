#region

using System;
using System.Collections.Concurrent;
using System.Threading;
using HC.Core.Logging;
using HC.Core.Threading;

#endregion

namespace HC.Core.Comunication.TopicBased
{
    public static class TopicPublisherCache
    {
        #region Members

        private static readonly ConcurrentDictionary<string, TopicPublisher> m_topicPublisher;
        private static readonly Random m_rng = new Random();
        private static object m_lockRng = new object();

        #endregion

        #region Constructors

        static TopicPublisherCache()
        {
            m_topicPublisher = new ConcurrentDictionary<string, TopicPublisher>();
        }

        #endregion

        #region Public

        public static bool ContainsPublisher(
            string strServerName)
        {
            return ContainsPublisher(strServerName,
                                 TopicConstants.SUBSCRIBER_DEFAULT_PORT);
        }

        public static bool ContainsPublisher(
            string strServerName,
            int intPort)
        {
            string strConnectionKey = strServerName + intPort;
            return m_topicPublisher.ContainsKey(
                strConnectionKey);
        }

        public static TopicPublisher GetPublisher(string strServerName)
        {
            return GetPublisher(strServerName,
                                 TopicConstants.SUBSCRIBER_DEFAULT_PORT);
        }

        public static TopicPublisher GetPublisher(
            string strServerName,
            int intPort)
        {
            try
            {
                int intRandomPort;
                lock (m_lockRng)
                {
                    intRandomPort = m_rng.Next(intPort, intPort + TopicConstants.NUM_TOPIC_CONNECTIONS);
                }
                return GetPublisher0(strServerName, intRandomPort);
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public static TopicPublisher GetPublisher0(
            string strServerName,
            int intPort)
        {
            try
            {
                string strConnectionKey = strServerName + intPort;
                TopicPublisher topicPublisher;
                if (!m_topicPublisher.TryGetValue(
                    strConnectionKey,
                    out topicPublisher) ||
                    topicPublisher == null)
                {
                    lock (LockObjectHelper.GetLockObject(strConnectionKey + "_" +
                                                         typeof (TopicPublisherCache).Name))
                    {
                        if (!m_topicPublisher.TryGetValue(
                            strConnectionKey,
                            out topicPublisher) ||
                            topicPublisher == null)
                        {
                            topicPublisher = new TopicPublisher(
                                strServerName,
                                intPort);
                            topicPublisher.Connect(
                                strServerName,
                                intPort);

                            while (topicPublisher.m_blnIsConnecting)
                            {
                                Thread.Sleep(100);
                            }
                            m_topicPublisher[strConnectionKey] = topicPublisher;
                        }
                    }
                }
                return topicPublisher;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        #endregion
    }
}