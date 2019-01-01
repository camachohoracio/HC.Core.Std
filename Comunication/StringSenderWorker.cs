using System;

namespace HC.Core.Comunication
{
    public class StringSenderWorker : IDisposable
    {
        public void Dispose()
        {
            Tree = null;
            Key = null;
            Obj = null;
        }

        public string Tree { get; set; }

        public string Key { get; set; }

        public object Obj { get; set; }

        public bool IsChart { get; set; }
    }
}