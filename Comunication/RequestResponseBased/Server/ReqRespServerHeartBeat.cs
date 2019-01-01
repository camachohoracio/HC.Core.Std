#region

using System;
using System.Collections.Concurrent;
using System.Threading;
using HC.Core.Comunication.TopicBased;
using HC.Core.Comunication.TopicBased.Contracts;
using HC.Core.Comunication.TopicBased.HeartBeat;
using HC.Core.DynamicCompilation;
using HC.Core.Events;
using HC.Core.Logging;
using HC.Core.Threading;

#endregion

namespace HC.Core.Comunication.RequestResponseBased.Server
{
    public class ReqRespServerHeartBeat
    {
        #region Delegate & events

        public delegate void ClientDisconnectedDel(string strClientName);
        public event ClientDisconnectedDel OnClientDisconnected;

        #endregion

        #region Properties

        public ConcurrentDictionary<string, SelfDescribingClass> MapClientToPingTime { get; private set; }
        public SelfDescribingClass ProviderStats { get; private set; }

        #endregion

        #region Members

        private readonly ThreadWorker m_threadWorker;
        public ConcurrentDictionary<string, string> m_knownClients;
        private readonly object m_clientLock = new object();
        private readonly string m_strServerName;
        private readonly bool m_blnPublishGui;
        private const double TIME_OUT_SECONDS = 30;
        private int m_intPort;
        private readonly ConcurrentDictionary<string, string> m_publishChecks = 
            new ConcurrentDictionary<string, string>();

        #endregion

        #region Constructors

        public ReqRespServerHeartBeat(
            string strServerName,
            int intPort,
            bool blnPublishGui)
        {
            m_intPort = intPort;
            m_strServerName = strServerName;
            m_blnPublishGui = blnPublishGui;
            m_knownClients = new ConcurrentDictionary<string, string>();
            MapClientToPingTime = new ConcurrentDictionary<string, SelfDescribingClass>();
            ProviderStats = new SelfDescribingClass();
            ProviderStats.SetClassName(typeof(ReqRespServerHeartBeat).Name + "_providerStats");

            m_threadWorker = new ThreadWorker();
            m_threadWorker.OnExecute += CheckDisconnectLoop;
            m_threadWorker.Work();
            ZmqTopicSubscriber.OnPublishAnyMessage += topicMessage =>
                OnTopicCallback(topicMessage.PublisherName);

            TopicSubscriberCache.GetSubscriber(
                strServerName,
                TopicConstants.PUBLISHER_HEART_BEAT_PORT).Subscribe(
                EnumReqResp.AsyncHeartBeatClientToServerTopic.ToString(),
                OnTopicCallback);

            LiveGuiPublisherEvent.RemoveForm(
                EnumReqResp.Admin.ToString(),
                EnumReqResp.RequestResponse.ToString() + "_" +
                m_strServerName + "_" +
                m_intPort);
        }

        #endregion

        #region Public

        public bool IsClientConnected(
            string strClientName)
        {
            SelfDescribingClass dummy;
            if (!MapClientToPingTime.TryGetValue(
                strClientName,
                out dummy))
            {
                string strDummy;
                if (!m_knownClients.TryGetValue(strClientName, out strDummy))
                {
                    //
                    // the client has not yet shown up in the ping list
                    //
                    OnTopicCallback(strClientName);
                    return true;
                }
                return false;
            }
            return true;
        }

        #endregion

        #region Private

        private void OnTopicCallback(TopicMessage topicmessage)
        {
            //var topicParams = (ASelfDescribingClass) topicmessage.EventData;
            string strClientName = topicmessage.PublisherName;
            OnTopicCallback(strClientName);
        }

        public void OnTopicCallback(string strClientName)
        {
            SelfDescribingClass pingStats;
            if (!MapClientToPingTime.TryGetValue(
                strClientName,
                out pingStats))
            {
                lock (m_clientLock)
                {
                    if (!MapClientToPingTime.TryGetValue(
                        strClientName,
                        out pingStats))
                    {
                        pingStats = new SelfDescribingClass();
                        pingStats.SetClassName(typeof(ReqRespServerHeartBeat).Name + "_clientStats");
                        MapClientToPingTime[strClientName] = pingStats;
                        m_knownClients[strClientName] = strClientName;
                        string strMessage = GetType().Name + " " + strClientName + " is now connected";
                        Console.WriteLine(strMessage);
                        Logger.Log(strMessage);
                    }
                }
            }
            pingStats.SetDateValue("Time", DateTime.Now);
        }

