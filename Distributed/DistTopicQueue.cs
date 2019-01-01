#region

using System;
using HC.Core.Comunication.TopicBased;
using HC.Core.Comunication.TopicBased.Contracts;
using HC.Core.Threading.ProducerConsumerQueues;
using HC.Core.Threading.ProducerConsumerQueues.Support;

#endregion

namespace HC.Core.Distributed
{
    public class DistTopicQueue : IDisposable
    {
        private readonly string m_strServerName;

        #region Members

        private EfficientWorkerManager<TopicMessage> m_efficientQueue;

        #endregion

        #region Constructors

        public DistTopicQueue(string strServerName)
        {
            m_strServerName = strServerName;
            m_efficientQueue = new EfficientWorkerManager<TopicMessage>(1);
            m_efficientQueue.OnWork += topicMessage => TopicPublisherCache.GetPublisher(
                strServerName).SendMessage(
                true,
                topicMessage);

        }

        #endregion

        #region Public

        public TaskWrapper AddItem(
            string strTopic,
            string strKey,
            object obj)
        {
            return AddItem(
                strTopic,
                strKey,
                obj,
                true);
        }

        public TaskWrapper AddItem(
            string strTopic,
            string strKey,
            object obj,
            bool blnUseQueue)
        {
            TopicMessage topicMessage = TopicPublisher.PrepareTopicMessage(
                obj,
                strTopic);
            if (blnUseQueue)
            {
                return m_efficientQueue.AddItem(
                    strKey,
                    topicMessage);
            }
            return TopicPublisherCache.GetPublisher(m_strServerName).SendMessage(
                true,
                topicMessage);
        }

        #endregion

        public void Dispose()
        {
            if(m_efficientQueue != null)
            {
                m_efficientQueue.Dispose();
                m_efficientQueue = null;
            }
            HC.Core.EventHandlerHelper.RemoveAllEventHandlers(this);
        }
    }
}

