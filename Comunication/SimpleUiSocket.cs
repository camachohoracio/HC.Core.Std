using System.Collections.Generic;
using System.Text;
using System.Threading;
using HC.Core.DynamicCompilation;
using HC.Core.Logging;
using HC.Core.Reflection;
using HC.Core.Threading;
using HC.Core.Threading.ProducerConsumerQueues;
using System;
using System.Net;
using System.Net.Sockets;
using HC.Core.Time;

namespace HC.Core.Comunication
{
    public static class SimpleUiSocket
    {
        #region Properties

        public static bool RequestCache { get; set; }
        public static bool IsQueueFull { get { return GetIsQueueFull(); } }

        #endregion

        private static Socket m_clientSocket;
        private static readonly EfficientWorkerManager<StringSenderWorker> m_queue;
        private static readonly object m_sendLock = new object();
        private static readonly object m_connectionLock = new object();
        private const int QUEUE_CAPACITY = 1000;
        private static bool m_blnLoadCachedData;
        private static DateTime m_logTime;

        static SimpleUiSocket()
        {
            try
            {
                ThreadWorker.StartTaskAsync(() => LoadSocket());
                //while (!LoadSocket())
                //{
                //    Thread.Sleep(5000);
                //}
                m_queue = new EfficientWorkerManager<StringSenderWorker>(2, 50000);
                m_queue.SetAutoDisposeTasks(true);
                m_queue.OnWork += SendWorker;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public static void FlushQueue()
        {
            m_queue.Flush();
        }

        private static void SendWorker(StringSenderWorker state)
        {
            try
            {
                if (state.IsChart)
                {
                    var sb = new StringBuilder(state.Tree);
                    sb.Append("|")
                        .Append(state.Key)
                        .Append("|")
                        .Append(state.Obj)
                        .Append("|IsChart");
                    SendString(sb.ToString());
                }
                else
                {
                    SendObjectLocal(state.Tree,
                                    state.Key,
                                    state.Obj);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public static void DoTest()
        {
            try
            {
                const string toSend = "Hello!";
                // Receiving
                while (true)
                {
                    SendString(toSend);
                }

                //// Receiving
                //while (true)
                //{
                //    byte[] rcvLenBytes = new byte[4];
                //    m_clientSocket.Receive(rcvLenBytes);
                //    int rcvLen = System.BitConverter.ToInt32(rcvLenBytes, 0);
                //    byte[] rcvBytes = new byte[rcvLen];
                //    m_clientSocket.Receive(rcvBytes);
                //    String rcv = System.Text.Encoding.ASCII.GetString(rcvBytes);

                //    Console.WriteLine("Client received: " + rcv);
                //}
                //m_clientSocket.Close();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public static void SendObject(
            string strTree,
            string strKey,
            object obj,
            bool blnForceSend)
        {
            SendObject(strTree,
                strKey,
                obj,
                false,
                blnForceSend);
        }

        public static void SendObject(
            string strTree,
            string strKey,
            object obj,
            bool blnIsChart,
            bool blnForceSend)
        {
            try
            {
                if(m_queue == null)
                {
                    return;
                }
                if (GetIsQueueFull() && !blnForceSend && (DateTime.Now - m_logTime).TotalSeconds > 5)
                {
                    string strMessage = "Coould not send [" +
                                        strTree + "_" + strKey + "]. Queue is full";
                    Console.WriteLine(strMessage);
                    m_logTime = DateTime.Now;
                    //
                    // avoid filling the queue due to small consumption
                    //
                    return;
                }
                var queue = m_queue;
                var sendWorker = new StringSenderWorker
                                     {
                                         Tree = strTree,
                                         Key = strKey,
                                         Obj = obj,
                                         IsChart = blnIsChart
                                     };
                //
                // there is no unique key for charts
                //
                string strQueueKey = strTree + "_" + strKey;
                strQueueKey = blnIsChart ? 
                    (strQueueKey + Guid.NewGuid().ToString()) :
                    strQueueKey;
                queue.AddItem(strQueueKey, sendWorker);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static bool GetIsQueueFull()
        {
            return m_queue.QueueSize > 0.9 * QUEUE_CAPACITY;
        }

        private static void SendObjectLocal(
            string strTree,
            string strKey,
            object obj)
        {
            try
            {
                if (string.IsNullOrEmpty(strTree) ||
                    string.IsNullOrEmpty(strKey) ||
                    obj == null)
                {
                    return;
                }
                SendString(GetStringDescription(strTree, strKey, obj));
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static string GetStringDescription(
            string strTree,
            string strKey,
            object obj)
        {
            try
            {
                if (obj == null)
                {
                    return string.Empty;
                }
                if (typeof(ASelfDescribingClass).IsAssignableFrom(obj.GetType()))
                {
                    obj = SelfDescribingClassFactory.CreateInstance(((ASelfDescribingClass)obj));
                }
                IReflector reflector = ReflectorCache.GetReflector(obj.GetType());
                List<string> props = reflector.GetPropertyNames();
                if (props == null || props.Count == 0)
                {
                    return string.Empty;
                }
                var sb = new StringBuilder(strTree);
                sb.Append("|")
                    .Append(strKey
                        .Replace(";", "_")
                        .Replace("|", "_"))
                    .Append("|");
                string strProps = string.Join(";", props);
                sb.Append(strProps)
                    .Append("|");
                AppendProp(obj, reflector, props, 0, sb);
                for (int i = 1; i < props.Count; i++)
                {
                    sb.Append(";");
                    AppendProp(obj, reflector, props, i, sb);
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return string.Empty;
        }

        private static void AppendProp(
            object obj,
            IReflector reflector,
            List<string> props,
            int i,
            StringBuilder sb)
        {
            try
            {
                object currObj = reflector.GetPropertyValue(
                    obj,
                    props[i]);
                string strCurrStr = string.Empty;
                if (currObj != null)
                {
                    string str;
                    if(currObj is DateTime)
                    {
                        str = DateHelper.ToDateTimeString((DateTime) currObj);
                    }
                    else
                    {
                        str = currObj.ToString();
                    }
                    strCurrStr = str
                        .Replace(";", "_")
                        .Replace("|", "_");
                }

                sb.Append(strCurrStr);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void SendString(string strToSend)
        {
            try
            {
                while (!DoSendString(strToSend))
                {
                    Thread.Sleep(5000);
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static bool DoSendString(string strToSend)
        {
            lock (m_sendLock)
            {
                try
                {
                    if (m_clientSocket == null ||
                        string.IsNullOrEmpty(strToSend))
                    {
                        return true;
                    }
                    int toSendLen = Encoding.ASCII.GetByteCount(strToSend);
                    byte[] toSendBytes = Encoding.ASCII.GetBytes(strToSend);
                    byte[] toSendLenBytes = BitConverter.GetBytes(toSendLen);
                    m_clientSocket.Send(toSendLenBytes);
                    m_clientSocket.Send(toSendBytes);

                    if(m_blnLoadCachedData)
                    {
                        m_blnLoadCachedData = false;
                        RequestCache = true;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    //
                    // we need to reconnect
                    //
                    while (!LoadSocket())
                    {
                        Thread.Sleep(5000);
                    }
                }
            }
            return false;
        }

        private static bool LoadSocket()
        {
            lock (m_connectionLock)
            {
                try
                {
                    Close();
                    var serverAddress = new IPEndPoint(IPAddress.Parse(
                        NetworkHelper.GetIpAddr("admin-pc")), 4343);

                    m_clientSocket = new Socket(
                        AddressFamily.InterNetwork,
                        SocketType.Stream,
                        ProtocolType.Tcp);
                    m_clientSocket.Connect(serverAddress);
                    m_blnLoadCachedData = true;
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                return false;
            }
        }

        private static void Close()
        {
            try
            {
                if(m_clientSocket != null)
                {
                    Socket clientSocket = m_clientSocket;
                    clientSocket.Close();
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }
    }
}
