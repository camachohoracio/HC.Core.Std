using System.Collections.Generic;
using HC.Core.Resources;

namespace HC.Core.Cache.SqLite
{
    public class SqliteObjectPool : PooledObject
    {
        private const int DEFAULT_POOL_SIZE = 500000;

        public int ObjectPoolSize { get { return Buffer.Count; }  }
        public List<object[]> Buffer { get; set; } 

        public SqliteObjectPool()
        {
            Buffer = new List<object[]>();
            IncreasePool();
        }

        public void IncreasePool()
        {
            for (int i = 0; i < DEFAULT_POOL_SIZE; i++)
            {
                Buffer.Add(null);
            }
        }

        protected override void OnReleaseResources()
        {
            // Override if the resource needs to be manually cleaned before the memory is reclaimed
        }
        protected override void OnResetState()
        {
            // Override if the resource needs resetting before it is getting back into the pool
        }    
    }
}
