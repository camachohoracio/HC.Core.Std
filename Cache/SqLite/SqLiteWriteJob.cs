#region

using System;
using System.Collections.Generic;
using HC.Core.Threading.ProducerConsumerQueues;
using HC.Core.Threading.ProducerConsumerQueues.Support;

#endregion

namespace HC.Core.Cache.SqLite
{
    public class SqLiteWriteJob : IDisposable
    {
        #region Propeties

        public ISqLiteCacheBase DbConn { get; set; }
        public string TableName { get; set; }
        public List<KeyValuePair<string, object>> ImportList { get; set; }
        public bool IsBLob { get; set; }
        public string FileName { get; set; }
        public EfficientWorkerManager<SqLiteWriteJob> WriterQueue { get; set; }
        public object LockObj { get; set; }
        public bool IsConsumed { get; set; }
        public TaskWrapper TaskWrapper { get; set; }
        public string Query { get; set; }
        public bool IsWriteTask { get; set; }
        public bool IsDisposed { get; private set; }

        #endregion

        public SqLiteWriteJob() 
        {
            ImportList = new List<KeyValuePair<string, object>>();
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            IsDisposed = true;
            DbConn = null;
            TableName = null;
            if (ImportList != null)
            {
                ImportList.Clear();
            }
            ImportList = null;
            FileName = null;
            WriterQueue = null;
            LockObj = null;
            TaskWrapper = null;
        }

        public bool UseCompression { get; set; }
    }
}