using System;
using System.Collections.Generic;

namespace HC.Core.DataStructures
{
    public class ListWrapper<T> : IDisposable
    {
        public List<T> m_list;

        public ListWrapper(List<T> list)
        {
            m_list = list;
        }

        public void Dispose()
        {
            if(m_list != null)
            {
                m_list.Clear();
            }
            m_list = null;
        }
    }
}
