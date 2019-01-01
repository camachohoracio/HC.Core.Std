using System;

namespace HC.Core.Comunication.RequestResponseBased.Server
{
    public class ResponseServerTaskWrapper : IDisposable
    {
        public ResponseServerTask ResponseServerTask { get; set; }
        
        public ResponseServerTaskWrapper(ResponseServerTask responseServerTask)
        {
            ResponseServerTask = responseServerTask;
        }

        public void Dispose()
        {
            ResponseServerTask = null;
        }
    }
}