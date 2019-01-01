#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HC.Core.Comunication.RequestResponseBased.Server.RequestHub;
using HC.Core.Comunication.TopicBased;
using HC.Core.Exceptions;
using HC.Core.Io.Serialization;
using HC.Core.Io.Serialization.Readers;
using HC.Core.Io.Serialization.Writers;
using HC.Core.Logging;
using HC.Core.Threading;
using LZ4;
using ZMQ;
using Exception = System.Exception;

#endregion

namespace HC.Core.Comunication.RequestResponseBased.Server
{
    public class ZmqReqRespServer : ReqRespServer
    {
        #region Properties

        public static ConcurrentDictionary<string, ResponseServerTask> MapRequestToTask { get; private set; }

        #endregion

        #region Members

        private static bool m_blnIsConnected;
        private static readonly object m_connectLockObj = new object();
        private static int m_intConnectionsLoaded;
        private static readonly object m_connectionsLoadedLock = new object();
        public static object RequestResponseLock { get; private set; }
        private static readonly ThreadWorker m_pingWhoIsWrapper;
        private ZmqReqRespServerThreadWorker m_baseConnection1;
        private ZmqReqRespServerThreadWorker m_baseConnection2;

        #endregion

        #region Constructors

        static ZmqReqRespServer()
        {
            RequestResponseLock = new object();
            MapRequestToTask =
                new ConcurrentDictionary<string, ResponseServerTask>();
            m_pingWhoIsWrapper = new ThreadWorker(ThreadPriority.Highest);
            m_pingWhoIsWrapper.OnExecute += PingWhoIs;
            m_pingWhoIsWrapper.Work();
        }

        #endregion

        #region Private & protected

        protected override void Connect(
            string strServerName,
            int intPort,
            int intConnections)
        {
            if (!m_blnIsConnected)
            {
                lock (m_connectLockObj)
                {
                    if (!m_blnIsConnected)
                    {
                        var zmqReqRespServerAck = new ZmqReqRespServerAck(strServerName);
                        var threads = new List<ZmqReqRespServerThreadWorker>();
                        if (!strServerName.Equals("local"))
                        {
                            bool blnLoadLoopBackIp = true;
                            string strIp = NetworkHelper.GetIpAddr(strServerName);
                            if (NetworkHelper.IsLoopBackIp(strIp))
                            {
                                blnLoadLoopBackIp = false;
                            }
                            for (int i = 0; i < intConnections; i++)
                            {
                                threads.Add(
                                    new ZmqReqRespServerThreadWorker(
                                        strServerName,
                                        strIp,
                                        intPort,
                                        i,
                                        zmqReqRespServerAck,
                                        false));

                                if (blnLoadLoopBackIp)
                                {
                                    threads.Add(
                                        new ZmqReqRespServerThreadWorker(
                                            strServerName,
                                            strIp,
                                            intPort,
                                            i,
                                            zmqReqRespServerAck,
                                            true));
                                }
                            }
                            //
                            // load base connection
                            //
                            m_baseConnection1 = new ZmqReqRespServerThreadWorker(
                                strServerName,
                                strIp,
                                intPort,
                                -1,
                                zmqReqRespServerAck,
                                false);
                            if (blnLoadLoopBackIp)
                            {
                                m_baseConnection2 = new ZmqReqRespServerThreadWorker(
                                    strServerName,
                                    strIp,
                                    intPort,
                                    -1,
                                    zmqReqRespServerAck,
                                    true);
                            }
                            threads.Add(m_baseConnection1);
                            threads.Add(m_baseConnection2);
                        }

                        while (m_intConnectionsLoaded < intConnections + 1)
                        {
                            Thread.Sleep(100);
                        }
                        m_blnIsConnected = true;
                    }
                }
            }
        }

