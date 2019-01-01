#region

using System;
using System.Collections.Generic;
using System.Threading;
using HC.Core.Logging;
using HC.Core.Threading;
using ZMQ;
using Exception = System.Exception;

#endregion

namespace HC.Core.Comunication.TopicBased
{
    public static class ZmqTopicService
    {
        #region Members

        private static bool m_blnIsConnected;
        private static readonly object m_connectLockObj = new object();
        private static bool m_blnIsReady;
        private static List<ThreadWorker> m_workers1;
        private static List<ThreadWorker> m_workers2;

        #endregion

        public static void Connect()
        {
            try
            {
                if (!m_blnIsConnected)
                {
                    lock (m_connectLockObj)
                    {
                        if (!m_blnIsConnected)
                        {
                            string strServerName = Config.GetTopicServerName();
                            if (!strServerName.Equals("local"))
                            {
                                m_workers1 = new List<ThreadWorker>();
                                m_workers2 = new List<ThreadWorker>();
                                for (int i = 0; i < TopicConstants.NUM_TOPIC_CONNECTIONS; i++)
                                {
                                    m_blnIsConnected = true;
                                    m_blnIsReady = false;
                                    var worker = new ThreadWorker(ThreadPriority.Highest);
                                    worker.OnExecute += () => DoConnect(
                                        TopicConstants.PUBLISHER_DEFAULT_PORT + i,
                                        TopicConstants.SUBSCRIBER_DEFAULT_PORT + i);
                                    worker.Work();
                                    m_workers1.Add(worker);
                                    while (!m_blnIsReady)
                                    {
                                        Thread.Sleep(100);
                                    }

                                    m_blnIsReady = false;
                                    var workers2 = new ThreadWorker(ThreadPriority.Highest);
                                    workers2.OnExecute += () => DoConnect(
                                        TopicConstants.PUBLISHER_HEART_BEAT_PORT + i,
                                        TopicConstants.SUBSCRIBER_HEART_BEAT_PORT + i);
                                    workers2.Work();
                                    m_workers2.Add(workers2);
                                    while (!m_blnIsReady)
                                    {
                                        Thread.Sleep(100);
                                    }
                                }

                                Thread.Sleep(1000);
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void DoConnect(
            int intPublisherPort,
            int intSubscriberPort)
        {
            try
            {
                while (true)
                {
                    try
                    {
                        using (var context = new Context())
                        {
                            using (Socket publisher = context.Socket(SocketType.PUB))
                            {
                                string strPublisherConn = "tcp://*:" + intPublisherPort;
                                publisher.HWM = CoreConstants.HWM; // PUB and ROUTER sockets will drop data if they reach their HWM, while other socket types will bloc
                                publisher.Bind(strPublisherConn);
                                string strMessage = "Publisher socket connected to [" +
                                                    strPublisherConn + "]";
                                Logger.Log(strMessage);
                                Console.WriteLine(strMessage);

                                using (Socket dealerSocket = context.Socket(SocketType.DEALER))
                                {
                                    string strDealerConnection = "tcp://*:" + intSubscriberPort;
                                    dealerSocket.HWM = CoreConstants.HWM;
                                    dealerSocket.Bind(strDealerConnection);
                                    strMessage = "Dealer socket connected to [" +
                                                 strDealerConnection + "]";

                                    Logger.Log(strMessage);
                                    Console.WriteLine(strMessage);

                                    Logger.Log("ZeroMq topic server connected");
                                    m_blnIsReady = true;
                                    while (true)
                                    {
                                        byte[] bytes = dealerSocket.Recv();
                                        SendToPublisher(publisher, bytes, dealerSocket.RcvMore);
                                        while (dealerSocket.RcvMore && bytes != null)
                                        {
                                            bytes = dealerSocket.Recv();
                                            SendToPublisher(publisher, bytes, dealerSocket.RcvMore);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        Logger.Log(ex);
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void SendToPublisher(
            Socket publisher, 
            byte[] bytes,
            bool blnSendMore)
        {
            bool blnSuccess = false;
            var status = SendStatus.Interrupted;
            int intCounter = 0;
            while (!blnSuccess)
            {
                try
                {
                    if (blnSendMore)
                    {
                        status = publisher.SendMore(bytes);
                    }
                    else
                    {
                        status = publisher.Send(bytes);
                    }
                    blnSuccess = status == SendStatus.Sent;
                    if (!blnSuccess)
                    {
                        string strMessage = typeof (ZmqTopicService).Name +
                                            " could not send message [" +
                                            status + "][" +
                                            intCounter + "]. Resending...";
                        Logger.Log(strMessage);
                        Console.WriteLine(strMessage);
                        Thread.Sleep(5000);
                    }
                }
                catch (Exception ex)
                {
                    string strMessage = typeof (ZmqTopicService).Name +
                                        " could not send message [" +
                                        status + "][" +
                                        intCounter + "]. Resending...";
                    Logger.Log(strMessage);
                    Console.WriteLine(strMessage);
                    Logger.Log(ex);
                    Thread.Sleep(5000);
                }
                intCounter++;
            }
        }
    }
}