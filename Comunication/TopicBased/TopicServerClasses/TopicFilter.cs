#region

using System;
using System.Collections.Generic;
using HC.Core.Comunication.TopicBased.Contracts;
using HC.Core.Logging;

#endregion

namespace HC.Core.Comunication.TopicBased.TopicServerClasses
{
    public class TopicFilter
    {
        #region Members

        private static readonly Dictionary<string, List<ITopicPublishing>> m_subscribersList =
            new Dictionary<string, List<ITopicPublishing>>();

        #endregion

        public static Dictionary<string, List<ITopicPublishing>> SubscribersList
        {
            get
            {
                lock (typeof(TopicFilter))
                {
                    return m_subscribersList;
                }
            }
        }

        public static List<ITopicPublishing> GetSubscribers(string topicName)
        {
            lock (typeof(TopicFilter))
            {
                if (SubscribersList.ContainsKey(topicName))
                {
                    return SubscribersList[topicName];
                }
                return null;
            }
        }

        public static void AddTopic(String topicName, ITopicPublishing subscriberCallbackReference)
        {
            try
            {
                lock (typeof (TopicFilter))
                {
                    if (SubscribersList.ContainsKey(topicName))
                    {
                        if (!SubscribersList[topicName].Contains(subscriberCallbackReference))
                        {
                            SubscribersList[topicName].Add(subscriberCallbackReference);
                        }
                    }
                    else
                    {
                        var newSubscribersList = new List<ITopicPublishing>();
                        newSubscribersList.Add(subscriberCallbackReference);
                        SubscribersList.Add(topicName, newSubscribersList);
                        string strMessage = "Added topic [" + topicName + "]";
                        Logger.Log(strMessage);
                        Console.WriteLine(strMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public static void RemoveTopic(String topicName, ITopicPublishing subscriberCallbackReference)
        {
            try
            {

                lock (typeof(TopicFilter))
                {
                    if (SubscribersList.ContainsKey(topicName))
                    {
                        if (SubscribersList[topicName].Contains(subscriberCallbackReference))
                        {
                            SubscribersList[topicName].Remove(subscriberCallbackReference);
                            string strMessage = "Removed topic: [" + topicName + "]";
                            Logger.Log(strMessage);
                            Console.WriteLine(strMessage);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }
    }
}


