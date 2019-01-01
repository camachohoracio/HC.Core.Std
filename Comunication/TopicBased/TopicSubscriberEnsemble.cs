using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HC.Core.Comunication.TopicBased.Contracts;
using HC.Core.Logging;

namespace HC.Core.Comunication.TopicBased
{
    public class TopicSubscriberEnsemble : ITopicSubscriber
    {
        private readonly List<ITopicSubscriber> m_topicSubscribers;
        private readonly object m_lockObj = new object();
        private readonly Random m_rng = new Random();

        public TopicSubscriberEnsemble(List<ITopicSubscriber> topicSubscribers)
        {
            m_topicSubscribers = topicSubscribers;
        }

        public void Dispose()
        {
            try
            {
                foreach (ITopicSubscriber topicSubscriber in m_topicSubscribers)
                {
                    topicSubscriber.Dispose();
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public void Connect(string strServerName, int intPort)
        {
            try
            {
                for (int i = 0; i < intPort + TopicConstants.NUM_TOPIC_CONNECTIONS; i++)
                {
                    m_topicSubscribers[i].Connect(strServerName, intPort + i);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public void Subscribe(string strTopic, SubscriberCallback subscriberCallback)
        {
            try
            {
                Parallel.For(
                    0, m_topicSubscribers.Count, delegate(int i)
                                          {
                                              ITopicSubscriber topicSubscriber = m_topicSubscribers[i];
                                              topicSubscriber.Subscribe(strTopic, subscriberCallback);
                                          });
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public bool IsSubscribedToTopic(string strTopic)
        {
            try
            {
                return m_topicSubscribers[0].IsSubscribedToTopic(strTopic);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return false;
        }

        public void UnSubscribe(string strTopic)
        {
            try
            {
                foreach (ITopicSubscriber topicSubscriber in m_topicSubscribers)
                {
                    topicSubscriber.UnSubscribe(strTopic);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public void Publish(TopicMessage topicMessage)
        {
            try
            {
                int intRng;
                lock (m_lockObj)
                {
                    intRng = m_rng.Next(0, m_topicSubscribers.Count);
                }
                m_topicSubscribers[intRng].Publish(topicMessage);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public int SubscriberCount(string strTopic)
        {
            try
            {
                return m_topicSubscribers[0].SubscriberCount(strTopic);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return 0;
        }

        public void NotifyDesconnect(string strTopic, NotifierDel notifierDel)
        {
            try
            {
                foreach (ITopicSubscriber topicSubscriber in m_topicSubscribers)
                {
                    topicSubscriber.NotifyDesconnect(strTopic, notifierDel);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }
    }
}
