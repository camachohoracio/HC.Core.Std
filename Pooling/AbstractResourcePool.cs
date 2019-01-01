#region

using System;
using HC.Core.Resources;

#endregion

namespace HC.Core.Pooling
{
    public abstract class AbstractResourcePool : IDisposable
    {
        #region Properties

        public int PoolCapacity
        {
            get
            {
                lock (m_lockObject)
                {
                    return m_resourcePool.Capacity;
                }
            }
            set
            {
                lock (m_lockObject)
                {
                    if (m_resourcePool != null)
                    {
                        m_resourcePool.Capacity = value;
                    }
                }
            }
        }

        public int PoolSize
        {
            get
            {
                lock (m_lockObject)
                {
                    if (m_resourcePool == null)
                    {
                        return 0;
                    }
                    return m_resourcePool.Size;
                }
            }
        }

        public bool AllowSerialisation
        {
            get
            {
                return m_blnAllowSerialisation;
            }
            set
            {
                m_blnAllowSerialisation = value;
            }
        }

        #endregion

        #region Members

        private static readonly object m_lockObject = new object();
        protected bool m_blnAllowSerialisation;
        protected ResourcePool m_resourcePool;
        private static readonly object m_lockCreateInstanceObject = new object();

        #endregion

        public AbstractResourcePool() { }

        public AbstractResourcePool(bool blnAllowSerialisation)
        {
            m_blnAllowSerialisation = blnAllowSerialisation;
        }

        #region IDisposable Members

        public void Dispose()
        {
            EventHandlerHelper.RemoveAllEventHandlers(this);
            m_resourcePool.Dispose();
        }

        #endregion

        protected void InitializeResourcePool(
            IResourceFactory resourceFactory,
            int intCapacity)
        {
            lock (m_lockCreateInstanceObject)
            {
                // create a resource pool if it doesn't exist existing pools 
                m_resourcePool = ResourcePool.GetInstance(
                    resourceFactory.GetType());

                if (m_resourcePool == null)
                {
                    m_resourcePool = ResourcePool.CreateInstance(
                        resourceFactory,
                        GetPoolName(),
                        m_blnAllowSerialisation,
                        intCapacity);
                }
            }
        }

        #region Abstract Methods

        public abstract IResource Reserve(
            IDataRequest dataRequest);

        public abstract void Release(
            IResource resource);

        public abstract string GetPoolName();

        public abstract bool ContainsResource(
            IDataRequest dataRequest);

        #endregion
    }
}