        private static void PingWhoIs()
        {
            string strServerName = Config.GetTopicServerName();
            while (true)
            {
                try
                {
                    KeyValuePair<string, ResponseServerTask>[] mapRequestToTaskArr = MapRequestToTask.ToArray();
                    if (MapRequestToTask != null)
                    {
                        for (int i = 0; i < mapRequestToTaskArr.Length; i++)
                        {
                            ResponseServerTask responseServerTask = mapRequestToTaskArr[i].Value;
                            if (responseServerTask != null)
                            {
                                if (responseServerTask.BlnWait &&
                                    responseServerTask.IsClientConnected())
                                {
                                    var whoIsWrapper = responseServerTask.WhoIsWrapper;
                                    if (whoIsWrapper != null)
                                    {
                                        Socket socket = responseServerTask.Socket;
                                        object socketLock = responseServerTask.SocketLock;
                                        byte[] whoIs = responseServerTask.WhoIs;
                                        if (!responseServerTask.IsDisposed)
                                        {
                                            //
                                            // ping request in process
                                            //
                                            TopicPublisherCache.GetPublisher(
                                                strServerName,
                                                TopicConstants.SUBSCRIBER_HEART_BEAT_PORT).SendMessage(
                                                    mapRequestToTaskArr[i].Key,
                                                    EnumReqResp.WhoIsPingTopic.ToString(),
                                                    true);
                                            whoIsWrapper.PingWhoIs(
                                                socket,
                                                socketLock,
                                                whoIs);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    Logger.Log(ex);
                }
                Thread.Sleep(5000);
            }
        }

        public static void DoConnect(string strIp,
            int intPort,
            ZmqReqRespServerAck zmqReqRespServerAck,
            bool blnIsIpcConnection)
        {
            bool blnSuccess = false;
            while (!blnSuccess)
            {
                try
                {
                    blnSuccess = DoConnect0(intPort,
                        zmqReqRespServerAck,
                        blnIsIpcConnection,
                        strIp);
                    Thread.Sleep(30*1000);
                }
                catch(Exception ex)
                {
                    Logger.Log(ex);
                }
            }
        }

        private static bool DoConnect0(int intPort,
            ZmqReqRespServerAck zmqReqRespServerAck,
            bool blnIsIpcConnection,
            string strIp)
        {
            try
            {
                using (var context = new Context())
                {
                    using (Socket socket = context.Socket(SocketType.XREP))
                    {
                        //string strIp = NetworkHelper.GetIpAddr(strServerName);
                        string strAddr;

                        if (blnIsIpcConnection)
                        {
                            strAddr = "tcp://" + NetworkHelper.LOOP_BACK_IP + ":" + intPort;
                        }
                        else
                        {
                            strAddr = "tcp://" + strIp + ":" + intPort;
                        }
                        socket.HWM = CoreConstants.HWM;
                        socket.Bind(strAddr);
                        lock (m_connectionsLoadedLock)
                        {
                            m_intConnectionsLoaded++;
                        }
                        string strMessage = typeof(ZmqReqRespServer).Name +
                            " loaded addr: " + strAddr;
                        Console.WriteLine(strMessage);
                        Logger.Log(strMessage);
                        var socketLock = new object();
                        var lastPing = new DateTime();
                        while (true)
                        {
                            try
                            {
                                if (!OnRecv(socket,
                                    socketLock,
                                    zmqReqRespServerAck,
                                    ref lastPing))
                                {
                                    //
                                    // slow down
                                    //
                                    Thread.Sleep(30000);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Log(ex);
                                //
                                // slow down
                                //
                                Thread.Sleep(30000);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return false;
        }

        private static bool OnRecv(
            Socket socket,
            object socketLock,
            ZmqReqRespServerAck zmqReqRespServerAck,
            ref DateTime lastPing)
        {
            try
            {
                //
                // get request
                //
                byte[] bytesWhoIs;
                byte[] bytes = null;
                lock (socketLock)
                {
                    bytesWhoIs = socket.Recv(10);
                    if (bytesWhoIs != null &&
                        socket.RcvMore)
                    {
                        //
                        // recieve multipart message here
                        //
                        bytes = RecvMultiPart(socket, 60*1000);
                    }
                }
                if (bytesWhoIs != null &&
                    bytes != null &&
                    bytes.Length > 0)
                {
                    if (bytes.Length == 1 &&
                        bytes[0] == 1)
                    {
                        //
                        // this is a client ping, just do a ping reponse
                        //
                        if ((DateTime.Now - lastPing).TotalSeconds > 4)
                        {
                            lastPing = DateTime.Now;
                            WhoIsWrapper.SendPingMsg(
                                socket,
                                bytesWhoIs,
                                socketLock);
                        }
                    }
                    else
                    {
                        ProcessRequest(socket,
                                       socketLock,
                                       bytes,
                                       bytesWhoIs,
                                       zmqReqRespServerAck);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                return false;
            }
            return true;
        }

        private static byte[] RecvMultiPart(
            Socket socket,
            int intTimeOut)
        {
            byte[] bytes = socket.Recv(intTimeOut);
            bool blnIsMultiMsg = bytes != null && socket.RcvMore;
            List<byte> byteArrList = null;
            bool blnIsValidMessage = true;
            bool blnFoundFirstMessage = false;
            int intBytestSize = 0;
            if (blnIsMultiMsg)
            {
                byteArrList = new List<byte>();
                ISerializerReader serializerReader = Serializer.GetReader(bytes);
                intBytestSize = serializerReader.ReadInt32();
                blnFoundFirstMessage = serializerReader.ReadBoolean();
                if (blnFoundFirstMessage)
                {
                    byteArrList.AddRange(serializerReader.ReadByteArray());
                }
            }

            bool blnFoundLastMesssage = false;
            while (bytes != null &&
                   socket.RcvMore &&
                   byteArrList != null) // keep receiving even if it is not a valid message
            {
                byte[] currBytes = socket.Recv(5 * intTimeOut);
                if (currBytes != null)
                {
                    ISerializerReader serializerReader = Serializer.GetReader(currBytes);
                    intBytestSize = serializerReader.ReadInt32();
                    serializerReader.ReadBoolean(); // dummy first message flag
                    if (blnFoundFirstMessage)
                    {
                        byteArrList.AddRange(serializerReader.ReadByteArray());
                    }
                    blnFoundLastMesssage = serializerReader.ReadBoolean();
                    if (blnFoundLastMesssage)
                    {
                        break;
                    }
                }
            }

            if (blnIsMultiMsg &&
                (!blnFoundFirstMessage ||
                 !blnFoundLastMesssage))
            {
                blnIsValidMessage = false;
            }

            if (bytes != null &&
                byteArrList != null &&
                byteArrList.Count > 0 &&
                blnIsValidMessage)
            {
                bytes = byteArrList.ToArray();
                if (intBytestSize > 0)
                {
                    bytes = LZ4Codec.Decode(bytes, 0, bytes.Length, intBytestSize); //(byte[])MemoryZipper.UnZipMemory(new MemoryStream(bytes));
                }
            }

            if (bytes != null && blnIsValidMessage)
            {
                if(blnIsMultiMsg)
                {
                    double dblMb = Math.Round(((bytes.Length / 1024f) / 1024f), 2);
                    string strMessage = "Debug => " + typeof(ZmqReqRespServer).Name +
                                        " received very large message [" + dblMb + "] mb";
                    Console.WriteLine(strMessage);
                    Logger.Log(strMessage);
                }
                return bytes;
            }
            return null;
        }

        private static void ProcessRequest(
            Socket socket,
            object socketLock,
            byte[] bytes,
            byte[] bytesWhoIs,
            ZmqReqRespServerAck zmqReqRespServerAck)
        {
            try
            {
                while (!m_blnIsConnected ||
                       !IsInitialized)
                {
                    Thread.Sleep(1000);
                    string strMessage = typeof (ZmqReqRespServer).Name +
                                        " is waiting to be connected...";
                    Console.WriteLine(strMessage);
                    Logger.Log(strMessage);
                }

                ISerializerReader serializer = Serializer.GetReader(bytes);

                int intBytesSize = serializer.ReadInt32();
               string strRequestId = serializer.ReadString();
                lock (RequestResponseLock)
                {
                    ResponseServerTask responseServerTask;
                    if (MapRequestToTask.TryGetValue(strRequestId, out responseServerTask)) //&&
                    {
                        responseServerTask.WhoIs = bytesWhoIs;
                        ReqRespServerHeartBeat.OnTopicCallback(
                            responseServerTask.Request.RequestorName);
                        string strMessage = "RequestResponse task [" + strRequestId +
                                            "] has been replaced";
                        Console.WriteLine(strMessage);
                        Logger.Log(strMessage);
                    }
                    else
                    {
                        byte[] byteArr = serializer.ReadByteArray();

                        if (byteArr == null ||
                            byteArr.Length == 0)
                        {
                            throw new HCException("Invalid array size. Request [" +
                                                  strRequestId + "]");
                        }

                        if (intBytesSize > 0)
                        {
                            byteArr = LZ4Codec.Decode(byteArr, 0, byteArr.Length, intBytesSize); //(byte[])MemoryZipper.UnZipMemory(new MemoryStream(byteArr));
                        }
                        //
                        // this is a new request
                        //
                        var request =
                            (RequestDataMessage)
                            SerializerCache.GetSerializer(
                                typeof (RequestDataMessage))
                                .Deserialize(
                                    Serializer.GetReader(
                                        byteArr));
                        request.Error = string.Empty; // reset error, in case of any
                        ReqRespServerHeartBeat.OnTopicCallback(
                            request.RequestorName);
                        responseServerTask = new ResponseServerTask(
                            strRequestId,
                            request,
                            socket,
                            socketLock,
                            bytesWhoIs,
                            zmqReqRespServerAck);
                        MapRequestToTask[strRequestId] = responseServerTask;
                        responseServerTask.Run();
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public static void SendResponse(
            Socket socket,
            string strRequestId,
            RequestDataMessage response,
            object socketLock,
            byte[] bytesWhoIs)
        {
            if (response == null)
            {
                return;
            }

            lock (socketLock)
            {
                socket.SendMore(bytesWhoIs);

                if (!string.IsNullOrEmpty(response.Error))
                {
                    SendSingleMessage(socket, response, strRequestId);
                }
                else if (response.IsAsync)
                {
                    //
                    // send response in multiple parts
                    //
                    List<RequestDataMessage> responseList = ReqRespHelper.GetListOfResponses(response);
                    for (int i = 0; i < responseList.Count - 1; i++)
                    {
                        RequestDataMessage dataMessage = responseList[i];
                        ISerializerWriter writer = Serializer.GetWriter();
                        writer.Write(strRequestId);
                        writer.Write(dataMessage.GetByteArr());
                        byte[] bytes = writer.GetBytes();
                        SendBytes(bytes, socket, true);
                    }
                    if (responseList.Count > 0)
                    {
                        SendSingleMessage(socket, responseList.Last(), strRequestId);
                    }
                    else
                    {
                        SendSingleMessage(socket, response, strRequestId);
                    }
                }
                else
                {
                    SendSingleMessage(socket, response, strRequestId);
                }
            }
        }

        private static void SendSingleMessage(
            Socket socket,
            RequestDataMessage response,
            string strRequestId)
        {
            try
            {
                ISerializerWriter writer = Serializer.GetWriter();
                writer.Write(strRequestId);
                writer.Write(response.GetByteArr());
                byte[] bytes = writer.GetBytes();
                SendBytes(bytes, socket, false);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        #endregion

        #region SendBytes in smaller sizes

        private static void SendBytes(
            byte[] bytes,
            Socket socket,
            bool blnMultipleMessages)
        {
            int intBytesSize = 0;
            if(bytes.Length >= Serializer.m_dblZipLimit)
            {
                intBytesSize = bytes.Length;
                bytes = LZ4Codec.Encode(bytes, 0, bytes.Length);  //MemoryZipper.ZipInMemory(bytes).GetBuffer();
            }

            if (bytes.Length <= Serializer.BYTES_LIMIT)
            {
                ISerializerWriter serializerWriter = Serializer.GetWriter();
                serializerWriter.Write(intBytesSize);
                serializerWriter.Write(true); // check if it is the first message
                serializerWriter.Write(bytes);
                serializerWriter.Write(true); // check if it is the last message
                serializerWriter.Write(blnMultipleMessages);
                SendBytes(
                    serializerWriter.GetBytes(),
                    blnMultipleMessages, 
                    socket);
            }
            else
            {
                List<byte[]> byteList = Serializer.GetByteArrList(bytes);
                int intByteListSize = byteList.Count;
                for (int i = 0; i < intByteListSize; i++)
                {
                    ISerializerWriter serializerWriter = Serializer.GetWriter();
                    serializerWriter.Write(intBytesSize);
                    serializerWriter.Write(i == 0); // check if it is the first message
                    serializerWriter.Write(byteList[i]);
                    serializerWriter.Write(i == intByteListSize - 1); // check if it is the last message
                    serializerWriter.Write(blnMultipleMessages);
                    SendBytes(
                        serializerWriter.GetBytes(),
                        blnMultipleMessages || i < intByteListSize - 1,
                        socket);
                }
                double dblMb = Math.Round(((bytes.Length / 1024f) / 1024f), 2);
                string strMessage = "Debug => " + typeof(ZmqReqRespServer).Name + " sent very large message [" +
                byteList.Count + "]. [" + dblMb + "] mb";
                Console.WriteLine(strMessage);
                Logger.Log(strMessage);
            }
        }

        private static void SendBytes(
            byte[] bytes,
            bool blnSendMore,
            Socket socket)
        {
            bool blnSuccess = false;
            int intCounter = 0;
            while (!blnSuccess)
            {
                var status = SendStatus.Interrupted;

                try
                {
                    status = SendBytesUnlocked(
                        bytes,
                        blnSendMore,
                        socket);
                    blnSuccess = status == SendStatus.Sent;
                    if (!blnSuccess)
                    {
                        string strMessage = typeof(ZmqReqRespServer).Name + " could not send message [" +
                                            status + "][" +
                                            intCounter + "]. Resending...";
                        Logger.Log(strMessage);
                        Console.WriteLine(strMessage);
                        Thread.Sleep(5000);
                    }
                }
                catch (Exception ex)
                {
                    string strMessage = typeof(ZmqReqRespServer).Name + " could not send message [" +
                                        status + "][" +
                                        intCounter + "]. Resending...";
                    Logger.Log(strMessage);
                    Console.WriteLine(strMessage);
                    Logger.Log(ex);
                    Thread.Sleep(5000);
                }
                intCounter++;
            }
        }

        private static SendStatus SendBytesUnlocked(
            byte[] bytes,
            bool blnSendMore,
            Socket socket)
        {
            SendStatus status;
            if (blnSendMore)
            {
                status = socket.Send(bytes, new[] { SendRecvOpt.NOBLOCK, SendRecvOpt.SNDMORE});
            }
            else
            {
                status = socket.Send(bytes, SendRecvOpt.NOBLOCK);
            }
            return status;
        }

        #endregion
    }
}