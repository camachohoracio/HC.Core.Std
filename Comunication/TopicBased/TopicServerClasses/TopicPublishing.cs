#region

using System;
using System.Reflection;
using HC.Core.Comunication.TopicBased.Contracts;
using HC.Core.Logging;
//using System.ServiceModel;

#endregion

//[ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
namespace HC.Core.Comunication.TopicBased.TopicServerClasses
{
    public class TopicPublishing : ITopicPublishing
    {
        #region Members

        private static MethodInfo m_publishMethodInfo;

        #endregion

        #region Constructors

        static TopicPublishing()
        {
            var type = typeof(ITopicPublishing);
            m_publishMethodInfo = type.GetMethod("Publish");
        }

        #endregion

        private delegate void UnSubscribeDel(
            TopicMessage e,
            ITopicPublishing topicPublishing);

        #region ITopicPublishing Members

        public void Publish(TopicMessage topicMessage)
        {
            Exception exception = null;
            try
            {
                var subscribers = TopicFilter.GetSubscribers(
                    topicMessage.TopicName);
                if (subscribers == null)
                {
                    return;
                }


                foreach (ITopicPublishing subscriber in subscribers)
                {
                    try
                    {
                        m_publishMethodInfo.Invoke(subscriber, new object[] {topicMessage});
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                        Logger.Log(ex, false);
                        new UnSubscribeDel(UnSubscribe).BeginInvoke(
                            topicMessage, subscriber, null, null);
                        //UnSubscribe(topicMessage, subscriber);
                    }
                }
            }
            catch (Exception ex)
            {
                exception = ex;
                const string strMessage = "Exception at the publish lelvel";
                Logger.Log(strMessage);
                Console.WriteLine(strMessage);
                Logger.Log(ex, false);
            }
            finally
            {
                if(exception != null)
                {
                    //
                    // note if we dont throw then the publisher will not recieve any exceptions
                    //
                    throw exception;
                }
            }
        }

        public void Reconnect()
        {
        }

        private static void UnSubscribe(
            TopicMessage e, 
            ITopicPublishing topicPublishing)
        {
            try
            {
                string strTopicName = e.TopicName;
                string strMessage = "Exception catched. Removing topic [" +
                                    strTopicName + "]";
                Logger.Log(strMessage);
                Console.WriteLine(strMessage);
                TopicFilter.RemoveTopic(
                    strTopicName,
                    topicPublishing);
            }
            catch (Exception ex)
            {
                Logger.Log(ex, false);
                throw;
            }
        }

        #endregion
    }
}


