#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HC.Core.Comunication.TopicBased.Contracts;
using HC.Core.Comunication.TopicBased.HeartBeat;
using HC.Core.Exceptions;
using HC.Core.Io.Serialization;
using HC.Core.Io.Serialization.Interfaces;
using HC.Core.Io.Serialization.Readers;
using HC.Core.Io.Serialization.Writers;
using HC.Core.Logging;
using HC.Core.Threading;
using HC.Core.Threading.ProducerConsumerQueues;
using ZMQ;
using Exception = System.Exception;

#endregion

namespace HC.Core.Comunication.TopicBased
{
    public delegate void PublishAnyMessageDel(TopicMessage topiMessage);

    public class ZmqTopicSubscriber : ITopicSubscriber
    {
        private delegate void PollerDelegate(string strServerName);
        public static event PublishAnyMessageDel OnPublishAnyMessage;

        #region Members

        private Context m_context;
        private Socket m_socket;
        private readonly object m_subscribeLock = new object();
        private ConcurrentDictionary<string, List<ZmqTopicSubscriberHelper>> m_subscribers;
        private bool m_blnIsALocalConnection;
        private Dictionary<string, List<SubscriberCallback>> m_callbacks;
        private bool m_blnIsConnected;
        private readonly object m_connectLockObj = new object();
        private readonly object m_connectCreatedLockObj = new object();
        private readonly object m_startConnectLockObj = new object();
        private string m_strServerName;
        private int m_intPort;
        private ThreadWorker m_subscriberWorker;
        private string m_strDoDisonnect;
        private readonly ConcurrentDictionary<string, List<NotifierDel>> m_subscriberNotifier =
            new ConcurrentDictionary<string, List<NotifierDel>>();
        private static readonly ConcurrentDictionary<string,int> m_topicCounter =
            new ConcurrentDictionary<string, int>();
        private static readonly object m_counterLock = new object();

        private readonly ProducerConsumerQueue<ByteWrapper> m_processBytesQueue;
        
        private static readonly ConcurrentDictionary<string, int> m_topicNameToWaitCounter = 
            new ConcurrentDictionary<string, int>();

        private bool m_blnSubscribing;
        private ThreadWorker m_pollerWorker;

        #endregion

