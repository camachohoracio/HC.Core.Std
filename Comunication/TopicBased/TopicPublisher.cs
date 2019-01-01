#region

using System;
//using System.ServiceModel;
using System.Threading;
using HC.Core.Comunication.TopicBased.HeartBeat;
using HC.Core.Exceptions;
using HC.Core.Logging;
using HC.Core.Threading;
using HC.Core.Threading.ProducerConsumerQueues;
using HC.Core.Threading.ProducerConsumerQueues.Support;
using System.Collections.Concurrent;
using HC.Core.Comunication.TopicBased.Contracts;
using HC.Core.ConfigClasses;

#endregion

namespace HC.Core.Comunication.TopicBased
{
    public class TopicPublisher
    {
        #region Members

        private string m_strServerName;
        //private ChannelFactory<ITopicPublishing> m_channelFactory;
        private IThreadedQueue<TopicMessage> m_messageQueue;
        private ITopicPublishing m_topicPublishing;
        private bool m_blnIsLocalConnection;
        private readonly object m_lockObject = new object();
        private bool m_blnIsConnected;
        public TopicPublisher m_ownInstance;
        public bool m_blnIsConnecting;
        private readonly object m_publishLock = new object();
        private int m_intPort;
        public static readonly ConcurrentDictionary<string, int> m_publisherLogCounter = 
            new ConcurrentDictionary<string, int>();
        public static readonly ConcurrentDictionary<string, object> m_publisherLogCounterChanges =
            new ConcurrentDictionary<string, object>();
        private ProducerConsumerQueue<TaskWrapper> m_noWaitQueue;
        private static ThreadWorker m_queueSizeLoggerWorker;

        #endregion

        #region Constructors

        public void Connect(
            string strServerName,
            int intPort)
        {
            if(m_blnIsConnected)
            {
                return;
            }
            lock (m_lockObject)
            {
                if(m_blnIsConnected)
                {
                    return;
                }
                if(string.IsNullOrEmpty(strServerName))
                {
                    throw new HCException("Empty server name");
                }
                m_blnIsConnecting = true;
                m_noWaitQueue = new ProducerConsumerQueue<TaskWrapper>(20,10000,false,false);
                m_noWaitQueue.OnWork += task =>
                    {
                        try
                        {
                            task.Wait();
                            task.Dispose();
                        }
                        catch(Exception ex)
                        {
                            Logger.Log(ex);
                        }
                    };

                m_strServerName = strServerName;
                m_intPort = intPort;
                if (m_strServerName.Equals("local"))
                {
                    m_blnIsLocalConnection = true;
                }
                m_ownInstance = new TopicPublisher(
                    strServerName,
                    intPort);
                m_blnIsConnected = true;

                Logger.Log(typeof(TopicPublisher).Name + " is connected to server: "  +
                    strServerName);
                m_blnIsConnecting = false;
            }
        }

        private void TopicClientHeartBeatOnDisconnectedState(string strservername)
        {
            if(m_topicPublishing == null)
            {
                return;
            }
            string strMessage = typeof (TopicPublisher).Name + " server disconnected...";
            Console.WriteLine(strMessage);
            Logger.Log(strMessage);
            m_topicPublishing.Reconnect();
        }

        public TopicPublisher(
            string strServerName,
            int intPort)
        {
            StartPublisher(
                strServerName,
                intPort);
        }

        #endregion

        #region Public

        public void SendMessage(
            object dataObj,
            string strTopic)
        {
            SendMessage(
                dataObj,
                strTopic,
                true);
        }

        public void SendMessage(
            object dataObj,
            string strTopic,
            bool blnWait)
        {
            try
            {
                TopicMessage topicMessage = PrepareTopicMessage(
                    dataObj,
                    strTopic);

                if (blnWait)
                {
                    SendMessage(true, topicMessage);
                }
                else
                {
                    m_noWaitQueue.EnqueueTask(SendMessage(false, topicMessage));
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex, false);
            }
        }

        public TaskWrapper SendMessage(bool blnWait, TopicMessage topicMessage)
        {
            try
            {
                TaskWrapper task = m_messageQueue.EnqueueTask(
                    topicMessage);
                if (blnWait)
                {
                    task.Wait();
                    task.Dispose();
                }
                return task;
            }
            catch (Exception ex)
            {
                Logger.Log(ex, false);
            }
            return null;
        }

        #endregion

        #region Private

