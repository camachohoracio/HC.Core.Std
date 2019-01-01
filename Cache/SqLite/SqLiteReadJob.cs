using System;
using System.Collections.Generic;
using HC.Core.Threading.ProducerConsumerQueues.Support;

namespace HC.Core.Cache.SqLite
{
    public class SqLiteReadJob : IDisposable
    {
        public string Query { get; set; }
        public List<object[]> Data { get; set; }
        public string FileName{ get; set; }
        public IThreadedQueue<SqLiteReadJob> ReaderQueue { get; set; }
        public bool IsDisposed { get; private set; }
        public bool ExecuteRead { get; set; }
        public bool ExecuteScalar { get; set; }
        public ISqLiteCacheBase SqLiteCacheBase { get; set; }
        public bool LoadColNames { get; set; }
        public string[] ColNames { get; set; }

        public SqLiteReadJob()
        {
            Data = new List<object[]>();
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            IsDisposed = true;
            Query = null;
            if (Data != null &&
                Data.Count > 0)
            {
                Data.Clear();
            }
            Data = null;
            SqLiteCacheBase = null;
            FileName = null;
            ReaderQueue = null;
            ColNames = null;
        }
    }
}