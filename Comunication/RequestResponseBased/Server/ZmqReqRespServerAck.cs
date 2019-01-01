using HC.Core.Comunication.RequestResponseBased.Server.RequestHub;
using ZMQ;

namespace HC.Core.Comunication.RequestResponseBased.Server
{
    public class ZmqReqRespServerAck
    {
        //public ConcurrentDictionary<string, RequestJob> JobsToAck { get; private set; }
        //public ConcurrentDictionary<string, object> JobsDone { get; private set; }

        private const int WAIT_MILLS = 10;
        private const int TOPIC_CONFIRM_MILLS = 5000;

        public ZmqReqRespServerAck(string strServerName)
        {
            //JobsToAck = new ConcurrentDictionary<string, RequestJob>();
            //JobsDone = new ConcurrentDictionary<string, object>();
            //TopicSubscriberCache.GetSubscriber(strServerName).Subscribe(
            //    EnumReqResp.ClientToServerReqRespAck.ToString(),
            //    OnClientToServerReqRespAck);
        }

        //public void ResendJob(string strRequestId)
        //{
        //    RequestJob jobItem;
        //    if(JobsToAck.TryGetValue(strRequestId, out jobItem) &&
        //        jobItem != null)
        //    {
        //        ZmqReqRespServer.SendResponse(
        //            jobItem.Socket,
        //            jobItem.RequestId,
        //            jobItem.RequestDataMessage,
        //            jobItem.SocketLock,
        //            jobItem.WhoIs);
        //        string strMessage = GetType().Name + " resent job [" + strRequestId +
        //                            "]";
        //        Console.WriteLine(strMessage);
        //        Logger.Log(strMessage);
        //    }
        //}

        //private void OnClientToServerReqRespAck(TopicMessage topicmessage)
        //{
        //    //
        //    // client received our result
        //    //
        //    var strJobId = (string)topicmessage.EventData;
        //    RequestJob dummy;
        //    JobsToAck.TryRemove(strJobId, out dummy);
        //    JobsDone[strJobId] = null;
        //}

        public void RequestAck(
            Socket socket,
            string strRequestId,
            RequestDataMessage response,
            object socketLock,
            byte[] bytesWhoIs)
        {
            ZmqReqRespServer.SendResponse(
                socket,
                strRequestId,
                response,
                socketLock,
                bytesWhoIs);

            //var requestJob = new RequestJob
            //                     {
            //                         Socket = socket,
            //                         RequestId = strRequestId,
            //                         RequestDataMessage = response,
            //                         SocketLock = socketLock,
            //                         WhoIs = bytesWhoIs
            //                     };

            //JobsToAck[strRequestId] = requestJob;

            //while(TopicPublisher.GetPublisher == null)
            //{
            //    const string strMessage = "Topic pulisher is not yet connected...";
            //    Console.WriteLine(strMessage);
            //    Logger.Log(strMessage);
            //}

            //TopicPublisher.GetPublisher.SendMessage(
            //    strRequestId,
            //    EnumReqResp.ServerToClientReqRespAck.ToString(),
            //    true);

            ////
            //// wait for client to ack the response
            ////
            //int intTotalMills = 0;
            //while (JobsToAck.ContainsKey(strRequestId))
            //{
            //    Thread.Sleep(WAIT_MILLS);
            //    intTotalMills += WAIT_MILLS;
            //    if(intTotalMills > TOPIC_CONFIRM_MILLS)
            //    {
            //        intTotalMills = 0;
            //        TopicPublisher.GetPublisher.SendMessage(
            //            strRequestId,
            //            EnumReqResp.ServerToClientReqRespAck.ToString(),
            //            true);
            //    }
            //}
        }
    }
}