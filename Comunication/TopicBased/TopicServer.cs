#region

using System;
using System.Net;
//using System.ServiceModel;
using System.Threading;
using HC.Core.Comunication.TopicBased.HeartBeat;
using HC.Core.Helpers;
using HC.Core.Io.KnownObjects;
using HC.Core.Logging;
using HC.Core.Threading;

#endregion

namespace HC.Core.Comunication.TopicBased
{
    public class TopicServer
    {
        #region Properties

        private static TopicServer m_ownInstance;

        #endregion

        #region Members

        //private ServiceHost m_publishServiceHost;
        //private ServiceHost m_subscribeServiceHost;
        private static readonly object m_lockObject = new object();
        private static readonly object m_lockObject2 = new object();
        public static bool IsInitialized { get; private set; }

        #endregion

        #region Constructors

        private TopicServer()
        {
            try
            {
                string strTopicInterface = Config.GetTopicInterface().ToLower();
                //if (strTopicInterface.Equals("wcf"))
                //{
                //    HostPublishService();
                //    HostSubscriptionService();
                //}
                //else if (strTopicInterface.Equals("0mq"))
                {
                    ZmqTopicService.Connect();
                }
                //else
                //{
                //    throw new HCException("Topic server type not found");
                //}
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        #endregion

        #region Public

        public static void Connect(string strSererName)
        {
            if (m_ownInstance == null)
            {
                lock (m_lockObject)
                {
                    if (m_ownInstance == null)
                    {
                        m_ownInstance = new TopicServer();
                        Logger.Log("Topic server initialized for DNS = " + Dns.GetHostName());
                        TopicServerHeartBeat.StartHeartBeat(strSererName);
                    }
                }
            }
        }
        
        public static void StartTopicService()
        {
            if (!IsInitialized)
            {
                lock (m_lockObject2)
                {
                    if (!IsInitialized)
                    {
                        IsInitialized = true;
                        while (true)
                        {
                            LoadTopicService();
                            //
                            // topic service died, try again
                            //
                            m_ownInstance = null;
                            Thread.Sleep(10000);
                        }
                    }
                }
            }
        }

        #endregion

        #region Private

        //private void HostSubscriptionService()
        //{
        //    if (!Config.GetTopicServerName().Equals("local"))
        //    {
        //        m_subscribeServiceHost = new ServiceHost(typeof(TopicSubscription));
        //        var netTcpBinding = new NetTcpBinding("TopicBinding");
        //        netTcpBinding.CloseTimeout = TopicConstants.TIME_OUT;

        //        m_subscribeServiceHost.AddServiceEndpoint(
        //            typeof(ISubscription),
        //            netTcpBinding,
        //            "net.tcp://localhost:7002/Sub");
        //        m_subscribeServiceHost.Open();
        //    }
        //}

        //private void HostPublishService()
        //{
        //    if (!Config.GetTopicServerName().Equals("local"))
        //    {
        //        m_publishServiceHost = new ServiceHost(typeof(TopicPublishing));
        //        var tcpBindingpublish = new NetTcpBinding("TopicPublisherBinding");

        //        m_publishServiceHost.AddServiceEndpoint(
        //            typeof(ITopicPublishing),
        //            tcpBindingpublish,
        //            "net.tcp://localhost:7001/Pub");
        //        m_publishServiceHost.Open();
        //    }
        //}

        private static void LoadTopicService()
        {
            try
            {
                AssemblyCache.Initialize();
                string strServerName = Config.GetTopicServerName();
                var strMessage = "Starting topic service in [" + strServerName + "]...";
                PrintToScreen.WriteLine(strMessage);
                Logger.Log(strMessage);
                Connect(strServerName);
                strMessage = "Topic service started in [" + strServerName + "]";
                PrintToScreen.WriteLine(strMessage);
                Logger.Log(strMessage);
                //
                // keep thread working, unless there is an exception
                //
                WaitForever();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void WaitForever()
        {
            var threadWorker = new ThreadWorker();
            threadWorker.WaitForExit = true;
            threadWorker.OnExecute += OnThreadExecute;
            threadWorker.Work();
        }

        private static void OnThreadExecute()
        {
            while (true)
            {
                Thread.Sleep(1000);
            }
        }

        #endregion
    }
}