        private void StartPublisher(
            string strServerName,
            int intPort)
        {
            m_strServerName = strServerName;
            CreateProxy(strServerName, intPort);
            m_messageQueue =
                new ProducerConsumerQueueLite<TopicMessage>(1,100000); // keep one thead, otherwise the chart will display wrong date order
            m_messageQueue.OnWork +=
                OnPublishMessage;

                m_queueSizeLoggerWorker = new ThreadWorker();
                m_queueSizeLoggerWorker.OnExecute += () =>
                {
                    while (true)
                    {
                        try
                        {
                            int intQueueSize = m_messageQueue.QueueSize;
                            if(intQueueSize > 0)
                            {
                                Console.WriteLine("Topic publisher queue size [" + intQueueSize + "]");
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(ex);
                        }
                        Thread.Sleep(10000);
                    }
                };
            m_queueSizeLoggerWorker.Work();
            //
            // check if topic server is alive
            //
            TopicClientHeartBeat.StartHeartBeat(strServerName);
            TopicClientHeartBeat.OnDisconnectedState += TopicClientHeartBeatOnDisconnectedState;
        }

        public void SendMessageImmediately(
            object dataObj,
            string strTopic)
        {
            try
            {
                TopicMessage topicMessage = PrepareTopicMessage(
                    dataObj,
                    strTopic);
                PublishMessageImmediately(topicMessage);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public void PublishMessageImmediately(TopicMessage topicMessage)
        {
            OnPublishMessage(topicMessage);
        }

        /// <summary>
        ///   Note that this call should be thrad safe
        /// </summary>
        /// <param name = "topicMessage"></param>
        private void OnPublishMessage(TopicMessage topicMessage)
        {
            try
            {
                lock (m_publishLock)
                {
                    if (m_blnIsLocalConnection)
                    {
                        //
                        // local connection. Pass the message
                        //
                        TopicSubscriberCache.GetSubscriber(
                            m_strServerName).Publish(topicMessage);
                    }
                    else
                    {
                        m_topicPublishing.Publish(topicMessage);
                    }
                    int intCounter;
                    m_publisherLogCounter.TryGetValue(topicMessage.TopicName, out intCounter);
                    intCounter++;
                    m_publisherLogCounter[topicMessage.TopicName] = intCounter;
                    m_publisherLogCounterChanges[topicMessage.TopicName] = null;
                }
            }
            catch (Exception ex)
            {
                OnFailurePublish(topicMessage, ex);
            }
        }

        private void OnFailurePublish(TopicMessage topicMessage, Exception ex)
        {
            try
            {
                //if (m_channelFactory == null)
                //{
                //    return;
                //}
                //m_channelFactory.Close();
                StartPublisher(
                    m_strServerName,
                    m_intPort);
                //
                // note => publish again!
                //
                m_topicPublishing.Publish(
                    topicMessage);
                Logger.Log(ex, false);
                string strTopciName = topicMessage.TopicName;
                string strMessage = "Published topic [" + strTopciName + "] in failure mode";
                Logger.Log(strMessage);
                Console.WriteLine(strMessage);
            }
            catch (Exception ex2)
            {
                try
                {
                    string strTopciName = topicMessage.TopicName;
                    string strMessage = "Error published topic [" + strTopciName + "] in failure mode";
                    Logger.Log(strMessage);
                    Console.WriteLine(strMessage);
                    Logger.Log(ex2, false);
                }
                catch (Exception ex3)
                {
                    Logger.Log(ex3, false);
                }
            }
        }

        private void CreateProxy(
            string strServerName,
            int intPort)
        {
            try
            {
                if (!strServerName.Equals("local"))
                {
                    var endpointAddressInString = Config.GetTopicPublisher(strServerName);

                    string strTopicInterface = Config.GetTopicInterface().ToLower();

                    //if (strTopicInterface.Equals("wcf"))
                    //{
                    //    var endpointAddress = new EndpointAddress(
                    //        new Uri(endpointAddressInString),
                    //        new SpnEndpointIdentity(""));
                    //    var netTcpBinding = new NetTcpBinding("TopicPublisherBinding")
                    //                            {
                    //                                CloseTimeout = TopicConstants.TIME_OUT
                    //                            };

                    //    m_channelFactory = new ChannelFactory<ITopicPublishing>(
                    //        netTcpBinding,
                    //        endpointAddress);
                    //    m_channelFactory.Faulted += MChannelFactoryFaulted;
                    //    m_topicPublishing = m_channelFactory.CreateChannel();
                    //    Logger.Log("Topic publisher connected as " + strServerName);
                    //}
                    //else if(strTopicInterface.Equals("0mq"))
                    {
                        m_topicPublishing = new ZmqTopicPublisherConnection(strServerName, intPort);
                    }
                    //else
                    //{
                    //    throw new NotImplementedException();
                    //}
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Error while creating proxy");
                Logger.Log(ex, false);
            }
        }

        private void MChannelFactoryFaulted(object sender, EventArgs e)
        {
            Logger.Log("faulted channel. it is now closed");
            //m_channelFactory.Abort();
        }

        public static TopicMessage PrepareTopicMessage(
            object objData,
            string strTopic)
        {
            var topicMessage = new TopicMessage
                        {
                            EventData = objData, 
                            TopicName = strTopic,
                            PublisherName = HCConfig.ClientUniqueName
                        };
            return topicMessage;
        }

        #endregion
    }
}