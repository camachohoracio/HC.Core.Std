using System.Collections.Concurrent;

namespace HC.Core.Comunication.RequestResponseBased.Client.ZmqSocketWrapper
{
    public class RequestJob
    {
        public Server.RequestHub.RequestDataMessage RequestDataMessage { get; set; }

        public string RequestId { get; set; }

        public object SocketLock { get; set; }

        public ConcurrentDictionary<string, Server.RequestHub.RequestDataMessage> RequestMap { get; set; }

        public ZMQ.Socket Socket { get; set; }

        public ZmqReqRespClientSocketWrapper SocketWrapper { get; set; }

        public byte[] WhoIs { get; set; }

        public System.Threading.ReaderWriterLock Rwl { get; set; }
    }
}
