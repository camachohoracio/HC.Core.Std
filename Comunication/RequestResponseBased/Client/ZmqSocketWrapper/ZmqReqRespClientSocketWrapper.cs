#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HC.Core.Comunication.RequestResponseBased.Server.RequestHub;
using HC.Core.Exceptions;
using HC.Core.Io.Serialization;
using HC.Core.Io.Serialization.Readers;
using HC.Core.Io.Serialization.Writers;
using HC.Core.Logging;
using HC.Core.Threading;
using HC.Core.Threading.ProducerConsumerQueues.Support;
using LZ4;
using ZMQ;
using Exception = System.Exception;

#endregion

namespace HC.Core.Comunication.RequestResponseBased.Client.ZmqSocketWrapper
{
    public class ZmqReqRespClientSocketWrapper : IDisposable
    {
        #region Members

        private readonly ZmqRequestResponseClient m_zmqRequestResponseClient;
        private Context m_context;
        private ThreadWorker m_socketRecvWorker;
        private ZmqReqRespClientAck m_zmqReqRespClientAck;
        public readonly ReaderWriterLock m_rwl = new ReaderWriterLock();
        private int m_intContextCounter;
        private const int CONTEXT_RESET_COUNTER = 20;
        private readonly object m_connectLock = new object();
        public readonly object m_sendRcvLock = new object();
        private ThreadWorker m_pingWorker;

        #endregion

        #region Properties

        public ConcurrentDictionary<string, RequestDataMessage> RequestMap { get; private set; }

        public bool IsBaseSocket { get; set; }

        public Socket Socket { get; private set; }

        public DateTime ConnectionDate { get; private set; }

        public SocketInfo EndPointAddr
        {
            get;
            private set;
        }

        public int NumUsages { get; set; }

        public int NumRequests
        {
            get
            {
                return RequestMap.Count;
            }
        }

        public DateTime LastRecvPingTime { get; set; }

        #endregion

        #region Constructors

        public ZmqReqRespClientSocketWrapper(
            SocketInfo socketInfo,
            ZmqRequestResponseClient zmqRequestResponseClient)
        {
            m_zmqRequestResponseClient = zmqRequestResponseClient;
            while (!Startup(socketInfo))
            {
                string strMessage = GetType().Name + " failed to startup...";
                Console.WriteLine(strMessage);
                Logger.Log(strMessage, false, true);
                Thread.Sleep(5000);
            }
        }

        #endregion

        #region Public

        public void Dispose()
        {
            EventHandlerHelper.RemoveAllEventHandlers(this);
            if (Socket != null)
            {
                Socket.Dispose();
            }
        }

        public void Connect()
        {
            bool blnConnected;
            lock (m_connectLock)
            {
                blnConnected = TryToConnect();
            }
            Thread.Sleep(1000);
            if (blnConnected)
            {
                ResendJobs();
            }
        }

