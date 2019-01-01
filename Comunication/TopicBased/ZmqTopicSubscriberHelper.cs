#region

using System;
using System.Collections.Generic;
using System.Threading;
using HC.Core.Comunication.RequestResponseBased;
using HC.Core.ConfigClasses;
using HC.Core.DynamicCompilation;
using HC.Core.Events;
using HC.Core.Exceptions;
using HC.Core.Logging;
//using HC.Core.SystemMonitor;
using HC.Core.Threading;
using HC.Core.Threading.ProducerConsumerQueues;
using System.Collections.Concurrent;
using System.Linq;
using HC.Core.Comunication.TopicBased.Contracts;

#endregion

namespace HC.Core.Comunication.TopicBased
{
    public class ZmqTopicSubscriberHelper
    {
        public SubscriberCallback SubscriberCallback { get; private set; }

        private const int CONSUMER_QUEUE_SIZE = 20;

        #region Members

        private readonly string m_strTopic;
        private static ProducerConsumerQueue<HcKeyValuePair<SubscriberCallback, TopicMessage>> m_topicConsumerQueue;
        private static int m_intJobsDone;
        private static int m_intJobsInProgress;
        private static readonly object m_intJobsInProgressLock = new object();
        private static ThreadWorker m_logWorker;
        private static readonly ConcurrentDictionary<string, int> m_topicCounter = new ConcurrentDictionary<string, int>();
        private static readonly ConcurrentDictionary<string, int> m_topicCounterToDo = new ConcurrentDictionary<string, int>();
        private static readonly ConcurrentDictionary<string, object> m_topicCounterChanges = new ConcurrentDictionary<string, object>();
        private static readonly object m_counterLock = new object();
        public static readonly ConcurrentDictionary<string, DateTime> m_topicNameToLastUpdate = new ConcurrentDictionary<string, DateTime>();
        private static readonly ConcurrentDictionary<string, string> m_topicValidator = new ConcurrentDictionary<string, string>();
        private static readonly ConcurrentDictionary<string, string> m_topicValidator2 = new ConcurrentDictionary<string, string>();

        #endregion

        #region Constructors

        static ZmqTopicSubscriberHelper()
        {
            SetConsumerQueue();
            SetQueueMonitor();
        }

        public ZmqTopicSubscriberHelper(
            string strTopic,
            SubscriberCallback subscriberCallback)
        {
            m_strTopic = strTopic;
            SubscriberCallback = subscriberCallback;
        }

        #endregion

