#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HC.Core.Exceptions;
using HC.Core.Logging;

#endregion

namespace HC.Core.Threading.Buffer
{
    public class EfficientMemoryBuffer<TK,TV> : IDictionary<TK,TV>, IDisposable
    {
        private readonly double m_dblBufferLifeMins;

        public delegate void ItemRemovedDel(TV removedItem);

        public delegate void ItemLoadedDel(TK key, ref TV item);

        public event ItemLoadedDel OnLoadedItem;

        public event ItemRemovedDel OnItemRemoved;

        #region Properties

        public long Capacity { get; private set; }
        public bool IsReadOnly { get { return false; } }
        
        public ICollection<TK> Keys
        {
            get
            {
                lock (m_lockObj)
                {
                    return m_data.Keys.ToList();
                }
            }
        } 

        public ICollection<TV> Values
        {
            get
            {
                lock (m_lockObj)
                {
                    return m_data.Values.ToList();
                }
            }
        }

        public int Count
        {
            get
            {
                lock (m_lockObj)
                {
                    return m_data.Count;
                }
            }
        }

        #endregion

        #region Members

        private readonly Dictionary<TK, TV> m_data;
        private readonly Dictionary<BufferItem, TK> m_ageMap;
        private readonly Dictionary<TK, BufferItem> m_keyToAge;
        private readonly List<BufferItem> m_ages;
        private long m_lngAge;
        private readonly object m_lockObj = new object();
        private readonly int m_lngWaitMills;

        #endregion

        #region Constructors

        public EfficientMemoryBuffer(
            int intCapacity) : this(intCapacity,0){}

