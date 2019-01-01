#region

using System;
using System.Collections.Concurrent;
using System.Text;
using HC.Core.Logging;
using ZMQ;
using Exception = System.Exception;

#endregion

namespace HC.Core.Comunication.RequestResponseBased.Server
{
    public class WhoIsWrapper
    {
        private const int WHO_IS_PING_TIME_SECONDS = 5;

        #region Members

        private readonly object m_lockObj = new object();
        private static readonly ConcurrentDictionary<string, WhoIsWrapper> m_whoIsMap =
            new ConcurrentDictionary<string, WhoIsWrapper>();
        private static readonly object m_whoIsLock =
            new object();
        private DateTime m_lastUpdateTime;

        #endregion

        public static WhoIsWrapper GetWhoIsWrapper(
            byte[] whoIs)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < whoIs.Length; i++)
            {
                sb.Append(whoIs[i]);
            }
            string strKey = sb.ToString();
            WhoIsWrapper whoIsWrapper;
            if (!m_whoIsMap.TryGetValue(
                strKey,
                out whoIsWrapper))
            {
                lock (m_whoIsLock)
                {
                    if (!m_whoIsMap.TryGetValue(
                        strKey,
                        out whoIsWrapper))
                    {
                        whoIsWrapper = new WhoIsWrapper();
                        m_whoIsMap[strKey] = whoIsWrapper;
                    }
                }
            }
            return whoIsWrapper;
        }

        public void PingWhoIs(
            Socket socket,
            object socketLock,
            byte[] whoIs)
        {
            var now = Clock.LastTime;
            if ((now - m_lastUpdateTime).TotalSeconds > WHO_IS_PING_TIME_SECONDS)
            {
                lock (m_lockObj)
                {
                    if ((now - m_lastUpdateTime).TotalSeconds > WHO_IS_PING_TIME_SECONDS)
                    {
                        lock (socketLock)
                        {
                            try
                            {
                                m_lastUpdateTime = DateTime.Now;
                                //SendPingMsg(socket, whoIs); // ping should be done only by the base socket
                            }
                            catch (Exception ex)
                            {
                                Logger.Log(ex);
                            }
                        }
                    }
                }
            }
        }

        public static void SendPingMsg(
            Socket socket, 
            byte[] whoIs,
            object socketLock)
        {
            lock (socketLock)
            {
                socket.SendMore(whoIs);
                socket.Send(new byte[] {1});
            }
        }
    }
}