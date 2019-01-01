#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HC.Core.Comunication.RequestResponseBased.Client.ZmqSocketWrapper;
using HC.Core.Comunication.RequestResponseBased.Server;
using HC.Core.Comunication.RequestResponseBased.Server.RequestHub;
using HC.Core.ConfigClasses;
using HC.Core.Logging;
using System.Threading.Tasks;
using HC.Core.Threading.ProducerConsumerQueues;

#endregion

namespace HC.Core.Comunication.RequestResponseBased.Client
{
    public class ZmqRequestResponseClient : ARequestResponseClient
    {
        private delegate void GetProxyDel(
            string strServerName,
            int intPort,
            int intConnections);

        #region Properties

        public KeyValuePair<ZmqReqRespClientSocketWrapper, SocketInfo> BaseSocket { get; private set; }
        public ConcurrentDictionary<ZmqReqRespClientSocketWrapper, SocketInfo> Sockets { get; private set; }
        public ZmqReqRespClientHeartBeat ZmqReqRespClientHeartBeat { get; private set; }
        public EfficientWorkerManager<RequestJob> SendJobEfficientQueue { get; private set; }
        public int NumConnections { get; private set; }

        #endregion

        #region Members

        private bool m_socketsReady;
        private readonly object m_socketLock = new object();
        private int m_intRequestCounter;

        #endregion

        #region Constructors

        public ZmqRequestResponseClient(
            string strServerName,
            int intPort)
            : base(strServerName, intPort) { }

        #endregion

        public override void Dispose()
        {
            EventHandlerHelper.RemoveAllEventHandlers(this);
            if (Sockets != null &&
                Sockets.Count > 0)
            {
                foreach (ZmqReqRespClientSocketWrapper socketWrapper in Sockets.Keys)
                {
                    socketWrapper.Dispose();
                }
            }
        }