        private void CheckDisconnectLoop()
        {
            while (true)
            {
                try
                {
                    //
                    // iterate each client
                    //
                    var clientToPingTimeArr = MapClientToPingTime.ToArray();
                    TopicClientHeartBeatThreadWorker topicClientHeartBeatThreadWorker;
                    double dblLastTopicPingSecs = 0;
                    if (TopicClientHeartBeat.HeartBeatWorker.TryGetValue(m_strServerName,
                                                                     out topicClientHeartBeatThreadWorker))
                    {
                        dblLastTopicPingSecs =
                            (Clock.LastTime - topicClientHeartBeatThreadWorker.LastTopicPingTime).
                                TotalSeconds;
                    }
                    for (int i = 0; i < clientToPingTimeArr.Length; i++)
                    {
                        var kvp = clientToPingTimeArr[i];
                        EnumReqResp connectionState;
                        DateTime lastPingTime;
                        if (!kvp.Value.TryGetDateValue("Time", out lastPingTime))
                        {
                            lastPingTime = DateTime.Now;
                        }
                        double dblTotalSeconds = (Clock.LastTime - lastPingTime).TotalSeconds;
                        double dblTimeoutLimit = Math.Min(TopicClientHeartBeatThreadWorker.CONNECTION_TIME_OUT_SECS,
                                                          TIME_OUT_SECONDS / 2);
                        if (dblTotalSeconds > TIME_OUT_SECONDS &&
                            dblLastTopicPingSecs < dblTimeoutLimit)
                        {
                            connectionState = EnumReqResp.Disconnected;
                        }
                        else
                        {
                            connectionState = EnumReqResp.Connected;
                        }
                        string strClientName = kvp.Key;

                        if (m_blnPublishGui)
                        {
                            //
                            // publish state
                            //
                            SelfDescribingClass publishObj = kvp.Value;
                            publishObj.SetDateValue("Time", lastPingTime);
                            publishObj.SetStrValue(EnumReqResp.ClientName, strClientName);
                            publishObj.SetStrValue(EnumReqResp.ConnectionState, connectionState.ToString());

                            string strConnKey = strClientName + "_" + connectionState.ToString();
                            string strValCheck;
                            if (!m_publishChecks.TryGetValue(kvp.Key, out strValCheck) || // avoid publishing too often
                                !strValCheck.Equals(strConnKey))
                            {
                                m_publishChecks[kvp.Key] = strConnKey;
                                LiveGuiPublisherEvent.PublishGrid(
                                    EnumReqResp.Admin.ToString(),
                                    EnumReqResp.RequestResponse.ToString() + "_" +
                                    m_strServerName + "_" +
                                    m_intPort,
                                    "Connections",
                                    kvp.Key,
                                    publishObj);
                            }
                        }

                        if (connectionState == EnumReqResp.Disconnected)
                        {
                            lock (m_clientLock)
                            {
                                SelfDescribingClass dummy;
                                if (MapClientToPingTime.TryRemove(strClientName, out dummy))
                                {
                                    string strMessage = GetType().Name +
                                                        ". Client [" + strClientName + "] is disconnected";
                                    Console.WriteLine(strMessage);
                                    Logger.Log(strMessage);

                                    if (OnClientDisconnected != null)
                                    {
                                        OnClientDisconnected(strClientName);
                                    }
                                }
                            }
                        }
                    }

                    if (m_blnPublishGui)
                    {
                        //
                        // publish provider stats
                        //
                        ProviderStats.SetDateValue("Time", DateTime.Now);
                        ProviderStats.SetStrValue(
                            EnumReqResp.ConnectionState,
                            EnumReqResp.Connected.ToString());
                        LiveGuiPublisherEvent.PublishGrid(
                            EnumReqResp.Admin.ToString(),
                            EnumReqResp.RequestResponse.ToString() + "_" +
                            m_strServerName + "_" +
                                m_intPort,
                            "Service",
                            "Service",
                            ProviderStats,
                            0,
                            true);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
                Thread.Sleep(3000);
            }
        }

        #endregion
    }
}