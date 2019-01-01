using HC.Core.Comunication.TopicBased.Contracts;

namespace HC.Core.Comunication.TopicBased
{
    #region Delegates

    public delegate void SubscriberCallback(TopicMessage topicMessage);
    public delegate void NotifierDel(string strTopic);

    #endregion
    
    public interface ITopicSubscriber
    {
        void Dispose();
        void Connect(
            string strServerName,
            int intPort);

        void Subscribe(
            string strTopic,
            SubscriberCallback subscriberCallback);

        bool IsSubscribedToTopic(string strTopic);

        void UnSubscribe(string strTopic);
        void Publish(TopicMessage topicMessage);

        int SubscriberCount(string strTopic);

        void NotifyDesconnect(
            string strTopic,
            NotifierDel notifierDel);

    }
}


