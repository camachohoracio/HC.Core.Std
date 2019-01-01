#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HC.Core.Comunication.TopicBased;
using HC.Core.Comunication.TopicBased.Contracts;
using HC.Core.ConfigClasses;
using HC.Core.DynamicCompilation;
using HC.Core.Logging;
using HC.Core.Threading;

#endregion

namespace HC.Core.Comunication.RequestResponseBased.Client.ZmqSocketWrapper
{
    public class ZmqReqRespClientHeartBeat
    {
        #region Constants

        private const int INDIVIDUAL_SOCKET_TIMEOUT_MINS = 5;
        public const int TIME_OUT_SECS = 30;

        #endregion

        #region Members

        private readonly ThreadWorker m_threadWorker;
        //private DateTime m_lastRecvPingTime;
        private bool m_blnPingStarted;
        private readonly object m_lockPingStart = new object();
        private ThreadWorker m_baseSocketWorker;
        private static ThreadWorker m_pingWorker;
        private readonly ZmqRequestResponseClient m_zmqRequestResponseClient;
        private static readonly ConcurrentDictionary<String,DateTime> m_whoIsPingMap =
                new ConcurrentDictionary<String, DateTime>();
        private static readonly ThreadWorker m_whoIsMapFlusher;

        #endregion

        #region Constructors

