#region

using System;
using System.Collections.Generic;
using System.Threading;
using HC.Core.Comunication.TopicBased.Contracts;
using HC.Core.Io.Serialization;
using HC.Core.Logging;
using ZMQ;

#endregion

namespace HC.Core.Comunication.TopicBased
{
    public class ZmqTopicPublisherConnection : ITopicPublishing
    {
        #region Members

        private Context m_context;
        private Socket m_delerSocket;
        private readonly object m_sendLock = new object();
        private readonly string m_strserverName;
        private readonly int m_intPort;
        private static readonly double m_dblZipLimit = 0.5 * 1024f * 1024f;

        #endregion

        #region Constructors

        public ZmqTopicPublisherConnection(
            string strServerName,
            int intPort)
        {
            m_strserverName = strServerName;
            m_intPort = intPort;
            //
            // connect to dealer
            //
            DoConnection(strServerName, intPort);
        }

        #endregion

        private void DoConnection(
            string strServerName,
            int intPort)
        {
            while (!DoConnection0(
                strServerName,
                intPort))
            {
                DisposeConnection();
                Thread.Sleep(5000);
                DoConnection(strServerName, intPort);
            }
        }

        private bool DoConnection0(
            string strServerName,
            int intPort)
        {
            try
            {
                lock (m_sendLock)
                {
                    Context context = m_context;
                    if (context == null)
                    {
                        context = new Context();
                    }
                    m_delerSocket = context.Socket(SocketType.XREQ);
                    string strIp = NetworkHelper.GetIpAddr(strServerName);
                    string strConnection = "tcp://" + strIp + ":" + intPort;
                    m_delerSocket.HWM = CoreConstants.HWM;
                    m_delerSocket.Connect(strConnection);
                    m_context = context;
                }
            }
            catch(System.Exception ex)
            {
                Logger.Log(ex);
                return false;
            }
            return true;
        }

        private void DisposeConnection()
        {
            try
            {
                if (m_delerSocket != null)
                {
                    m_delerSocket.Dispose();
                }
            }
            catch (System.Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public void Publish(TopicMessage topicMessage)
        {
            string strTopic = topicMessage.TopicName;
            SendBytes(strTopic, topicMessage.GetByteArr());
        }

        #region SendBytes in smaller sizes

        private void SendBytes(string strTopic, byte[] bytes)
        {

            if (bytes.Length <= Serializer.BYTES_LIMIT)
            {
                SendBytes(bytes, false, true);
            }
            else
            {
                List<byte[]> byteList = Serializer.GetByteArrList(bytes);
                int intListSize = byteList.Count;

                lock (m_sendLock) // send all together
                {
                    for (int i = 0; i < intListSize; i++)
                    {
                        var serializerWriter = Serializer.GetWriter();
                        serializerWriter.Write(strTopic);
                        serializerWriter.Write(i == 0); // check if it is the first message
                        serializerWriter.Write(byteList[i]);
                        bool blnLastMessage = i == intListSize - 1;
                        serializerWriter.Write(blnLastMessage); // check if it is the last message

                        SendBytes(
                            serializerWriter.GetBytes(),
                            i < intListSize - 1,
                            false);
                    }
                    double dblMb = Math.Round(((bytes.Length / 1024f) / 1024f), 2);
                    string strMessage = "Debug => " + GetType().Name + " sent very large message [" +
                    byteList.Count + "]. [" + dblMb + "] mb";
                    Console.WriteLine(strMessage);
                    Logger.Log(strMessage);

                }
            }
        }

        private void SendBytes(
            byte[] bytes, 
            bool blnSendMore,
            bool blnPublishLocked)
        {

            bool blnSuccess = false;
            int intCounter = 0;
            while (!blnSuccess)
            {
                var status = SendStatus.Interrupted;

                try
                {
                    if (blnPublishLocked)
                    {
                        status = SendBytesLocked(bytes, blnSendMore);
                    }
                    else
                    {
                        status = SendBytesUnlocked(bytes, blnSendMore);
                    }
                    blnSuccess = status == SendStatus.Sent;
                    if (!blnSuccess)
                    {
                        string strMessage = GetType().Name + " could not send message [" +
                                            status + "][" +
                                            intCounter + "]. Resending...";
                        Logger.Log(strMessage);
                        Console.WriteLine(strMessage);
                        Thread.Sleep(5000);
                    }
                }
                catch (System.Exception ex)
                {
                    string strMessage = GetType().Name + " could not send message [" +
                                        status + "][" +
                                        intCounter + "]. Resending...";
                    Logger.Log(strMessage);
                    Console.WriteLine(strMessage);
                    Logger.Log(ex);
                    Thread.Sleep(5000);
                }
                intCounter++;

                if (intCounter > 10)
                {
                    intCounter = 0;
                    Reconnect();
                }
            }
        }

        private SendStatus SendBytesLocked(
            byte[] bytes, 
            bool blnSendMore)
        {
            SendStatus status;
            lock (m_sendLock)
            {
                status = SendBytesUnlocked(bytes, blnSendMore);
            }
            return status;
        }

        private SendStatus SendBytesUnlocked(byte[] bytes, bool blnSendMore)
        {
            SendStatus status;
            if (blnSendMore)
            {
                status = m_delerSocket.SendMore(bytes);
            }
            else
            {
                status = m_delerSocket.Send(bytes);
            }
            return status;
        }

        #endregion

        public void Reconnect()
        {
            lock (m_sendLock)
            {
                m_delerSocket.Dispose();
            }   
            //
            // note, do not put it inside the lock block, it will get deadlocked!
            //
            DoConnection(m_strserverName,
                m_intPort);
            string strMessage = GetType().Name + " has been reconnected";
            Console.WriteLine(strMessage);
            Logger.Log(strMessage);
        }
    }
}