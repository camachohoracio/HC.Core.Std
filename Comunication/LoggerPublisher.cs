#region

using System;
using System.Threading;
using HC.Core.Threading;
using HC.Core.Comunication.TopicBased;
using HC.Core.Logging;

#endregion

namespace HC.Core.Comunication
{
    public static class LoggerPublisher
    {
        public static bool IsPublisherConnected { get; set; }
        public static bool IsSubscriberConnected { get; set; }
        private static readonly object m_connectLock = new object();
        private static string m_strServerNamePublisher;

        public static void ConnectPublisher(string strServerName)
        {
            if (IsPublisherConnected)
            {
                return;
            }
            lock (m_connectLock)
            {
                if (IsPublisherConnected)
                {
                    return;
                }
                m_strServerNamePublisher = strServerName;
                IsPublisherConnected = true;
            }
        }

        public static void ConnectSubscriber(string strServerName)
        {
            ThreadWorker.StartTaskAsync(() => ConnectSubscriber0(strServerName));
        }

        private static void ConnectSubscriber0(string strServerName)
        {
            if (IsSubscriberConnected)
            {
                return;
            }
            lock (m_connectLock)
            {
                if (IsSubscriberConnected)
                {
                    return;
                }

                while(!TopicSubscriberCache.ContainsSubscriber(strServerName))
                {
                    string strMessage = "Subscriber not found [" + strServerName + "]";
                    Console.WriteLine(strMessage);
                    Logger.Log(strMessage, false, false, false);
                    Thread.Sleep(1000);
                }

                //TopicSubscriberCache.GetSubscriber(strServerName).Subscribe(
                //    "LogGlobal",
                //    topicMessage =>
                //    {
                //        try
                //        {
                //            Logger.Log(topicMessage.EventData.ToString(), false, false, false);
                //        }
                //        catch (Exception ex)
                //        {
                //            Logger.Log(ex);
                //        }
                //    });
                IsSubscriberConnected = true;
            }
        }

        public static void PublishLog(string strLog)
        {
            try
            {
                if (!IsPublisherConnected ||
                    !TopicPublisherCache.ContainsPublisher(m_strServerNamePublisher))
                {
                    return;
                }
                strLog = Environment.NewLine + "--RemoteLog-- [" + ConfigClasses.HCConfig.ClientUniqueName + "]" +
                    Environment.NewLine + strLog;
                TopicPublisherCache.GetPublisher(m_strServerNamePublisher).SendMessage(
                    strLog,
                    "LogGlobal", false);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }
    }
}