        static ZmqReqRespClientHeartBeat()
        {
            try
            {
                String strServerName = Config.GetTopicServerName();

                TopicSubscriberCache.GetSubscriber(
                    strServerName,
                    TopicConstants.PUBLISHER_HEART_BEAT_PORT).Subscribe(
                        EnumReqResp.WhoIsPingTopic.ToString(),
                        OnWhoIsPing);

                //
                // flush whois requests
                //
                m_whoIsMapFlusher = new ThreadWorker();
                m_pingWorker = new ThreadWorker();
                m_pingWorker.OnExecute += OnPingWorker;

                m_whoIsMapFlusher.Work();

                LoadPingWorker();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void OnPingWorker()
        {
            try
            {
                while (true)
                {
                    try
                    {
                        var keysToDelete = new List<string>();

                        foreach (var kvp in m_whoIsPingMap)
                        {
                            double intMins =
                                (DateTime.Now - kvp.Value).TotalMinutes;

                            if (intMins > 60)
                            {
                                keysToDelete.Add(kvp.Key);
                            }
                        }

                        if (keysToDelete.Count > 0)
                        {
                            foreach (String strKey in keysToDelete)
                            {
                                DateTime dummy;
                                m_whoIsPingMap.TryRemove(strKey, out dummy);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                    }
                    finally
                    {
                        Thread.Sleep(60000);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void OnWhoIsPing(TopicMessage topicMessage)
        {
            if (topicMessage.EventData != null)
            {
                var strRequestId = (string) topicMessage.EventData;
                m_whoIsPingMap[strRequestId] =
                                   DateTime.Now;
            }
        }

        public ZmqReqRespClientHeartBeat(
            ZmqRequestResponseClient zmqRequestResponseClient)
        {
            try
            {
                m_zmqRequestResponseClient = zmqRequestResponseClient;
                m_threadWorker = new ThreadWorker();
                m_threadWorker.OnExecute += PingThread;
                m_threadWorker.Work();
                StartBaseSocketPinger(zmqRequestResponseClient.BaseSocket);
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        #endregion

        #region Public

        private void StartBaseSocketPinger(
            KeyValuePair<ZmqReqRespClientSocketWrapper, SocketInfo> baseSocket)
        {
            m_baseSocketWorker = new ThreadWorker();
            m_baseSocketWorker.OnExecute += () => OnBaseSocketWorker(baseSocket);
            m_baseSocketWorker.Work();
        }

        private static void OnBaseSocketWorker(
            KeyValuePair<ZmqReqRespClientSocketWrapper, SocketInfo> baseSocket)
        {
            try
            {
                while (true)
                {
                    ReaderWriterLock rwl = baseSocket.Key.m_rwl;
                    try
                    {
                        rwl.AcquireReaderLock(15*60*1000);
                        ZmqReqRespClientSocketWrapper.SendBytes0(
                            new byte[] {1},
                            false,
                            baseSocket.Key.Socket,
                            baseSocket.Key.m_sendRcvLock);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                        Thread.Sleep(5000);
                    }
                    finally
                    {
                        try
                        {
                            rwl.ReleaseReaderLock();
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(ex);
                        }
                        Thread.Sleep(4000);
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public void StartPing(ZmqReqRespClientSocketWrapper socketWrapper)
        {
            try
            {
                if(!socketWrapper.IsBaseSocket)
                {
                    return;
                }

                if (m_blnPingStarted)
                {
                    return;
                }
                lock (m_lockPingStart)
                {
                    if (m_blnPingStarted)
                    {
                        return;
                    }

                    ZmqReqRespClientSocketWrapper.SendPingBytes(
                        socketWrapper);

                    m_blnPingStarted = true;
                    m_zmqRequestResponseClient.BaseSocket.Key.LastRecvPingTime = DateTime.Now;
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public void Ping(ZmqReqRespClientSocketWrapper socketWrapper)
        {
            try
            {
                m_zmqRequestResponseClient.BaseSocket.Key.LastRecvPingTime = DateTime.Now;
                //socketWrapper.LastRecvPingTime = m_lastRecvPingTime;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public static void LoadPingWorker()
        {
            try
            {
                string strServerName = Config.GetTopicServerName();
                m_pingWorker = new ThreadWorker(ThreadPriority.Highest);
                m_pingWorker.OnExecute += () => OnPingWorker(strServerName);
                m_pingWorker.Work();

                Logger.Log("Loaded AsynClient [" + HCConfig.ClientUniqueName + "] ping worker.");
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void OnPingWorker(string strServerName)
        {
            while (true)
            {
                try
                {
                    while (TopicPublisherCache.GetPublisher(
                        strServerName,
                        TopicConstants.SUBSCRIBER_HEART_BEAT_PORT) == null)
                    {
                        string strMessage = "[" + typeof (ZmqReqRespClientSocketWrapper).Name +
                                            "]. Topic publisher is not initialized.";
                        Console.WriteLine(strMessage);
                        Logger.Log(strMessage);
                        Thread.Sleep(1000);
                    }
                    var pingObj = new SelfDescribingClass();
                    string strTopic = EnumReqResp.AsyncHeartBeatClientToServerTopic.ToString();
                    pingObj.SetClassName(typeof (ZmqRequestResponseClient).Name + "1_" +
                                         strTopic);
                    TopicPublisherCache.GetPublisher(
                        strServerName,
                        TopicConstants.SUBSCRIBER_HEART_BEAT_PORT).SendMessageImmediately(
                            pingObj,
                            strTopic);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
                Thread.Sleep(1000);
            }
        }

        #endregion

        #region Private

        private void PingThread()
        {
            while (true)
            {
                try
                {
                    //CheckIndividualRequests();

                    CheckServerPings();

                    //CheckIndivudalSocketTimeout();
                }
                catch(Exception ex)
                {
                    Logger.Log(ex);
                    Thread.Sleep(30000);
                }
                finally
                {
                    Thread.Sleep(2000);
                }
            }
        }

        private void CheckIndividualRequests()
        {
            try
            {
                foreach (var kvp in m_zmqRequestResponseClient.Sockets)
                {
                    //
                    // iterate each request map
                    //
                    foreach (var kvpReqMap in kvp.Key.RequestMap)
                    {
                        string strReqId = kvpReqMap.Key;

                        if (!m_whoIsPingMap.ContainsKey(strReqId))
                        {
                            m_whoIsPingMap[strReqId] = DateTime.Now;
                        }

                        DateTime lastPingTime = m_whoIsPingMap[strReqId];
                        double intSeconds = (DateTime.Now - lastPingTime).TotalSeconds;

                        if (intSeconds > 300)
                        {
                            //
                            // disconnection is detected
                            //
                            string strMessage = "ReqRespId [" +
                                                strReqId + "] timeout at [" +
                                                DateTime.Now + "][" + intSeconds + "] secs. Reconnecting...";
                            Logger.Log(strMessage);
                            Console.WriteLine(strMessage);

                            kvp.Key.Connect();
                            Ping(kvp.Key);
                            m_whoIsPingMap[strReqId] = DateTime.Now;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private void CheckServerPings()
        {
            try
            {
                DateTime now = Clock.LastTime;
                if ((now - m_zmqRequestResponseClient.BaseSocket.Key.LastRecvPingTime).TotalSeconds > 
                        TIME_OUT_SECS &&
                    m_zmqRequestResponseClient.BaseSocket.Key.LastRecvPingTime != new DateTime())
                {
                    //
                    // check if there are requests to do
                    //
                    bool blnExistsRequests = false;
                    foreach (var kvp in m_zmqRequestResponseClient.Sockets)
                    {
                        if (kvp.Key.NumRequests > 0)
                        {
                            string strActiveRequests = string.Join(",",
                                                                   from n in
                                                                       kvp.Key.RequestMap.Values
                                                                   select n.ToString());
                            string strMessage = "Connection timeout. Active requests [" +
                                                strActiveRequests + "]";
                            Logger.Log(strMessage);
                            Console.WriteLine(strMessage);
                            blnExistsRequests = true;
                            break;
                        }
                    }

                    //if (blnExistsRequests)
                    {
                        //
                        // reconnect base socket. Very important! connect the base socket in the end. 
                        // todo if the base socket connects fine, then how do we know if a particular socket dies?
                        // (already done) either we reconnect individual socket at a timeout 
                        // todo - another solution would be to include in the ping (via base connection) which sockets are "busy" with large messages
                        //
                        m_zmqRequestResponseClient.BaseSocket.Key.Connect();
                        Ping(m_zmqRequestResponseClient.BaseSocket.Key);

                        //
                        // reset connection
                        //
                        foreach (var kvp in m_zmqRequestResponseClient.Sockets)
                        {
                            string strMessage = GetType().Name + " request-Response Connection timeout [" +
                                                kvp.Key.EndPointAddr + "] at [" +
                                                DateTime.Now + "]. Reconnecting...";
                            Console.WriteLine(strMessage);
                            Logger.Log(strMessage);
                            kvp.Key.Connect();
                            //Ping(kvp.Key);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private void CheckIndivudalSocketTimeout()
        {
            try
            {
                if (m_zmqRequestResponseClient.Sockets != null)
                {
                    foreach (KeyValuePair<ZmqReqRespClientSocketWrapper, SocketInfo> kvp in
                        m_zmqRequestResponseClient.Sockets)
                    {
                        double dblTimeMins = (Clock.LastTime - kvp.Key.LastRecvPingTime).TotalMinutes;
                        //
                        // leave timeout long since we are not sure if a long message is on its way.
                        // Remember that 0mq sends multipart messages, but all of these multipart messages come at the same time!
                        // A message which takes longer than INDIVIDUAL_SOCKET_TIMEOUT_MINS mins to send is too long for the infrastructure
                        //
                        if (dblTimeMins > INDIVIDUAL_SOCKET_TIMEOUT_MINS &&
                            kvp.Key.NumRequests > 0)
                        {
                            string strMessage = GetType().Name +
                                                "Individual socket timeout [" + dblTimeMins +
                                                " mins]. Request-Response Connection timeout [" +
                                                kvp.Key.EndPointAddr + "] at [" +
                                                DateTime.Now + "]. Total time [" + dblTimeMins + "]. Reconnecting...";
                            Console.WriteLine(strMessage);
                            Logger.Log(strMessage);
                            kvp.Key.Connect();
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        #endregion

    }
}