        public List<RequestDataMessage> SendRequestAndGetResponse(
            RequestDataMessage requestDataMessage,
            string strRequestId,
            int intWaitTime)
        {
            try
            {
                while (m_zmqRequestResponseClient.ZmqReqRespClientHeartBeat == null)
                {
                    string strMessage = "Heart beat is not ready [" + DateTime.Now + "]";
                    Logger.Log(strMessage);
                    Console.WriteLine(strMessage);
                    Thread.Sleep(1000);
                }
                var tcs = new TaskCompletionSource<object>();
                requestDataMessage.SetTcs(tcs);

                m_zmqRequestResponseClient.ZmqReqRespClientHeartBeat.StartPing(this);
                SendRequest(requestDataMessage,
                            strRequestId,
                            RequestMap,
                            this);

                List<RequestDataMessage> response = WaitForResponse(
                    requestDataMessage,
                    strRequestId,
                    intWaitTime);

                m_zmqReqRespClientAck.SendJobAck(strRequestId);
                return response;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return new List<RequestDataMessage>();
        }

        #endregion

        #region Private

        private bool TryToConnect()
        {
            bool blnException = false;
            var rwl = m_rwl;
            try
            {
                string strMessage;
                rwl.AcquireWriterLock(15 * 60 * 1000);

                m_intContextCounter++;

                if (Socket != null)
                {
                    //
                    // it is reconnecting
                    //
                    Socket.Dispose();
                    Thread.Sleep(1000);
                    strMessage = "RequestResponseClient socket disposed [" + EndPointAddr +
                                 "]. Context counter [" + m_intContextCounter + "]/[" +
                                 CONTEXT_RESET_COUNTER + "]";
                    Logger.Log(strMessage);
                    strMessage = "RequestResponseClient is reconnecting to [" + EndPointAddr +
                                 "]. Context counter [" + m_intContextCounter + "]/[" +
                                 CONTEXT_RESET_COUNTER + "]";
                    Logger.Log(strMessage);

                    if (m_intContextCounter > CONTEXT_RESET_COUNTER)
                    {
                        ResetContext();
                    }
                }
                else
                {
                    strMessage = "RequestResponseClient is connecting to [" + EndPointAddr + "]";
                    Logger.Log(strMessage);
                }
                Socket = m_context.Socket(SocketType.XREQ);
                Socket.HWM = CoreConstants.HWM;
                Socket.Connect(EndPointAddr.GetConnectionUrl());

                strMessage = "RequestResponseClient is connected to [" + EndPointAddr + "]";
                Console.WriteLine(strMessage);
                Logger.Log(strMessage);
                LastRecvPingTime = DateTime.Now;
                ConnectionDate = DateTime.Now;
            }
            catch (Exception ex)
            {
                Thread.Sleep(5000);
                try
                {
                    //
                    // we need to release the lock before the finally state
                    //
                    rwl.ReleaseWriterLock();
                }
                catch (Exception ex2)
                {
                    Logger.Log(ex2);
                }
                if (ex.Message.Contains("Too many open files"))
                {
                    ResetContext();
                }
                TryToConnect();
                blnException = true;
                Console.WriteLine(ex);
                Logger.Log(ex);
            }
            finally
            {
                if (!blnException)
                {
                    try
                    {
                        rwl.ReleaseWriterLock();
                    }
                    catch (Exception ex2)
                    {
                        Logger.Log(ex2);
                    }
                }
            }
            return !blnException;
        }

        private void ResetContext()
        {
            while (!ResetContext0())
            {
                string strMessage = "Faulure to reset context. Trying again [" +
                    DateTime.Now + "]...";
                Logger.Log(strMessage);
                Console.WriteLine(strMessage);
                Thread.Sleep(5000);
            }
        }

        private bool ResetContext0()
        {
            try
            {
                //
                // try to dispose context
                //
                var oldContext = m_context;
                ThreadWorker.StartTaskAsync(() =>
                {
                    try
                    {
                        string strMessage = "Disposing context [" +
                                            EndPointAddr.GetConnectionUrl() + "]...";
                        Console.WriteLine(strMessage);
                        Logger.Log(strMessage);
                        oldContext.Dispose();
                        strMessage = "Context reset done [" + EndPointAddr.GetConnectionUrl() + "]";
                        Console.WriteLine(strMessage);
                        Logger.Log(strMessage);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                    }
                });

                m_context = new Context();
                m_intContextCounter = 0;
                string strMessage2 = "New context created [" + EndPointAddr.GetConnectionUrl() + "]";
                Console.WriteLine(strMessage2);
                Logger.Log(strMessage2);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                return false;
            }
            return true;
        }

        private void ResendJobs()
        {
            try
            {
                KeyValuePair<string, RequestDataMessage>[] requestsArr = RequestMap.ToArray();
                int intRequestLength = requestsArr.Length;
                //
                // avoid spamming the server with continuous resends
                //
                int intRequestLengthHalf = Math.Max(1,
                    m_zmqRequestResponseClient.NumConnections / 2);
                intRequestLengthHalf = Math.Min(intRequestLengthHalf, requestsArr.Length);
                if (intRequestLength > 0)
                {
                    string strMessage = GetType().Name + " Request-Response [" + EndPointAddr +
                                        "] is resending [" + intRequestLengthHalf + "] out of [" +
                                        intRequestLength + "] at [" +
                                        DateTime.Now + "]";
                    Console.WriteLine(strMessage);
                    Logger.Log(strMessage);

                    for (int i = 0; i < intRequestLengthHalf; i++)
                    {
                        RequestDataMessage currReqMessage = requestsArr[i].Value;
                        if (currReqMessage.GetResponse() == null) // this check avoids race conditions
                        {
                            SendRequest(
                                currReqMessage,
                                requestsArr[i].Key,
                                RequestMap,
                                this);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private void PingSocket()
        {
            if(!IsBaseSocket)
            {
                return;
            }
            while (true)
            {
                try
                {
                    if (RequestMap.Count > 0)
                    {
                        SendPingBytes(this);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
                finally
                {
                    Thread.Sleep(4000);
                }
            }
        }

        private bool Startup(SocketInfo socketInfo)
        {
            try
            {
                LastRecvPingTime = DateTime.Now;
                EndPointAddr = socketInfo;
                RequestMap = new ConcurrentDictionary<string, RequestDataMessage>();
                m_zmqReqRespClientAck = new ZmqReqRespClientAck(this);
                m_context = new Context();
                Connect();
                m_socketRecvWorker = new ThreadWorker();
                m_socketRecvWorker.OnExecute += () =>
                {
                    while (true)
                    {
                        try
                        {
                            DoRecvLoop();
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(ex);
                        }
                        finally
                        {
                            Thread.Sleep(60 * 1000); // take a break
                        }
                    }
                };
                m_socketRecvWorker.Work();

                m_pingWorker = new ThreadWorker();
                m_pingWorker.OnExecute += PingSocket;
                m_pingWorker.Work();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                return false;
            }
            return true;
        }

        private void SendRequest(
            RequestDataMessage requestDataMessage,
            string strRequestId,
            ConcurrentDictionary<string, RequestDataMessage> requestMap,
            ZmqReqRespClientSocketWrapper socketWrapper)
        {
            TaskWrapper task = null;
            try
            {
                task = m_zmqRequestResponseClient.SendJobEfficientQueue.AddItem(
                    strRequestId,
                    new RequestJob
                    {
                        RequestDataMessage = requestDataMessage,
                        RequestId = strRequestId,
                        RequestMap = requestMap,
                        SocketWrapper = socketWrapper,
                        Rwl = socketWrapper.m_rwl
                    });
                task.Wait();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            finally
            {
                if (task != null)
                {
                    task.Dispose();
                }
            }
        }

        public static void DoSendRequest(
            RequestDataMessage requestDataMessage,
            string strRequestId,
            ConcurrentDictionary<string, RequestDataMessage> requestMap,
            ZmqReqRespClientSocketWrapper zmqReqRespClientSocketWrapper,
            ReaderWriterLock rwl)
        {
            try
            {
                rwl.AcquireReaderLock(600000);
                byte[] requestBytes = requestDataMessage.GetByteArr();
                ISerializerWriter serializer = Serializer.GetWriter();
                int intBytesSize = 0;
                if (requestBytes.Length >= Serializer.m_dblZipLimit)
                {
                    intBytesSize = requestBytes.Length;
                    requestBytes = LZ4Codec.Encode(
                        requestBytes, 
                        0, 
                        requestBytes.Length);  //MemoryZipper.ZipInMemory(requestBytes).GetBuffer();
                }

                serializer.Write(intBytesSize);
                serializer.Write(strRequestId);
                serializer.Write(requestBytes);
                byte[] bytes = serializer.GetBytes();
                SendBytes(bytes, zmqReqRespClientSocketWrapper, intBytesSize);
                zmqReqRespClientSocketWrapper.LastRecvPingTime = DateTime.Now;
                requestMap[strRequestId] = requestDataMessage;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            finally
            {
                rwl.ReleaseReaderLock();
            }
        }

        private List<RequestDataMessage> WaitForResponse(
            RequestDataMessage requestDataMessage,
            string strRequestId,
            int intWaitTime)
        {
            try
            {
                Task<object> task = requestDataMessage.GetTcs().Task;
                //if (intWaitTime > 0) // TODO, this line broke everything, why??!!!
                //{
                //    task.Wait(intWaitTime);
                //}
                //else
                {
                    task.Wait();
                }
                task.Dispose();
                List<RequestDataMessage> response;
                if ((response = requestDataMessage.GetResponse()) != null)
                {
                    RequestDataMessage dummy;
                    RequestMap.TryRemove(strRequestId, out dummy);
                    return response;
                }
                throw new HCException("Null result");
            }
            catch (Exception ex)
            {
                Logger.Log(
                    new HCException(
                        "Error with message:" +
                        Environment.NewLine +
                        requestDataMessage +
                        Environment.NewLine + "-----------------------------" + Environment.NewLine +
                        ex.Message +
                        Environment.NewLine + "-----------------------------" + Environment.NewLine +
                        ex.InnerException +
                        Environment.NewLine + "-----------------------------" + Environment.NewLine +
                        ex.StackTrace));
            }
            return new List<RequestDataMessage>();
        }

        private void DoRecvLoop()
        {
            while (true)
            {
                bool blnLockAckquired = false;
                ReaderWriterLock rwl = m_rwl;
                try
                {
                    if (RequestMap.Count > 0 ||
                        IsBaseSocket)
                    {
                        rwl.AcquireReaderLock(15 * 60 * 1000);
                        blnLockAckquired = true;
                        bool blnMoreMessages;
                        byte[] bytes = RecvMultiPart(
                            this,
                            Socket,
                            m_zmqRequestResponseClient.ZmqReqRespClientHeartBeat,
                            out blnMoreMessages);

                        if (bytes != null &&
                            bytes.Length > 0)
                        {
                            ISerializerReader serializerReader = Serializer.GetReader(bytes);
                            string strRequestid = serializerReader.ReadString();
                            var response = (RequestDataMessage)SerializerCache.GetSerializer(
                                typeof(RequestDataMessage)).Deserialize(
                                    Serializer.GetReader(serializerReader.ReadByteArray()));
                            var responseList = new List<RequestDataMessage>
                                                   {
                                                       response
                                                   };

                            bool blnRecvMore = Socket.RcvMore;
                            bool blnFailure = false;
                            while (blnRecvMore)
                            {
                                bytes = RecvMultiPart(
                                    this,
                                    Socket,
                                    m_zmqRequestResponseClient.ZmqReqRespClientHeartBeat,
                                    out blnMoreMessages);

                                if (bytes != null &&
                                    bytes.Length > 0)
                                {
                                    blnRecvMore = Socket.RcvMore;
                                    serializerReader = Serializer.GetReader(bytes);
                                    serializerReader.ReadString();
                                    response = (RequestDataMessage)SerializerCache.GetSerializer(
                                        typeof(RequestDataMessage)).Deserialize(
                                            Serializer.GetReader(serializerReader.ReadByteArray()));
                                    responseList.Add(response);
                                }
                                else
                                {
                                    string strMessage =
                                        "****Exception. ReqResp client failed to recieve mesage. Request [" +
                                        strRequestid + "]";
                                    Console.WriteLine(strMessage);
                                    Logger.Log(strMessage);
                                    blnFailure = true;
                                }
                            }

                            RequestDataMessage request;
                            if (RequestMap.TryGetValue(strRequestid, out request))
                            {
                                if (blnFailure)
                                {
                                    responseList[0].Error =
                                        EnumReqRespErrors.IncompleteMessageReceived.ToString();
                                }
                                request.SetResponse(responseList.ToList());
                                TaskCompletionSource<object> task = request.GetTcs();
                                try
                                {
                                    task.SetResult(null);
                                }
                                catch (Exception ex)
                                {
                                    //
                                    // has the task already been completed?
                                    //
                                    Logger.Log(ex);
                                }
                            }
                        }
                    }
                    //
                    // avoid killing the cpu with too many socket calls, todo, is there a better way to do this?
                    //
                    Thread.Sleep(10);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                    //
                    // slow down
                    //
                    Thread.Sleep(5000);
                }
                finally
                {
                    if (blnLockAckquired)
                    {
                        try
                        {
                            rwl.ReleaseReaderLock();
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(ex);
                        }
                    }
                }
            }
        }

        private byte[] RecvMultiPart(
            ZmqReqRespClientSocketWrapper zmqReqRespClientSocketWrapper,
            Socket socket,
            ZmqReqRespClientHeartBeat zmqReqRespClientHeartBeat,
            out bool blnMoreMessages)
        {
            blnMoreMessages = false;
            try
            {
                byte[] bytes;
                lock (zmqReqRespClientSocketWrapper.m_sendRcvLock)
                {
                    bytes = socket.Recv(SendRecvOpt.NOBLOCK);
                }
                if (bytes == null)
                {
                    return null;
                }

                if (zmqReqRespClientHeartBeat == null)
                {
                    string strMessage = "zmqReqRespClientHeartBeat is not ready [" +
                                        DateTime.Now + "]";
                    Console.WriteLine(strMessage);
                    Logger.Log(strMessage);
                    Thread.Sleep(2000);
                    return null;
                }

                if(IsBaseSocket)
                {
                    LastRecvPingTime = DateTime.Now;
                }
                //
                // ping message
                //
                //zmqReqRespClientHeartBeat.Ping(zmqReqRespClientSocketWrapper);
                if ((bytes.Length == 1 &&
                     bytes[0] == 1))
                {
                    return null;
                }

                ISerializerReader serializerReader = Serializer.GetReader(bytes);
                int intBytesSize = serializerReader.ReadInt32();
                bool blnFoundFirstMessage = serializerReader.ReadBoolean();
                bytes = serializerReader.ReadByteArray();
                bool blnFoundLastMesssage = serializerReader.ReadBoolean();
                blnMoreMessages = serializerReader.ReadBoolean();

                bool blnIsMultiMsg = socket.RcvMore &&
                                     blnFoundFirstMessage &&
                                     !blnFoundLastMesssage;
                bool blnIsValidMessage = blnFoundFirstMessage;

                List<byte> byteArrList = null;
                if (blnIsMultiMsg)
                {
                    string strMessage = "Debug => " + typeof(ZmqReqRespClientSocketWrapper).Name +
                                        " is trying to receive a very large message...";
                    Console.WriteLine(strMessage);
                    Logger.Log(strMessage);
                    byteArrList = new List<byte>();
                    byteArrList.AddRange(bytes);
                }
                else if (blnFoundFirstMessage && blnFoundLastMesssage)
                {
                    if (intBytesSize > 0)
                    {
                        bytes = LZ4Codec.Decode(
                            bytes, 
                            0, 
                            bytes.Length, 
                            intBytesSize); //(byte[])MemoryZipper.UnZipMemory(new MemoryStream(bytes));
                    }
                    return bytes;
                }

                while (!blnFoundLastMesssage &&
                       socket.RcvMore &&
                       byteArrList != null) // keep receiving even if it is not a valid message
                {
                    byte[] currBytes;
                    lock (zmqReqRespClientSocketWrapper.m_sendRcvLock)
                    {
                        currBytes = socket.Recv(SendRecvOpt.NOBLOCK);
                    }
                    if (currBytes != null)
                    {
                        //zmqReqRespClientHeartBeat.Ping(zmqReqRespClientSocketWrapper);
                        serializerReader = Serializer.GetReader(currBytes);
                        serializerReader.ReadBoolean(); // dummy first message flag
                        byteArrList.AddRange(serializerReader.ReadByteArray());
                        blnFoundLastMesssage = serializerReader.ReadBoolean();
                        if (blnFoundLastMesssage)
                        {
                            blnMoreMessages = serializerReader.ReadBoolean();
                            break;
                        }
                    }
                }

                if (blnIsMultiMsg &&
                    (!blnFoundLastMesssage))
                {
                    blnIsValidMessage = false;
                }

                if (byteArrList != null &&
                    byteArrList.Count > 0 &&
                    blnIsValidMessage)
                {
                    bytes = byteArrList.ToArray();
                    if (intBytesSize > 0)
                    {
                        bytes = LZ4Codec.Decode(bytes, 0, bytes.Length, intBytesSize);//(byte[])MemoryZipper.UnZipMemory(new MemoryStream(bytes));
                    }
                }

                if (blnIsValidMessage)
                {
                    //zmqReqRespClientHeartBeat.Ping(zmqReqRespClientSocketWrapper);

                    if (blnIsMultiMsg)
                    {
                        double dblMb = Math.Round(((bytes.Length / 1024f) / 1024f), 2);
                        string strMessage = "Debug => " + typeof(ZmqReqRespClientSocketWrapper).Name +
                                            " received very large message [" + dblMb + "] mb";
                        Console.WriteLine(strMessage);
                        Logger.Log(strMessage);
                    }
                    return bytes;
                }
                return null;
            }
            catch (Exception ex)
            {
                var zmqEx = ex as ZMQ.Exception;
                if (zmqEx != null && zmqEx.Errno != (int)ERRNOS.EAGAIN)
                {
                    throw;
                }
                return null;
            }
        }

        #region SendBytes in smaller sizes

        public static void SendPingBytes(
            ZmqReqRespClientSocketWrapper socket)
        {
            ReaderWriterLock rwl = socket.m_rwl;
            try
            {
                rwl.AcquireReaderLock(600000);
                var bytes = new byte[] { 1 };
                SendBytes(bytes, socket, 0);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            finally
            {
                rwl.ReleaseReaderLock();
            }
        }

        private static void SendBytes(
            byte[] bytes,
            ZmqReqRespClientSocketWrapper socket,
            long lngZipSize)
        {
            try
            {

                if (bytes.Length <= Serializer.BYTES_LIMIT)
                {
                    while (!SendBytes(bytes, false, socket))
                    {
                        socket.Connect();
                    }
                }
                else
                {
                    List<byte[]> byteList = Serializer.GetByteArrList(bytes);
                    int intListSize = byteList.Count;

                    bool blnSentAll = false;
                    while (!blnSentAll)
                    {
                        blnSentAll = true;
                        for (int i = 0; i < intListSize; i++)
                        {
                            ISerializerWriter serializerWriter = Serializer.GetWriter();
                            serializerWriter.Write(lngZipSize);
                            serializerWriter.Write(i == 0); // check if it is the first message
                            serializerWriter.Write(byteList[i]);
                            serializerWriter.Write(i == intListSize - 1); // check if it is the last message

                            if (!SendBytes(
                                serializerWriter.GetBytes(),
                                i < intListSize - 1,
                                socket))
                            {
                                blnSentAll = false;
                                break;
                            }
                        }
                        if (!blnSentAll)
                        {
                            socket.Connect();
                        }
                    }
                    double dblMb = Math.Round(((bytes.Length / 1024f) / 1024f), 2);
                    string strMessage = "Debug => " + typeof(ZmqReqRespClientSocketWrapper).Name +
                                        " sent very large message [" + dblMb + "] mb";
                    Console.WriteLine(strMessage);
                    Logger.Log(strMessage);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static bool SendBytes(
            byte[] bytes,
            bool blnSendMore,
            ZmqReqRespClientSocketWrapper socket)
        {
            bool blnSuccess = false;
            int intCounter = 0;
            while (!blnSuccess)
            {
                var status = SendStatus.Interrupted;

                try
                {
                    status = SendBytes0(
                        bytes,
                        blnSendMore,
                        socket.Socket,
                        socket.m_sendRcvLock);
                    blnSuccess = status == SendStatus.Sent;
                    if (!blnSuccess)
                    {

                        string strMessage = typeof(ZmqReqRespClientSocketWrapper).Name + " could not send message [" +
                                            status + "][" +
                                            intCounter + "][" +
                                            socket.EndPointAddr.DNS + "]. Resending...";
                        Logger.Log(strMessage);
                        Console.WriteLine(strMessage);
                        Thread.Sleep(5000);
                    }
                }
                catch (Exception ex)
                {
                    string strMessage = typeof(ZmqReqRespClientSocketWrapper).Name + " could not send message [" +
                                        status + "][" +
                                        intCounter + "][" +
                                        socket.EndPointAddr.DNS + "]. Resending...";
                    Logger.Log(strMessage);
                    Console.WriteLine(strMessage);
                    Logger.Log(ex);
                    Thread.Sleep(5000);
                }
                intCounter++;

                if (intCounter > 10)
                {
                    return false;
                }
            }
            return true;
        }

        public static SendStatus SendBytes0(
            byte[] bytes,
            bool blnSendMore,
            Socket socket,
            object sendLock)
        {
            try
            {
                SendStatus status;
                lock (sendLock)
                {
                    if (blnSendMore)
                    {
                        status = socket.SendMore(bytes);
                    }
                    else
                    {
                        status = socket.Send(bytes, SendRecvOpt.NOBLOCK);
                    }
                }
                return status;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return SendStatus.TryAgain;
        }

        #endregion

        #endregion

    }
}