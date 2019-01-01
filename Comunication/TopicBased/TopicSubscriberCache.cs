#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using HC.Core.Logging;
using HC.Core.Threading;

#endregion

namespace HC.Core.Comunication.TopicBased
{
    public static class TopicSubscriberCache
    {
        #region Members

        private static readonly ConcurrentDictionary<string, ITopicSubscriber> m_topicSubscribers;

        #endregion

        #region Constructors

        static TopicSubscriberCache()
        {
            m_topicSubscribers = new ConcurrentDictionary<string, ITopicSubscriber>();
        }

        #endregion

        #region Public

        public static ITopicSubscriber GetSubscriber(string strServerName)
        {
            return GetSubscriber(strServerName,
                                 TopicConstants.PUBLISHER_DEFAULT_PORT);
        }

        public static bool ContainsSubscriber(
            string strServerName)
        {
            return ContainsSubscriber(strServerName,
                                      TopicConstants.PUBLISHER_DEFAULT_PORT);
        }

        public static bool ContainsSubscriber(
            string strServerName,
            int intPort)
        {
            try
            {
                string strConnectionKey = strServerName + intPort;
                return m_topicSubscribers.ContainsKey(
                    strConnectionKey);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return false;
        }

        public static ITopicSubscriber GetSubscriber(
            string strServerName,
            int intPort)
        {
            try
            {
                string strConnectionKey = strServerName + intPort;
                ITopicSubscriber topicSubscriber;
                if (!m_topicSubscribers.TryGetValue(
                    strConnectionKey,
                    out topicSubscriber) ||
                    topicSubscriber == null)
                {
                    lock (LockObjectHelper.GetLockObject(strConnectionKey + "_" +
                                                         typeof (TopicPublisherCache).Name))
                    {
                        if (!m_topicSubscribers.TryGetValue(
                            strConnectionKey,
                            out topicSubscriber) ||
                            topicSubscriber == null)
                        {
                            topicSubscriber = CreateSubscriberEnsemble(strServerName, intPort);
                            m_topicSubscribers[strConnectionKey] = topicSubscriber;
                        }
                    }
                }
                return topicSubscriber;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        private static ITopicSubscriber CreateSubscriberEnsemble(
            string strServerName, 
            int intPort)
        {
            try
            {
                var topicList = new List<ITopicSubscriber>();
                for (int i = 0; i < TopicConstants.NUM_TOPIC_CONNECTIONS; i++)
                {
                    topicList.Add(CreateSubscriber(strServerName, intPort + i));
                }
                var topicSubscriber = new TopicSubscriberEnsemble(topicList);
                return topicSubscriber;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        private static ITopicSubscriber CreateSubscriber(string strServerName, int intPort)
        {
            try
            {
                ITopicSubscriber topicSubscriber;
                string strTopicInterface = Config.GetTopicInterface().ToLower();
                //if (strTopicInterface.Equals("wcf"))
                //{
                //    topicSubscriber = new WcfTopicSubscriber();
                //}
                //else if (strTopicInterface.Equals("0mq"))
                {
                    topicSubscriber = new ZmqTopicSubscriber();
                }
                //else
                //{
                //    throw new NotImplementedException();
                //}
                topicSubscriber.Connect(
                    strServerName,
                    intPort);
                return topicSubscriber;
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