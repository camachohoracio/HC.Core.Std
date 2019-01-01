#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using HC.Core.Comunication.RequestResponseBased.Server.RequestHub;
using HC.Core.Logging;
using HC.Core.Threading;
using System.Threading;

#endregion

namespace HC.Core.Comunication.RequestResponseBased.Client
{
    public abstract class ARequestResponseClient : IDisposable
    {
        #region Properties

        public string ServerName { get; protected set; }
        public int Port { get; private set; }

        #endregion

        #region Members

        private static readonly ConcurrentDictionary<string, ARequestResponseClient> m_ownInstances = 
            new ConcurrentDictionary<string, ARequestResponseClient>();
        protected readonly object m_lockObject = new object();
        protected readonly bool m_blnIsLocalConnection;
        private readonly static string m_strDefaultServer;
        private readonly static int m_intDefaultPort;

        #endregion

        #region Constructors

        static ARequestResponseClient()
        {
            m_strDefaultServer = Config.GetDataServerName();
            m_intDefaultPort = Config.GetReqRespPort();
        }

        protected ARequestResponseClient(
            string strServerName,
            int intPort)
        {
            m_blnIsLocalConnection = strServerName.Equals("local");
            ServerName = strServerName;
            Port = intPort;
        }

        public static void Connect(
            string strServerName,
            int intPort,
            int intConnections)
        {
            try
            {
                string strInstanceName = GetInstanceName(
                    strServerName,
                    intPort);
                if (m_ownInstances.ContainsKey(strInstanceName))
                {
                    return;
                }
                lock (LockObjectHelper.GetLockObject(strInstanceName))
                {
                    if (m_ownInstances.ContainsKey(strInstanceName))
                    {
                        return;
                    }
                    ARequestResponseClient ownInstance;
                    //string strTopicInterface = Config.GetTopicInterface().ToLower();
                    //if (strTopicInterface.Equals("wcf"))
                    //{
                    //    ownInstance = new WcfRequestResponseClient(strServerName, intPort);
                    //}
                    //else if (strTopicInterface.Equals("0mq"))
                    {
                        ownInstance = new ZmqRequestResponseClient(strServerName, intPort);
                    }
                    //else
                    //{
                    //    throw new NotImplementedException();
                    //}
                    ownInstance.DoConnect(strServerName, intPort, intConnections);
                    m_ownInstances[strInstanceName] = ownInstance;
                    LoggerPublisher.ConnectPublisher(strServerName);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public static ARequestResponseClient GetDefaultConnection()
        {
            try
            {
                ARequestResponseClient defaultConnection = null;

                while (defaultConnection == null)
                {
                    defaultConnection = GetConnection(
                        m_strDefaultServer,
                        m_intDefaultPort);
                    if (defaultConnection == null)
                    {
                        string strMessage = "defaultConnection is not ready [" +
                                            DateTime.Now + "]";
                        Logger.Log(strMessage);
                        Console.WriteLine(strMessage);
                        Thread.Sleep(1000);
                    }
                }
                return defaultConnection;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public static ARequestResponseClient GetConnection(
            string strServerName,
            int intPort)
        {
            try
            {
                string strInstanceName = GetInstanceName(strServerName,
                                                         intPort);
                ARequestResponseClient currInstance;
                m_ownInstances.TryGetValue(strInstanceName, out currInstance);
                return currInstance;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        #endregion

        private static string GetInstanceName(
            string strServerName,
            int intPort)
        {
            return strServerName + "_" + intPort;
        }

        #region Abstract methods

        public abstract void Dispose();
        public abstract List<object> SendRequestAndGetResponse(
            RequestDataMessage requestDataMessage,
            int intTimeOutSeconds);
        public abstract void DoConnect(
            string serverName,
            int intPort,
            int intConnections);

        #endregion
    }
}