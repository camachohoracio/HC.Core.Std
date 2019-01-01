namespace HC.Core.Comunication.RequestResponseBased.Client
{
    public class SocketInfo
    {
        public string DNS { get; set; }
        public int Port { get; set; }

        public string GetConnectionUrl()
        {
            // ZMQ does not support IPC yet!
            string strIp = NetworkHelper.GetIpAddr(DNS);
            if (NetworkHelper.CurrentIp.Equals(strIp))
            {
                //
                // generate in process comunication
                //
                return "tcp://"  + NetworkHelper.LOOP_BACK_IP + ":" + Port;
            }
            // note: is tcp the fastest protocol?
            return "tcp://" + strIp + ":" + Port;
        }

        public override string ToString()
        {
            return GetConnectionUrl();
        }
    }
}