        public ZmqTopicSubscriber()
        {
            try
            {
                m_processBytesQueue = new ProducerConsumerQueue<ByteWrapper>(
                    20, 
                    5000, 
                    false,
                    true); // this is the thing, we dont want to throw items, but also, we dont want to be a slow consumer either

                m_processBytesQueue.Id = "topicsubs" + Guid.NewGuid().ToString();
                //m_processBytesQueue.LogQueuePerformance(m_processBytesQueue.Id);
                //m_processBytesQueue = new ProducerConsumerQueue<ByteWrapper>(20);
                m_processBytesQueue.SetAutoDisposeTasks(true);
                m_processBytesQueue.OnWork += ProcessBytes0;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public void Dispose()
        {
            EventHandlerHelper.RemoveAllEventHandlers(this);
        }

        public void Connect(string strServerName, int intPort)
        {
            try
            {
                if (!m_blnIsConnected)
                {
                    lock (m_startConnectLockObj)
                    {
                        m_subscribers = new ConcurrentDictionary<string, List<ZmqTopicSubscriberHelper>>();
                        m_strServerName = strServerName;
                        m_intPort = intPort;
                        if (!strServerName.Equals("local"))
                        {
                            LoadConnection(strServerName);
                            Thread.Sleep(1000);
                        }
                        else
                        {
                            m_callbacks = new Dictionary<string, List<SubscriberCallback>>();
                            m_blnIsALocalConnection = true;
                        }
                        m_subscriberWorker = new ThreadWorker();
                        m_subscriberWorker.OnExecute +=
                            () =>
                                {
                                    while (true)
                                    {
                                        try
                                        {
                                            //if (intQueueSize > 0)
                                            {
                                                //string strMessage =
                                                //    "###@@@---------TopicSubscriber. In queue [" +
                                                //    intQueueSize + "]";
                                                //Console.WriteLine(strMessage);
                                                //Logger.Log(strMessage);
                                                foreach (
                                                    KeyValuePair<string, int> keyValuePair in m_topicNameToWaitCounter)
                                                {
                                                    if (keyValuePair.Value > 20)
                                                    {
                                                        int intQueueSize = m_processBytesQueue.QueueSize +
                                                                           m_processBytesQueue.TasksInProgress;
                                                        Console.WriteLine("###@@@---------LargeQueue!![" +
                                                                          keyValuePair.Key + "][" +
                                                                          keyValuePair.Value + "][" +
                                                                          intQueueSize + "]");
                                                    }
                                                }
                                            }
                                            //Resubscribe(false);
                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.Log(ex);
                                        }
                                        Thread.Sleep(60000);
                                    }
                                };
                        m_subscriberWorker.Work();
                        TopicClientHeartBeat.OnDisconnectedState += strCurrServerName =>
                                                                        {
                                                                            if(strCurrServerName.Equals(m_strServerName))
                                                                            {
                                                                                m_strDoDisonnect = "Topic client heart beat is disconnected";
                                                                            }
                                                                        };

                        m_blnIsConnected = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private void LoadConnection(
            string strServerName)
        {
            try
            {
                m_socket = null;
                m_pollerWorker = new ThreadWorker();
                m_pollerWorker.OnExecute += () =>
                                                {
                                                    while (true)
                                                    {
                                                        try
                                                        {
                                                            StartPoller(strServerName);
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            Logger.Log(ex);
                                                        }
                                                        Thread.Sleep(1000 * 15);
                                                    }
                                                };
                m_pollerWorker.Work();

                //new PollerDelegate(StartPoller).BeginInvoke(strServerName, null, null);

                while (m_socket == null)
                {
                    string strMessage = GetType().Name + " is not ready [" +
                                        DateTime.Now + "]";
                    Console.WriteLine(strMessage);
                    Logger.Log(strMessage);
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public void Subscribe(
            string strTopic,
            SubscriberCallback subscriberCallback)
        {
            try
            {
                //lock (m_subscribeLock)
                {
                    if (m_blnIsALocalConnection)
                    {
                        SubscribeLocal(strTopic, subscriberCallback);
                    }
                    else
                    {
                        SubscribeService(strTopic, subscriberCallback);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public bool IsSubscribedToTopic(string strTopic)
        {
            return m_subscribers.ContainsKey(strTopic);
        }

        private void SubscribeLocal(
            string strTopic,
            SubscriberCallback subscriberCallback)
        {
            try
            {
                lock (m_subscribeLock)
                {
                    List<SubscriberCallback> callbacks;
                    if (!m_callbacks.TryGetValue(strTopic, out callbacks))
                    {
                        callbacks = new List<SubscriberCallback>();
                        m_callbacks[strTopic] = callbacks;
                    }
                    callbacks.Add(subscriberCallback);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private void SubscribeService(
            string strTopic,
            SubscriberCallback subscriberCallback)
        {
            try
            {
                SubscribeSocket(strTopic);
                var topicHelper = new ZmqTopicSubscriberHelper(
                    strTopic,
                    subscriberCallback);
                List<ZmqTopicSubscriberHelper> topicHelpers;
                if (!m_subscribers.TryGetValue(strTopic, out topicHelpers))
                {
                    topicHelpers = new List<ZmqTopicSubscriberHelper>();
                    m_subscribers[strTopic] = topicHelpers;
                }
                topicHelpers.Add(topicHelper);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private void SubscribeSocket(string strTopic)
        {
            try
            {
                string strMessage;
                while (m_socket == null)
                {
                    Thread.Sleep(1000);
                    strMessage = "Trying to subscribe to topic [" +
                                        strTopic + "] but socket is not ready...";
                    Console.WriteLine(strMessage);
                    Logger.Log(strMessage);
                }

                ISerializerWriter serializer = Serializer.GetWriter();
                serializer.Write(strTopic);
                byte[] bytes = serializer.GetBytes();
                lock (m_connectLockObj)
                {
                    lock (m_connectCreatedLockObj)
                    {
                        m_socket.Subscribe(bytes);
                    }
                }

                strMessage = GetType().Name + ". Socket is subscribed to topic [" +
                                    strTopic + "]";
                Verboser.WriteLine(strMessage);
                Logger.Log(strMessage);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                Logger.Log(ex);
                Logger.Log(GetType().Name + "subscription exception. Try to subsribe again...");
                Thread.Sleep(30000);
                SubscribeSocket(strTopic);
            }
        }

        private void OnRecv()
        {
            try
            {
                if (m_socket == null)
                {
                    return;
                }
                Recv();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private void Recv()
        {
            try
            {
                byte[] bytes = GetBytes();
                bool blnIsMultiMsg;
                lock (m_connectLockObj)
                {
                    blnIsMultiMsg = bytes != null && m_socket.RcvMore;
                }
                List<byte> byteArrList = null;
                bool blnIsValidMessage = true;
                bool blnFoundFirstMessage = false;
                string strTopic = string.Empty;
                if (blnIsMultiMsg)
                {
                    byteArrList = new List<byte>();
                    ISerializerReader serializerReader = Serializer.GetReader(bytes);
                    strTopic = serializerReader.ReadString(); // read the topic
                    blnFoundFirstMessage = serializerReader.ReadBoolean();
                    if (blnFoundFirstMessage)
                    {
                        byteArrList.AddRange(serializerReader.ReadByteArray());
                    }
                }

                bool blnFoundLastMesssage = false;
                bool blnRcvMore;
                lock (m_connectLockObj)
                {
                    blnRcvMore = m_socket.RcvMore;
                }
                while (bytes != null &&
                       blnRcvMore &&
                       byteArrList != null) // keep receiving even if it is not a valid message
                {
                    var currBytes = GetBytes();
                    lock (m_connectLockObj)
                    {
                        blnRcvMore = m_socket.RcvMore;
                    }

                    ISerializerReader serializerReader = Serializer.GetReader(currBytes);
                    serializerReader.ReadString(); // read the topic
                    serializerReader.ReadBoolean(); // dummy first message flag
                    byte[] currBytesArr = serializerReader.ReadByteArray();
                    if (blnFoundFirstMessage)
                    {
                        byteArrList.AddRange(currBytesArr);
                    }
                    blnFoundLastMesssage = serializerReader.ReadBoolean();
                    if (blnFoundLastMesssage)
                    {
                        break;
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
                    double dblMb = Math.Round(((bytes.Length/1024f)/1024f), 2);
                    string strMessage = "Debug => " +
                                        GetType().Name +
                                        " received very large message. Topic [" +
                                        strTopic + "]. [" + dblMb + "] mb";
                    Console.WriteLine(strMessage);
                    Logger.Log(strMessage);
                }

                if (bytes == null)
                {
                    //
                    // intTimeoutMills, reconnect
                    //
                    Reconnect("Bytes null");
                }
                else if (blnIsValidMessage)
                {
                    ProcessBytes(bytes, strTopic);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                Reconnect("Exception [" + ex.Message + "]");
            }
        }

        private byte[] GetBytes()
        {
            byte[] currBytes = null;
            int intTotalTimeOut = 0;
            while (currBytes == null)
            {
                try
                {
                    lock (m_connectLockObj)
                    {
                        var sock = m_socket;
                        currBytes = sock.Recv(
                            TopicConstants.SUBSCRIBER_POLL_WAIT_TIME_OUT);
                    }
                    intTotalTimeOut += TopicConstants.SUBSCRIBER_POLL_WAIT_TIME_OUT/1000;
                    if (currBytes == null)
                    {
                        if ((intTotalTimeOut/60) > 5 ||
                            !string.IsNullOrEmpty(m_strDoDisonnect))
                        {
                            string strReason;
                            if (!string.IsNullOrEmpty(m_strDoDisonnect))
                            {
                                strReason = m_strDoDisonnect;
                            }
                            else
                            {
                                strReason = "Timeout mins [" + (intTotalTimeOut/60) + "]";
                            }
                            intTotalTimeOut = 0;
                            m_strDoDisonnect = string.Empty;
                            Reconnect(true, strReason);
                        }
                    }
                    else
                    {
                        intTotalTimeOut = 0;
                    }
                }
                catch(Exception ex)
                {
                    Logger.Log(ex);
                    Console.WriteLine(ex);
                    Thread.Sleep(10000);
                }
            }
            return currBytes;
        }

        private void ProcessBytes(byte[] bytes, string strTopic)
        {
            try
            {
                lock (m_counterLock)
                {
                    int intCount;
                    m_topicCounter.TryGetValue(strTopic, out intCount);
                    intCount++;
                    m_topicCounter[strTopic] = intCount;
                }
                m_processBytesQueue.EnqueueTask(new ByteWrapper { Bytes = bytes });
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private void ProcessBytes0(ByteWrapper byteWrapper)
        {
            string strTopic = string.Empty;
            try
            {
                TopicMessage topicMessage = TopicMessage.DeserializeStatic(byteWrapper.Bytes);
                if(topicMessage == null 
                    ||  string.IsNullOrEmpty(topicMessage.TopicName))
                {
                    return;
                }
                strTopic = topicMessage.TopicName;
                List<ZmqTopicSubscriberHelper> subscribers;
                lock (m_subscribeLock)
                {
                    if(m_subscribers.TryGetValue(
                        topicMessage.TopicName,
                        out subscribers))
                    {
                        subscribers = subscribers.ToList();
                    }
                }
                if (subscribers != null)
                {
                    for(int i = 0; i <subscribers.Count; i++)
                    {
                        ZmqTopicSubscriberHelper zeroMqTopicSubscriberHelper = subscribers[i];
                        topicMessage.SetConnectionName(GetConnectionName());
                        zeroMqTopicSubscriberHelper.InvokeCallback(topicMessage);
                    }
                }
                if (OnPublishAnyMessage != null)
                {
                    OnPublishAnyMessage(topicMessage);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            finally
            {
                lock (m_counterLock)
                {
                    int intCount;
                    m_topicCounter.TryGetValue(strTopic, out intCount);
                    intCount--;
                    m_topicCounter[strTopic] = intCount;
                }
            }
        }

        private string GetConnectionName()
        {
            return m_strServerName + ":" + m_intPort;
        }

        private void Reconnect(string strReason)
        {
            Reconnect(false, strReason);
        }

        private void Reconnect(
            bool blnForceConnection,
            string strReason)
        {
            if (TopicClientHeartBeat.DoNotPing)
            {
                return;
            }
            if (TopicClientHeartBeat.IsConnected(m_strServerName) &&
                !blnForceConnection)
            {
                return;
            }
            if(m_subscribers.Count == 0)
            {
                return;
            }
            string strMessage = GetType().Name + " is reconnecting [" + 
                strReason + "]. " + 
                string.Join("|", m_subscribers.Keys) + "...";
            Console.WriteLine(strMessage);
            Logger.Log(strMessage);

            lock (m_connectLockObj)
            {
                lock (m_connectCreatedLockObj)
                {
                    m_socket.Dispose();
                }
            }

            CreateConnection(m_strServerName);

            //
            // resubscribe to topics
            //
            Resubscribe(true);
        }

        private void Resubscribe(bool blnNotify)
        {
            try
            {
                if (m_subscribers != null)
                {
                    if(m_blnSubscribing)
                    {
                        return;
                    }
                    lock (m_subscribeLock)
                    {
                        if (m_blnSubscribing)
                        {
                            return;
                        }

                        m_blnSubscribing = true;
                        if (m_subscribers != null)
                        {
                            int intCounter = 0;
                            foreach (string strTopic in m_subscribers.Keys)
                            {
                                string strCounterKey = ZmqTopicSubscriberHelper.GetCounterKey(
                                    strTopic,
                                    GetConnectionName());
                                DateTime lastUpdate;

                                int intTimeCounter;
                                m_topicNameToWaitCounter.TryGetValue(
                                    strCounterKey, out intTimeCounter);

                                if (!ZmqTopicSubscriberHelper.m_topicNameToLastUpdate.TryGetValue(
                                    strCounterKey,
                                    out lastUpdate) ||
                                    (DateTime.Now - lastUpdate).TotalSeconds > 
                                        Math.Min(60*60, 120 * (intTimeCounter + 1))) // slow down logic
                                {
                                    intCounter++;
                                    m_topicNameToWaitCounter[strCounterKey] = intTimeCounter + 1;

                                    SubscribeSocket(strTopic);
                                    if (blnNotify)
                                    {
                                        List<NotifierDel> notifierDelList;
                                        if (m_subscriberNotifier.TryGetValue(strTopic, out
                                                                                           notifierDelList))
                                        {
                                            for (int i = 0; i < notifierDelList.Count; i++)
                                            {
                                                notifierDelList[i].Invoke(strTopic);
                                            }
                                        }
                                    }
                                    ZmqTopicSubscriberHelper.m_topicNameToLastUpdate[strCounterKey] = DateTime.Now;
                                }
                            }
                            if(intCounter > 0)
                            {
                                Console.WriteLine("Warning. Resubscribed to [" + intCounter + "] topics");
                            }
                        }
                        m_blnSubscribing = false;
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public void NotifyDesconnect(
            string strTopic,
            NotifierDel notifierDel)
        {
            List<NotifierDel> notifierDelList;
            if (!m_subscriberNotifier.TryGetValue(strTopic,
               out notifierDelList))
            { 
                notifierDelList = new List<NotifierDel>();
                m_subscriberNotifier[strTopic] = notifierDelList;
            }
            notifierDelList.Add(notifierDel);
        }

        private void StartPoller(string strServerName)
        {
            try
            {
                CreateConnection(strServerName);
                while (true)
                {
                    OnRecv();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private void CreateConnection(string strServerName)
        {
            if (m_intPort == 0)
            {
                throw new HCException("Invalid port");
            }

            lock (m_connectCreatedLockObj)
            {
                lock (m_connectLockObj)
                {
                    if (m_context == null)
                    {
                        m_context = new Context();
                    }
                    Socket socket = m_context.Socket(SocketType.SUB);
                    string strIp = NetworkHelper.GetIpAddr(strServerName);
                    string strAddr = "tcp://" + strIp + ":" + m_intPort;
                    socket.HWM = CoreConstants.HWM;
                    socket.Connect(strAddr);
                    string strMessage = GetType().Name + " is connected to: " +
                                        strAddr;
                    Console.WriteLine(strMessage);
                    Logger.Log(strMessage, false, false, false);

                    //
                    // wait for connection to be loaded
                    //
                    Thread.Sleep(1000);
                    m_socket = socket;
                }
            }
        }

        public void UnSubscribe(string strTopic)
        {
            try
            {
                var serializer = Serializer.GetWriter();
                serializer.Write(strTopic);
                lock (m_connectLockObj)
                {
                    lock (m_connectCreatedLockObj)
                    {
                        m_socket.Unsubscribe(serializer.GetBytes());
                    }
                }
                List<ZmqTopicSubscriberHelper> subscribersList;
                if (m_subscribers.TryRemove(
                    strTopic, out subscribersList) &&
                    subscribersList != null)
                {
                    subscribersList.Clear();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public void Publish(TopicMessage topicMessage)
        {
            try
            {
                if (m_blnIsALocalConnection)
                {
                    PublishToLocal(topicMessage);
                }
                else
                {
                    PublishToService(topicMessage);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public int SubscriberCount(string strTopic)
        {
            try
            {
                List<SubscriberCallback> callbacks;
                if (m_callbacks.TryGetValue(strTopic, out callbacks))
                {
                    return callbacks.Count;
                }
                return 0;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return 0;
        }

        private void PublishToLocal(TopicMessage tioTopicMessage)
        {
            Exception exception = null;
            try
            {
                if (tioTopicMessage != null)
                {
                    try
                    {
                        string strTopicName = tioTopicMessage.TopicName;
                        List<SubscriberCallback> subscriberCallbacks;
                        if (m_callbacks.TryGetValue(
                            strTopicName,
                            out subscriberCallbacks))
                        {
                            lock (m_subscribeLock)
                            {
                                subscriberCallbacks = subscriberCallbacks.ToList();
                            }
                            foreach (SubscriberCallback subscriberCallback in subscriberCallbacks)
                            {
                                subscriberCallback.Invoke(tioTopicMessage);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                        Logger.Log(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                exception = ex;
                Logger.Log(ex);
            }
            if (exception != null)
            {
                throw exception;
            }
        }

        private void PublishToService(TopicMessage topicMessage)
        {
            try
            {
                IDynamicSerializable serializerEv = SerializerCache.GetSerializer(topicMessage.EventData.GetType());
                ISerializerWriter writerEv = Serializer.GetWriter();
                serializerEv.Serialize(topicMessage.EventData, writerEv);
                byte[] bytes = writerEv.GetBytes();
                ISerializerWriter serializer = Serializer.GetWriter();
                serializer.Write(topicMessage.TopicName);
                serializer.Write(bytes);

                bool blnSuccess = false;
                int intCounter = 0;
                var status = SendStatus.Interrupted;

                while (!blnSuccess)
                {
                    try
                    {
                        lock (m_connectLockObj)
                        {
                            status = m_socket.Send(serializer.GetBytes());
                        }
                        blnSuccess = status == SendStatus.Sent;
                        if (!blnSuccess)
                        {
                            string strMessage = typeof(ZmqTopicSubscriber).Name +
                                                " could not send message [" +
                                            status + "][" +
                                            intCounter + "]. Resending...";
                            Logger.Log(strMessage);
                            Console.WriteLine(strMessage);
                            Thread.Sleep(5000);
                        }
                    }
                    catch (Exception ex)
                    {
                        string strMessage = typeof(ZmqTopicSubscriber) +
                                            " could not send message [" +
                                            status + "][" +
                                            intCounter + "]. Resending...";
                        Logger.Log(strMessage);
                        Console.WriteLine(strMessage);
                        Thread.Sleep(5000);
                        Logger.Log(ex);
                    }

                    intCounter++;

                    if (intCounter > 10)
                    {
                        intCounter = 0;
                        Reconnect(true, "Number if trials is [" + intCounter + "]");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }
    }
}