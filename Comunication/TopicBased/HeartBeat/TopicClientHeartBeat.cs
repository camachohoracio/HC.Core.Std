#region

using System.Collections.Concurrent;
using HC.Core.Comunication.RequestResponseBased;
using HC.Core.Logging;

#endregion

namespace HC.Core.Comunication.TopicBased.HeartBeat
{
    public delegate void ConnectionStateDel(string strServerName);
    
    public static class TopicClientHeartBeat
    {
        public static bool DoNotPing { get; set; }
        public static ConcurrentDictionary<string, TopicClientHeartBeatThreadWorker> HeartBeatWorker { get; private set; }

        #region Events & delegates

        public static event ConnectionStateDel OnDisconnectedState;

        #endregion

        #region Members

        private static readonly object m_lockObject = new object();
        private static readonly ConcurrentDictionary<string, object> m_serversConnected;

        #endregion

        #region Constructors

        static TopicClientHeartBeat()
        {
            HeartBeatWorker = new ConcurrentDictionary<string, TopicClientHeartBeatThreadWorker>();
            m_serversConnected = new ConcurrentDictionary<string, object>();
        }

        #endregion

        public static bool IsConnected(string strServerName)
        {
            TopicClientHeartBeatThreadWorker heartBeatWorker;
            if(HeartBeatWorker.TryGetValue(strServerName, out heartBeatWorker))
            {
                return heartBeatWorker.ConnectionState == EnumReqResp.Connected;
            }
            return false;
        }

        public static void StartHeartBeat(string strServerName)
        {
            if(DoNotPing)
            {
                return;
            }
            if (!m_serversConnected.ContainsKey(strServerName))
            {
                lock (m_lockObject)
                {
                    if (!m_serversConnected.ContainsKey(strServerName))
                    {
                        m_serversConnected[strServerName] = null;
                        
                        HeartBeatWorker[strServerName] = 
                            new TopicClientHeartBeatThreadWorker(strServerName,
                                InvokeOnDisconnectedState);
                        Logger.Log("Started " + typeof(TopicClientHeartBeat).Name + " heart beat");
                    }
                }
            }
        }

        private static void InvokeOnDisconnectedState(string strservername)
        {
            if (DoNotPing)
            {
                return;
            }
            if (OnDisconnectedState != null)
            {
                OnDisconnectedState(strservername);
            }
        }
    }
}