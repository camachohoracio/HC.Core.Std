#region

using HC.Core.Logging;

#endregion

namespace HC.Core.Comunication.RequestResponseBased.Server
{
    public abstract class ReqRespServer
    {
        #region Properties

        public static ReqRespServer OwnInstance { get; private set; }
        public static bool IsInitialized { get; private set; }
        public static ReqRespServerHeartBeat ReqRespServerHeartBeat { get; private set; }
        public string ServerName { get; private set; }
        public int Port { get; private set; }

        #endregion

        #region Mebers

        private static readonly object m_lockObj = new object();

        #endregion

        #region Public

        public static void StartService(
            string strServerName,
            int intPort,
            int intConnections)
        {
            if (!IsInitialized)
            {
                lock (m_lockObj)
                {
                    if (!IsInitialized)
                    {
                        string strTopicInterface = Config.GetTopicInterface().ToLower();
                        //if (strTopicInterface.Equals("wcf"))
                        //{
                        //    OwnInstance = new WcfReqRespServer();
                        //}
                        //else if (strTopicInterface.Equals("0mq"))
                        {
                            OwnInstance = new ZmqReqRespServer();
                        }
                        //else
                        //{
                        //    throw new NotImplementedException();
                        //}

                        OwnInstance.Connect(strServerName,
                            intPort,
                            intConnections);

                        ReqRespServerHeartBeat = new ReqRespServerHeartBeat(
                            Config.GetTopicServerName(),
                            intPort,
                            true);

                        OwnInstance.ServerName = strServerName;
                        OwnInstance.Port = intPort;
                        IsInitialized = true;
                        Logger.Log(typeof(ReqRespServer).Name  +
                            " initialized for DNS [" + strServerName + "]");
                    }
                }
            }
        }

        protected abstract void Connect(string strServerName,
            int intPort,
            int intConnections);

        #endregion
    }
}