#region

using System;
using System.Collections;
using System.Collections.Generic;
using HC.Core.Cache.DataStructure;
using HC.Core.Exceptions;
using HC.Core.Helpers;
using HC.Core.Logging;
using HC.Core.Resources;

//using HC.Core.Caches.Pool;

#endregion

namespace HC.Core.Cache
{
    public class CacheDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        #region Properties

        public bool CompressItems
        {
            get { return m_cache.CompressItems; }
            set { m_cache.CompressItems = value; }
        }

        #endregion

        #region Members

        private readonly ICache m_cache;

        #endregion

        #region Constructor

        /// <summary>
        /// Used for serialization
        /// </summary>
        public CacheDictionary(){}

        public CacheDictionary(
            string strDbName)
            : this(
                strDbName,
                false)
        {
        }

        public CacheDictionary(
            string strDbName,
            bool blnCompressItems) :
                this(strDbName,
                     Core.Config.GetDefaultCacheDataPath() + @"\" + strDbName,
                     blnCompressItems)
        {
        }

        // Construct the SimpleDictionary with the desired number of items.
        // The number of items cannot change for the life time of this SimpleDictionary.
        public CacheDictionary(
            string strDbName,
            string strDbPath,
            bool blnCompressItems)
            : this(Core.Config.GetDbSerializerName(),
                   strDbName,
                   strDbPath,
                   string.Empty, blnCompressItems)
        {
        }

        public CacheDictionary(
            string strCacheName,
            string strDbName,
            string strCachePath,
            string strTableName,
            bool blnCompressItems)
        {
            var dbDataRequest =
                new DbDataRequest(
                    strCacheName,
                    strDbName,
                    strTableName,
                    strCachePath,
                    blnCompressItems);
            m_cache = new StdCache(
                dbDataRequest.DbPath,
                dbDataRequest.Compress);
        }

        #endregion

        #region IDictionary<TKey,TValue> Members

        public bool IsReadOnly
        {
            get { return false; }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            m_cache.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return m_cache.ContainsKey(item.Key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }

        public bool ContainsKey(TKey key)
        {
            return m_cache.ContainsKey(key);
        }

        public void Add(TKey key, TValue value)
        {
            Add(key,
                value,
                false);
        }

        public bool Remove(TKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key is null");
            }
            m_cache.Delete(key);
            return true;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            var blnContainsKey = ContainsKey(key);
            value = default(TValue);

            if (blnContainsKey)
            {
                value = this[key];
            }

            return blnContainsKey;
        }

        public TValue this[TKey key]
        {
            get
            {
                var value = default(TValue);
                try
                {
                    var strMessage = "Desearilizing object. " +
                                     key + "...";
                    Logger.Log(strMessage);
                    value = (TValue) m_cache.Get(key);
                }
                catch (Exception e)
                {
                    var strMessage = "Error while desearilizing object. " +
                                     e.Message +
                                     " object will be deleted.";
                    Logger.Log(strMessage);
                    PrintToScreen.WriteLine(strMessage);
                    PrintToScreen.WriteLine(strMessage);
                    m_cache.Delete(key);
                }
                return value;
            }
            set { m_cache.Update(key, value); }
        }

        ICollection<TValue> IDictionary<TKey, TValue>.Values
        {
            get { return new GenericKeyCollectionCache<TValue>(m_cache); }
        }

        ICollection<TKey> IDictionary<TKey, TValue>.Keys
        {
            get { return new GenericValueCollectionCache<TKey>(m_cache); }
        }


        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return new GenericDictionaryEnumeratorCache<TKey, TValue>(
                m_cache);
        }

        public int Count
        {
            get { return m_cache.Count; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            // Construct and return an enumerator.
            return ((IDictionary) this).GetEnumerator();
        }

        #endregion

        public void Add(
            TKey key,
            TValue value,
            bool blnCommit)
        {
            Logger.Log("Adding cache key: " +
                       key);
            m_cache.Add(
                key,
                value);
            if (blnCommit)
            {
                if (!ContainsKey(key))
                {
                    throw new HCException("Key not found.");
                }
            }
        }
    }
}