        private static void SetConsumerQueue()
        {
            try
            {

                m_topicConsumerQueue =
                    new ProducerConsumerQueue<HcKeyValuePair<SubscriberCallback, TopicMessage>>(
                        CONSUMER_QUEUE_SIZE, 150, false, false, typeof(ZmqTopicSubscriberHelper).Name);
                m_topicConsumerQueue.OnWork += kvp =>
                                                   {
                                                       string strTopicName = string.Empty;
                                                       try
                                                       {
                                                           strTopicName = kvp.Value.TopicName;
                                                           lock (m_intJobsInProgressLock)
                                                           {
                                                               m_intJobsInProgress++;
                                                           }
                                                           lock (m_counterLock)
                                                           {
                                                               int intInProgress;
                                                               m_topicCounterToDo.TryGetValue(strTopicName,
                                                                                              out intInProgress);
                                                               intInProgress++;
                                                               m_topicCounterToDo[strTopicName] = intInProgress;
                                                           }
                                                           kvp.Key.Invoke(kvp.Value);
                                                           m_topicNameToLastUpdate[
                                                               GetCounterKey(strTopicName, kvp.Value.GetConnectionName())
                                                               ] =
                                                               DateTime.Now;

                                                           lock (m_counterLock)
                                                           {
                                                               int intCounter;
                                                               m_topicCounter.TryGetValue(strTopicName, out intCounter);
                                                               intCounter++;
                                                               m_topicCounter[strTopicName] = intCounter;
                                                           }
                                                           m_topicCounterChanges[strTopicName] = null;
                                                       }
                                                       catch (Exception ex)
                                                       {
                                                           Logger.Log(ex);
                                                       }
                                                       finally
                                                       {
                                                           lock (m_intJobsInProgressLock)
                                                           {
                                                               m_intJobsDone++;
                                                               m_intJobsInProgress--;
                                                           }
                                                           int intInProgress;
                                                           if (!string.IsNullOrEmpty(strTopicName))
                                                           {
                                                               m_topicCounterToDo.TryGetValue(strTopicName,
                                                                                              out intInProgress);
                                                               lock (m_counterLock)
                                                               {
                                                                   intInProgress--;
                                                                   m_topicCounterToDo[strTopicName] = intInProgress;
                                                               }
                                                           }
                                                       }
                                                   };
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public static string GetCounterKey(
            string strTopicName, string strConnectionName)
        {
            return strTopicName + strConnectionName;
        }

        private static void SetQueueMonitor()
        {
            m_logWorker = new ThreadWorker();
            m_logWorker.OnExecute += () =>
            {
                while (true)
                {
                    try
                    {
                        PublishTopicSubscriber();
                        PublishTopicSubscriberDetails();
                        PublishTopicPublisher(TopicPublisher.m_publisherLogCounter);
                        PublishTopicPublisherDetails(
                            TopicPublisher.m_publisherLogCounter,
                            TopicPublisher.m_publisherLogCounterChanges);
                        //PerformanceHelper.PublishPerformance();
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                    }
                    Thread.Sleep(10000);
                }
            };
            m_logWorker.Work();
        }

        private static void PublishTopicPublisherDetails(
            ConcurrentDictionary<string, int> counterMap,
            ConcurrentDictionary<string, object> counterMapChanges)
        {
            try
            {
                KeyValuePair<string, object>[] mapArr = counterMapChanges.ToArray();
                for (int i = 0; i < mapArr.Length; i++)
                {
                    var kvp = mapArr[i];
                    int intCounter;
                    if (!counterMap.TryGetValue(kvp.Key, out intCounter))
                    {
                        continue;
                    }
                    string strTopic = kvp.Key;
                    int intToDo;
                    m_topicCounterToDo.TryGetValue(strTopic, out intToDo);
                    
                    string strValue = intToDo + "_" + intCounter;
                    string strKey = HCConfig.ClientUniqueName + "_" + kvp.Key;
                    string strOldValue;
                    if(!m_topicValidator.TryGetValue(strKey, out strOldValue) ||
                        !strOldValue.Equals(strValue))
                    {
                        m_topicValidator[strKey] = strValue;
                        var publishObj = new SelfDescribingClass();
                        publishObj.SetClassName(typeof (ZmqTopicSubscriberHelper).Name + "_Publisherdetails_log");
                        publishObj.SetStrValue("Id", HCConfig.ClientUniqueName);
                        publishObj.SetStrValue("Topic",
                                               strTopic);
                        publishObj.SetIntValue("JobsDone", intCounter);
                        publishObj.SetIntValue("JobsToDo", intToDo);
                        publishObj.SetDateValue("Time", DateTime.Now);

                        LiveGuiPublisherEvent.PublishGrid(
                            EnumReqResp.Admin.ToString(),
                            "Topic",
                            "TopicPublisherDetails",
                            strKey,
                            publishObj);
                    }
                }
                counterMapChanges.Clear();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void PublishTopicPublisher(
            ConcurrentDictionary<string, int> m_counterMap)
        {
            try
            {
                int intTotal = m_counterMap.Values.Sum();
                var publishObj = new SelfDescribingClass();
                publishObj.SetClassName(typeof(ZmqTopicSubscriberHelper).Name + "_Publisher_log");
                publishObj.SetStrValue("Id", HCConfig.ClientUniqueName);
                publishObj.SetIntValue("JobsDone", intTotal);
                publishObj.SetDateValue("Time", DateTime.Now);
                LiveGuiPublisherEvent.PublishGrid(
                    EnumReqResp.Admin.ToString(),
                    "Topic",
                    "TopicPublisher",
                    HCConfig.ClientUniqueName + "__Publisher_log",
                    publishObj);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void PublishTopicSubscriberDetails()
        {
            try
            {
                var mapArr = m_topicCounterChanges.ToArray();
                for (int i = 0; i < mapArr.Length; i++)
                {
                    var kvp = mapArr[i];
                    int intCounter;
                    if (!m_topicCounter.TryGetValue(kvp.Key, out intCounter))
                    {
                        continue;
                    }

                    string strKey = HCConfig.ClientUniqueName + "_" + kvp.Key;
                    string strVal = intCounter.ToString();
                    string strOldVal;
                    if (!m_topicValidator2.TryGetValue(strKey, out strOldVal) ||
                        !strOldVal.Equals(strVal))
                    {
                        m_topicValidator2[strKey] = strVal;
                        var publishObj = new SelfDescribingClass();
                        publishObj.SetClassName(typeof (ZmqTopicSubscriberHelper).Name + "_details_log");
                        publishObj.SetStrValue("Id", HCConfig.ClientUniqueName);
                        publishObj.SetStrValue("Topic",
                                               kvp.Key);
                        publishObj.SetIntValue("JobsDone", intCounter);
                        publishObj.SetDateValue("Time", DateTime.Now);
                        LiveGuiPublisherEvent.PublishGrid(
                            EnumReqResp.Admin.ToString(),
                            "Topic",
                            "TopicSubscriberDetails",
                            strKey,
                            publishObj);
                    }
                }
                m_topicCounter.Clear();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void PublishTopicSubscriber()
        {
            try
            {
                var publishObj = new SelfDescribingClass();
                publishObj.SetClassName(typeof(ZmqTopicSubscriberHelper).Name + "log");
                publishObj.SetStrValue("Id", HCConfig.ClientUniqueName);
                publishObj.SetIntValue("TotalJobs",
                    (m_intJobsInProgress + m_topicConsumerQueue.QueueSize));
                publishObj.SetIntValue("QueueSize",
                    m_topicConsumerQueue.QueueSize);
                publishObj.SetIntValue("InProgress",
                    m_intJobsInProgress);
                publishObj.SetIntValue("JobsDone", m_intJobsDone);
                publishObj.SetDateValue("Time", DateTime.Now);
                LiveGuiPublisherEvent.PublishGrid(
                    EnumReqResp.Admin.ToString(),
                    "Topic",
                    "TopicSubscriber",
                    HCConfig.ClientUniqueName,
                    publishObj);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public void InvokeCallback(
           TopicMessage topicMessage)
        {
            try
            {
                if (!m_strTopic.Equals(topicMessage.TopicName))
                {
                    throw new HCException("Invalid topic");
                }
                m_topicConsumerQueue.EnqueueTask(new HcKeyValuePair<SubscriberCallback, TopicMessage>(
                                                     SubscriberCallback, topicMessage));
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }
    }
}