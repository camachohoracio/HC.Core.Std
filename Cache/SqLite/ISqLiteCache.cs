#region

using System.Collections.Generic;
using HC.Core.Threading.ProducerConsumerQueues.Support;

#endregion

namespace HC.Core.Cache.SqLite
{
    public interface ISqLiteCache<T> : ISqLiteCacheBase
    {
        Dictionary<string, List<T>> LoadDataMap(string strQuery);
        List<T> LoadAllData();
        Dictionary<string, List<T>> LoadAllDataMap();
        List<T> LoadDataFromKeys(List<string> strKeys);
        List<T> LoadDataFromKey(string strKey);
        List<T> LoadDataFromWhere(string strWhere);
        int GetCountFromWhere(string strWhere);

        TaskWrapper Insert(
            string strKey,
            T obj);


        TaskWrapper Insert(
            string strKey,
            List<T> objs);

        List<TaskWrapper> Insert(Dictionary<string, List<T>> objs);
        void DropDefaultIndex();
        void CreateIndex(string strIndex);
        void ShrinkDb();
        void TrunkateTable(string strTableName);
        void DropTable(string strTableName);
        bool ContainsKey(string strKey);
        List<string> LoadAllKeys();
        void Delete(string strKey);
        void DeleteFromWhere(string strQuery);
        void Delete(List<string> strKeys);
        void Execute(string strQuery, 
            List<object[]> data);
        int Count { get; }

        string[] GetColNames();
        void Execute(string strQuery, List<object[]> data, bool blnLoadCols, out string[] cols);
        string GetTableName();
        TK ExecuteScalar<TK>(string strQuery);
        List<string> ContainsKeys(List<string> strKeys);
        Dictionary<string, List<T>> LoadDataMapFromWhere(string strQuery);
    }
}