        public override void DoConnect(
            string strServerName,
            int intPort,
            int intConnections)
        {
            try
            {
                if (!strServerName.Equals("local"))
                {
                    if (!m_blnIsLocalConnection)
                    {
                        NumConnections = intConnections;
                        new GetProxyDel(LoadConnections).BeginInvoke(strServerName,
                                                                     intPort,
                                                                     intConnections,
                                                                     null,
                                                                     null);
                        while (!m_socketsReady)
                        {
                            Thread.Sleep(100);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public override List<object> SendRequestAndGetResponse(
            RequestDataMessage requestDataMessage,
            int intTimeOutSeconds)
        {
            try
            {
                List<object> response;
                if (ServerName.Equals("local") ||
                    (ReqRespServer.IsInitialized &&
                     ReqRespServer.OwnInstance.ServerName.Equals(ServerName) &&
                     ReqRespServer.OwnInstance.Port == Port))
                {
                    response = ReqRespService.InvokeRequestEventHandler(
                        requestDataMessage).Response;
                }
                else
                {
                    response = SendRequestAndGetResponseViaService(
                        requestDataMessage,
                        intTimeOutSeconds * 1000);
                }
                return response;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return new List<object>();
        }

        private List<object> SendRequestAndGetResponseViaService(
            RequestDataMessage requestDataMessage,
            int intWaitTimeSeconds)
        {
            try
            {
                Interlocked.Increment(ref m_intRequestCounter);
                
                string strName;
                ZmqReqRespClientSocketWrapper socketWrapper = GetSocket(out strName);
                var responseObjs = new List<object>();

                bool blnIsError = true;
                while (blnIsError)
                {
                    string strRequestId =
                        HCConfig.ClientUniqueName + "_" +
                        requestDataMessage.RequestType + "_" +
                        m_intRequestCounter + "_" +
                        Guid.NewGuid().ToString();
                    
                    List<RequestDataMessage> responseList = socketWrapper.SendRequestAndGetResponse(
                        requestDataMessage,
                        strRequestId,
                        intWaitTimeSeconds);

                    string strError = string.Empty;
                    blnIsError = responseList != null &&
                                      responseList.Count > 0 &&
                                      !(string.IsNullOrEmpty(strError = responseList[0].Error));
                    if (blnIsError)
                    {
                        //
                        // faulty state, resend message
                        //
                        string strMesssage = "Req/Resp client received faulty message [" +
                                             strError + "]. Resending reqId [" +
                                             strRequestId + "] " +
                                             requestDataMessage;
                        Console.WriteLine(strMesssage);
                        Logger.Log(strMesssage);
                        Thread.Sleep(5000); // slow down
                        requestDataMessage.SetResponse(null);
                        requestDataMessage.Error = string.Empty; // reset error
                    }
                    else
                    {
                        if (responseList != null)
                        {
                            for (int i = 0; i < responseList.Count; i++)
                            {
                                responseObjs.AddRange(responseList[i].Response);
                            }
                        }
                    }
                }

                lock (m_socketLock)
                {
                    socketWrapper.NumUsages--;
                }

                return responseObjs;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
                Thread.Sleep(30000);
                return SendRequestAndGetResponseViaService(
                    requestDataMessage,
                    intWaitTimeSeconds);
            }
        }

        private Random m_rng = new Random();
        
        private ZmqReqRespClientSocketWrapper GetSocket(
            out string strName)
        {
            try
            {
                //
                // loop until we manage to get an empty socket
                //
                while (true)
                {
                    strName = string.Empty;
                    if (Sockets.Count > 0)
                    {
                        lock (m_socketLock)
                        {
                            if (Sockets.Count > 0)
                            {
                                KeyValuePair<ZmqReqRespClientSocketWrapper, SocketInfo>[] socketsArr =
                                    Sockets.ToArray();
                                int intMinNumRequests = int.MaxValue;
                                ZmqReqRespClientSocketWrapper socketWrapper = null;
                                for (int i = 0; i < socketsArr.Length; i++)
                                {
                                    ZmqReqRespClientSocketWrapper currSocket = socketsArr[i].Key;
                                    int intCurrReq = currSocket.NumUsages;
                                    if (intCurrReq < intMinNumRequests)
                                    {
                                        strName = socketsArr[i].Value.GetConnectionUrl();
                                        intMinNumRequests = intCurrReq;
                                        socketWrapper = currSocket;
                                    }
                                }

                                if (socketWrapper != null)
                                {
                                    var sockets = (from n in socketsArr
                                     where
                                         n.Key.NumUsages <= intMinNumRequests
                                     select n.Key).ToList();
                                    if (sockets.Count > 0)
                                    {
                                        Shuffle(sockets);
                                        socketWrapper = sockets[0]; 
                                        socketWrapper.NumUsages++;
                                        return socketWrapper;
                                    }
                                }
                            }
                        }
                    }
                    Thread.Sleep(100);
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            strName = null;
            return null;
        }

        public void Shuffle<T>(List<T> a)
        {
            try
            {
                int N = a.Count;
                for (int i = 0; i < N; i++)
                {
                    int r = i + (int)(m_rng.NextDouble() * (N - i)); // between i and N-1
                    Exch(a, i, r);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        // swaps array elements i and j
        private void Exch<T>(List<T> a, int i, int j)
        {
            T swap = a[i];
            a[i] = a[j];
            a[j] = swap;
        }



        private void LoadConnections(
            string strServerName,
            int intPortId,
            int intConnections)
        {
            ZmqReqRespClientSocketWrapper socket;
            SocketInfo socketInfo = CreateSocket(
                strServerName, 
                intPortId-1, 
                out socket);
            BaseSocket = new KeyValuePair<ZmqReqRespClientSocketWrapper, SocketInfo>(
                socket, 
                socketInfo);
            socket.IsBaseSocket = true;
            Sockets = new ConcurrentDictionary<ZmqReqRespClientSocketWrapper, SocketInfo>();
            string strDns = strServerName.ToLower();
            
            //
            // load all connections in parallel
            //
            Parallel.For(intPortId, intPortId +
                    intConnections, i => CreateConnection(strDns, i));
            LoadSendJobQueue();
            m_socketsReady = true;
            ZmqReqRespClientHeartBeat = new ZmqReqRespClientHeartBeat(this);
        }

        private void LoadSendJobQueue()
        {
            try
            {
                SendJobEfficientQueue =
                    new EfficientWorkerManager<RequestJob>(RequestResponseConstants.CLIENT_REQUEST_THREADS);
                SendJobEfficientQueue.OnWork += jobItem => ZmqReqRespClientSocketWrapper.DoSendRequest(jobItem.RequestDataMessage,
                                                                           jobItem.RequestId,
                                                                           jobItem.RequestMap,
                                                                           jobItem.SocketWrapper,
                                                                           jobItem.Rwl);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private void CreateConnection(string strServerName, int intPort)
        {
            try
            {
                ZmqReqRespClientSocketWrapper socket;
                SocketInfo socketInfo = CreateSocket(strServerName, intPort, out socket);
                Sockets[socket] = socketInfo;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private SocketInfo CreateSocket(
            string strServerName, 
            int intPort,
            out ZmqReqRespClientSocketWrapper socket)
        {
            socket = null;
            try
            {
                var socketInfo = new SocketInfo
                                     {
                                         DNS = strServerName,
                                         Port = intPort
                                     };
                socket = new ZmqReqRespClientSocketWrapper(
                    socketInfo,
                    this);
                return socketInfo;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

    }
}