using System;
using System.Collections.Generic;
using System.Threading;
using HC.Core.Logging;

namespace HC.Core.Comunication.RequestResponseBased.Server.RequestHub
{
    public class RequestTypeEvent
    {
        public event GetObjectListDel OnGetObjectList;

        public List<object> InvokeOnGetObjectList(
            RequestDataMessage requestDataMessage)
        {
            try
            {
                while (OnGetObjectList == null)
                {
                    string strMessage = "Event handler not found [" +
                                        requestDataMessage.RequestType + "]";
                    Console.WriteLine(strMessage);
                    Logger.Log(strMessage);
                    Thread.Sleep(1000);
                }
                return OnGetObjectList(requestDataMessage);
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return new List<object>();
        }
    }
}



