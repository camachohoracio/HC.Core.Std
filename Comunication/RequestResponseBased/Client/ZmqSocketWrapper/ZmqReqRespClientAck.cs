using System.Collections.Concurrent;
using HC.Core.Comunication.TopicBased.Contracts;

namespace HC.Core.Comunication.RequestResponseBased.Client.ZmqSocketWrapper
{
    public class ZmqReqRespClientAck
    {
        private readonly ConcurrentDictionary<string, object> m_jobsDone;

        public ZmqReqRespClientAck(ZmqReqRespClientSocketWrapper zmqReqRespClientSocketWrapper)
        {
            m_jobsDone = new ConcurrentDictionary<string, object>();
            //TopicSubscriberCache.GetSubscriber(zmqReqRespClientSocketWrapper.EndPointAddr.DNS).Subscribe(
            //    EnumReqResp.ServerToClientReqRespAck.ToString(),
            //    OnServerToClientReqRespAck);
        }

        private void OnServerToClientReqRespAck(TopicMessage topicmessage)
        {
            var strJobId = (string)topicmessage.EventData;
            if(m_jobsDone.ContainsKey(strJobId))
            {
                SendJobAck(strJobId);
            }
        }

        public void SendJobAck(string strJobId)
        {
        }
    }
}
