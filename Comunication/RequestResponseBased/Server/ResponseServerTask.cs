#region

using System;
using System.Threading;
using HC.Core.Comunication.RequestResponseBased.Server.RequestHub;
using HC.Core.Exceptions;
using HC.Core.Logging;
using HC.Core.Threading.ProducerConsumerQueues;
using ZMQ;

#endregion

namespace HC.Core.Comunication.RequestResponseBased.Server
{
    public class ResponseServerTask : IDisposable
    {

        #region Properties

        public byte[] WhoIs { get; set; }
        public RequestDataMessage Request { get; private set; }
        public bool BlnWait { get; private set; }
        public Socket Socket { get; private set; }
        public object SocketLock { get; private set; }
        public WhoIsWrapper WhoIsWrapper { get; private set; }
        public bool IsDisposed { get; private set; }

        #endregion

        #region Members

        private string m_strRequestId;
        private readonly ZmqReqRespServerAck m_zmqReqRespServerAck;
        private static readonly ProducerConsumerQueue<ResponseServerTaskWrapper> m_queue;

        #endregion

        #region Constructors

        static ResponseServerTask()
        {
            try
            {
                m_queue = new ProducerConsumerQueue<ResponseServerTaskWrapper>(100);
                m_queue.SetAutoDisposeTasks(true);
                m_queue.OnWork +=
                    reqServerTask => 
                        reqServerTask.ResponseServerTask.GetResponse();
            }
            catch(System.Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public ResponseServerTask(
            string strRequestId,
            RequestDataMessage request,
            Socket socket,
            object socketLock,
            byte[] whoIs,
            ZmqReqRespServerAck zmqReqRespServerAck)
        {
            if (socketLock == null)
            {
                throw new HCException("Null SocketLock");
            }
            SocketLock = socketLock;
            m_strRequestId = strRequestId;
            Request = request;
            Socket = socket;
            m_zmqReqRespServerAck = zmqReqRespServerAck;
            WhoIs = whoIs;
            BlnWait = true;
            WhoIsWrapper = WhoIsWrapper.GetWhoIsWrapper(whoIs);
        }

        public void Run()
        {
            if (!IsClientConnected())
            {
                FinalizeTask(true);
                return;
            }
            m_queue.EnqueueTask(new ResponseServerTaskWrapper(this));
        }

        private void FinalizeTask(bool blnIsClientDisconnected)
        {
            lock (ZmqReqRespServer.RequestResponseLock)
            {
                ResponseServerTask responseServerTask;
                ZmqReqRespServer.MapRequestToTask.TryRemove(m_strRequestId, out responseServerTask);
                if(responseServerTask != null)
                {
                    responseServerTask.Dispose();
                }
            }

            if (blnIsClientDisconnected)
            {
                //
                // send faulty response
                //
                string strMessage = "Sending faulty response due to client disconnected. " +
                                    Request.Id;
                Console.WriteLine(strMessage);
                Logger.Log(strMessage);

                var response = (RequestDataMessage) Request.Clone();
                response.Error = EnumReqRespErrors.ClientDisconnected.ToString();
                ZmqReqRespServer.SendResponse(
                    Socket,
                    m_strRequestId,
                    response,
                    SocketLock,
                    WhoIs);
            }
            BlnWait = false;
        }

        #endregion

        #region Public

        private void GetResponse()
        {
            try
            {
                if (!IsClientConnected())
                {
                    FinalizeTask(true);
                    return;
                }

                RequestDataMessage response =
                    ReqRespService.InvokeRequestEventHandler(Request);
                bool blnIsClientDisconnected;
                if (!response.GetIsClientDisconnected())
                {
                    blnIsClientDisconnected = false;
                    m_zmqReqRespServerAck.RequestAck(
                        Socket,
                        m_strRequestId,
                        response,
                        SocketLock,
                        WhoIs);
                }
                else
                {
                    blnIsClientDisconnected = true;
                }
                FinalizeTask(blnIsClientDisconnected);
            }
            catch(System.Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public bool IsClientConnected()
        {
            try
            {
                RequestDataMessage request = Request;
                if(IsDisposed ||
                    request == null)
                {
                    return true;
                }

                while (ReqRespServer.ReqRespServerHeartBeat == null)
                {
                    Thread.Sleep(100);
                }
                string strRequestorName = request.RequestorName;
                if (!ReqRespServer.ReqRespServerHeartBeat.IsClientConnected(strRequestorName))
                {
                    string strMessage = "Client [" + strRequestorName + "] is disconnected. Request [" +
                                        m_strRequestId +
                                        " ] is not loaded.";
                    Logger.Log(strMessage);
                    Console.WriteLine(strMessage);
                    FinalizeTask(true);
                    return false;
                }
                return true;
            }
            catch(System.Exception ex)
            {
                Logger.Log(ex);
            }
            return false;
        }

        #endregion

        public void Dispose()
        {
            if(IsDisposed)
            {
                return;
            }
            IsDisposed = true;
            WhoIs = null;
            Request = null;
            m_strRequestId = null;
            Socket = null;
            SocketLock = null;
            WhoIsWrapper = null;
        }
    }
}