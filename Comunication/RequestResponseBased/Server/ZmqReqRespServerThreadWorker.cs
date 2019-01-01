#region

using HC.Core.Threading;

#endregion

namespace HC.Core.Comunication.RequestResponseBased.Server
{
    public class ZmqReqRespServerThreadWorker
    {
        private readonly ThreadWorker m_threadWorker;

        public ZmqReqRespServerThreadWorker(
            string strServerName,
            string strIp,
            int intPortName,
            int intPort,
            ZmqReqRespServerAck zmqReqRespServerAck,
            bool blnIsIpcConnection)
        {
            m_threadWorker = new ThreadWorker();
            m_threadWorker.OnExecute += () => ZmqReqRespServer.DoConnect(
                strIp,
                intPortName + intPort,
                zmqReqRespServerAck,
                blnIsIpcConnection);

            m_threadWorker.Work();
        }
    }
}
