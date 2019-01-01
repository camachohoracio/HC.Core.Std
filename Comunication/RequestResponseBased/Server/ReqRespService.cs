#region

using System;
using System.Collections.Generic;
using HC.Core.Comunication.RequestResponseBased.Server.RequestHub;
//using HC.Core.Io.Serialization.FastSerialization.ServiceSerializer;
using HC.Core.Logging;

#endregion

namespace HC.Core.Comunication.RequestResponseBased.Server
{
    //[SerializerContractAttr]
    public class ReqRespService : IReqRespService
    {
        private static bool m_blnIsConnectred;
        private static readonly object m_connectionLock = new object();

        #region Members

        public static RequestTypeEvent[] Callbacks { get; private set; }

        #endregion

        #region Constructors

        static ReqRespService()
        {
            Connect();
        }

        public static void Connect()
        {
            if (m_blnIsConnectred)
            {
                return;
            }
            lock (m_connectionLock)
            {
                if (m_blnIsConnectred)
                {
                    return;
                }
                var values = Enum.GetNames(typeof(EnumRequestType));
                Callbacks = new RequestTypeEvent[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    Callbacks[i] = new RequestTypeEvent();
                }
                Callbacks[
                    (int)EnumRequestType.PingConnection].OnGetObjectList += OnPingConnection;
                m_blnIsConnectred = true;
            }
        }

        #endregion

        #region IReqRespService Members

        private static List<object> OnPingConnection(RequestDataMessage requestdatamessage)
        {
            return new List<object>();
        }

        public RequestDataMessage RequestDataOperation(RequestDataMessage transferMessage)
        {
            return InvokeRequestEventHandler(transferMessage);
        }

        public static RequestDataMessage InvokeRequestEventHandler(RequestDataMessage requestDataMessage)
        {
            try
            {
                requestDataMessage.Response = Callbacks[
                    (int) requestDataMessage.RequestType].InvokeOnGetObjectList(requestDataMessage);
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return requestDataMessage;
        }

        #endregion

    }
}