        public EfficientMemoryBuffer(
            int intCapacity,
            double dblBufferLifeMins)
        {
            try
            {
                m_dblBufferLifeMins = dblBufferLifeMins;
                Capacity = intCapacity;
                m_data = new Dictionary<TK, TV>(intCapacity + 2);
                m_ages = new List<BufferItem>(intCapacity + 2);
                m_ageMap = new Dictionary<BufferItem, TK>(intCapacity + 2);
                m_keyToAge = new Dictionary<TK, BufferItem>(intCapacity + 2);
                if (dblBufferLifeMins > 0)
                {
                    m_lngWaitMills = (int) Math.Min(60000, dblBufferLifeMins*60000);
                    ThreadWorker.StartTaskAsync(
                        () =>
                        {
                            while (true)
                            {
                                try
                                {
                                    Thread.Sleep(m_lngWaitMills);
                                    //
                                    // look for and remove old items
                                    //
                                    KeyValuePair<TK, BufferItem>[] dataArr;
                                    lock (m_lockObj)
                                    {
                                        dataArr = m_keyToAge.ToArray();
                                    }
                                    DateTime now = DateTime.Now;
                                    for (int i = 0; i < dataArr.Length; i++)
                                    {
                                        if ((now - dataArr[i].Value.Date).TotalMinutes >
                                            m_dblBufferLifeMins)
                                        {
                                            Remove(dataArr[i].Key);

                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.Log(ex);
                                }
                            }
                        });
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        #endregion

        public bool ContainsKey(TK key)
        {
            lock (m_lockObj)
            {
                return m_data.ContainsKey(key);
            }
        }

        public void Add(TK key, TV value)
        {
            try
            {
                lock (m_lockObj)
                {
                    //
                    // used for safety, avoid adding the same key twice
                    // otherwise this will cause problems with the age list and age map
                    //
                    RemoveUnsafe(key);

                    //
                    // add item
                    //
                    m_lngAge++;
                    m_data[key] = value;
                    var ageItem = GetAgeItem();
                    m_ageMap[ageItem] = key;
                    m_keyToAge[key] = ageItem;
                    m_ages.Add(ageItem);

                    //
                    // check capacity
                    //
                    if (m_data.Count > Capacity)
                    {
                        var lngAgeToRemove = m_ages[0];
                        m_ages.RemoveAt(0);

                        TK keyToRemove = m_ageMap[lngAgeToRemove];
                        if (!m_ageMap.Remove(lngAgeToRemove))
                        {
                            throw new HCException("Item not found");
                        }
                        if (!m_keyToAge.Remove(keyToRemove))
                        {
                            throw new HCException("Item not found");
                        }
                        TV oldValue;
                        m_data.TryGetValue(keyToRemove, out oldValue);

                        if (!m_data.Remove(keyToRemove))
                        {
                            throw new HCException("Item not found");
                        }
                        if (OnItemRemoved != null)
                        {
                            OnItemRemoved(oldValue);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private BufferItem GetAgeItem()
        {
            return new BufferItem
                       {
                           Age = m_lngAge,
                           Date = DateTime.Now
                       };
        }

        public bool Remove(TK key)
        {
            lock (m_lockObj)
            {
                return RemoveUnsafe(key);
            }
        }

        private bool RemoveUnsafe(TK key)
        {
            bool blnRemove = false;
            BufferItem lngAge;
            if(m_keyToAge.TryGetValue(key, out lngAge))
            {
                blnRemove = true;
                m_ageMap.Remove(lngAge);
                int intIndex = m_ages.BinarySearch(lngAge);
                m_ages.RemoveAt(intIndex);
                m_data.Remove(key);
                m_keyToAge.Remove(key);
            }
            return blnRemove;
        }

        public TV Get(TK key)
        {
            TV value;
            TryGetValue(key, out value);
            return value;
        }

        public bool TryGetValue(TK key, out TV value)
        {
            value = default(TV);
            try
            {
                lock (m_lockObj)
                {
                    if (key == null)
                    {
                        return false;
                    }
                    if (m_data.TryGetValue(key, out value))
                    {
                        m_lngAge++;
                        var ageItem = GetAgeItem();
                        var lngOldAge = m_keyToAge[key];
                        m_keyToAge.Remove(key);
                        m_keyToAge[key] = ageItem;

                        if (!m_ageMap.Remove(lngOldAge))
                        {
                            throw new HCException("Age not found");
                        }
                        m_ageMap[ageItem] = key;
                        int intIndex = m_ages.BinarySearch(lngOldAge);
                        m_ages.RemoveAt(intIndex);
                        m_ages.Add(ageItem);

                        if (OnLoadedItem != null)
                        {
                            OnLoadedItem(key, ref value);
                        }
                        return true;
                    }
                    return false;
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return false;
        }

        public TV this[TK key]
        {
            get { return Get(key); }
            set { Add(key, value); }
        }

        public IEnumerator<KeyValuePair<TK,TV>> GetEnumerator()
        {
            lock (m_lockObj)
            {
                var data = m_data;
                return data.GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<TK,TV> item)
        {
            Add(item.Key,item.Value);
        }

        public void Clear()
        {
            lock (m_lockObj)
            {
                m_lngAge = 0;
                m_ageMap.Clear();
                m_ages.Clear();
                m_data.Clear();
                m_keyToAge.Clear();
            }
        }

        public bool Contains(KeyValuePair<TK,TV> item)
        {
            return ContainsKey(item.Key);
        }

        public void CopyTo(KeyValuePair<TK,TV>[] array, int intArrayIndex)
        {
            lock (m_lockObj)
            {
                var dataArr = m_data.ToArray();
                int j = 0;
                for (int i = intArrayIndex; i < Count; i++)
                {
                    array[j] = dataArr[i];
                    j++;
                }
            }
        }

        public bool Remove(KeyValuePair<TK,TV> item)
        {
            return Remove(item.Key);
        }

        public static void Test()
        {
            var map = new EfficientMemoryBuffer<string, string>(10);
            string strguid1 = -1 + "_" + Guid.NewGuid().ToString();
            string strGuidBase = strguid1;
            Parallel.For(0, 10000, delegate(int i)
                                       {
                                           string strGuid2 = i + "_" + Guid.NewGuid().ToString();
                                           map[strguid1] = strGuid2;
                                           if(!map.ContainsKey(strguid1))
                                           {
                                               Console.WriteLine("does not contain key: " + strguid1);
                                           }
                                           else
                                           {
                                               Console.WriteLine("Contains key: " + strguid1);
                                           }

                                           int intCount = map.Count;
                                           if(intCount > 10)
                                           {
                                               throw new HCException("Invalid size: " + intCount);
                                           }
                                           strguid1 = i + "_" + Guid.NewGuid().ToString();
                                           Console.WriteLine("Size = " + map.Count());
                                       });
            if(map.ContainsKey(strGuidBase))
            {
                throw new HCException("Initial key was found");
            }
        }

        public void Dispose()
        {
            m_data.Clear();
            m_ageMap.Clear();
            m_keyToAge.Clear();
            m_ages.Clear();
        }
    }
}



