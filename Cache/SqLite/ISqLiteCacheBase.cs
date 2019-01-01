using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Threading;

namespace HC.Core.Cache.SqLite
{
    public interface ISqLiteCacheBase : IDisposable
    {
        bool UseCompression { get; set; }
        string FileName { get; }
        SQLiteConnection DbConn { get; }
        bool IsDisposed { get; }
        ReaderWriterLock DisposeLock { get; }
        KeyValuePair<string, DbType>[] ColNameToTypeArr { get; set; }
        object[][] GetImportArrBLob(List<KeyValuePair<string, object>> importList, bool blnUseCompression);
        object[][] GetImportArr(List<KeyValuePair<string, object>> importList, bool blnUseCompression);

        void GetColNameToTypeArr(string p);

        ISqLiteCacheBase GetCacheWrapperBase();
    }
}



