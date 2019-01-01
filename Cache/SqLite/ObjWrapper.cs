using System;

namespace HC.Core.Cache.SqLite
{
    [Serializable]
    public class ObjWrapper : IDisposable
    {
        public ObjWrapper(object obj)
        {
            Obj = obj;
        }
        public ObjWrapper()
        {
            Obj = null;
        }

        public void Dispose()
        {
            Obj = null;
        }

        public object Obj { get; set; }
    }
}
