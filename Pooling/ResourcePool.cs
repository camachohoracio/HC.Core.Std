#region

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using HC.Core.Cache;
using HC.Core.Exceptions;
using HC.Core.Logging;
using HC.Core.Resources;

#endregion

namespace HC.Core.Pooling
{
    [Serializable]
    public class ResourcePool : IResourcePool
    {
        #region Members

        private static readonly ConcurrentDictionary<Type, ResourcePool> m_resourceMap =
            new ConcurrentDictionary<Type, ResourcePool>();
        private readonly bool m_blnAllowSerialisation;
        private readonly CacheDictionary<string, IResource> m_cacheDictionary;
        private readonly object m_lockObjectGetStack = new object();
        private double m_dblMemorySize;
        private List<IResource> m_instancelist;
        private Hashtable m_readylist;
        private readonly object m_lockObject = new object();

        #endregion

        #region Properties

        public IResourceFactory Factory { get; set; }
        public int Capacity { get; set; }
        public double CapacityMb { get; set; }

        public int Size
        {
            get { return m_instancelist.Count; }
        }

        public CacheDictionary<string, IResource> CacheDictionary
        {
            get { return m_cacheDictionary; }
        }

        #endregion

        #region Constructor

        public ResourcePool(
            string strPoolName,
            bool blnAllowSerialisation,
            int intCapacity)
        {
            m_blnAllowSerialisation = blnAllowSerialisation;
            Capacity = intCapacity;

            if (blnAllowSerialisation)
            {
                //
                // initialize cache dictionary
                //
                m_cacheDictionary =
                    new CacheDictionary<string, IResource>(strPoolName,
                        typeof(StdCache).Name,
                        Config.GetDefaultCacheDataPath(),
                        strPoolName, 
                        false);
            }
        }

        #endregion

        #region IResourcePool Members

        public bool ContainsResource(
            IDataRequest dataRequest)
        {
            IDataRequest request =
                (Factory.Shared()) ? CoreConstants.SHARED :
                dataRequest;
            ConcurrentStack<object> stack;
            if (request == null)
            {
                return false;
            }

            //
            // only one thread
            //
            lock (m_lockObjectGetStack)
            {
                stack = (ConcurrentStack<object>) m_readylist[request];
                if (stack == null)
                {
                    return false;
                }
            }
            if (stack.Count == 0)
            {
                return false;
            }
            return true;
        }

        public virtual IResource Reserve(
            Object owner,
            IDataRequest dataRequest)
        {
            IResource resource;

            ConcurrentStack<object> stack = GetStack(dataRequest);
            ValdiateStack(dataRequest, stack);
            lock (stack)
            {
                int intStackCount = stack.Count;


                if (intStackCount == 0)
                {
                    if (!Factory.MultipleInstances())
                    {
                        lock (stack)
                        {
                            if ((stack.Count == 0))
                            {
                                //Debugger.Break();
                                throw new HCException(
                                    "Error. Multiple instances of this object not allowed.");
                            }
                        }
                    }
                    //
                    //currently all previously instantiated resources
                    //of this type are in use, so create a new one by 
                    //calling subclass's version of this method
                    //
                    resource = LoadResource(
                        dataRequest,
                        stack);
                    resource.DataRequest = dataRequest;
                    AddInstanceList(resource);
                    //AddSize(resource);
                    Logger.Log(
                        "Factory : " +
                        Factory.Name +
                        ", Creating a new '" +
                        dataRequest + "'");
                }
                else
                {
                    //
                    //pop a previously instantiated resource from the stack
                    //
                    lock (stack)
                    {
                        resource = PopFromStack(stack);
                    }
                    if (Factory.Shared())
                    {
                        resource.DataRequest = dataRequest;
                    }
                }


                if (resource == null)
                {
                    throw new HCException(
                        "Error creating or reserving the '" +
                        dataRequest +
                        "' resource");
                }


                resource.Owner = owner;

                //
                // Set usage
                //
                resource.TimeUsed = DateTime.Now;

                //
                // if the pool is full, then release the oldest item
                //
                lock (m_lockObject)
                {
                    if (m_instancelist.Count > Capacity)
                    {
                        RemoveOldestItem();
                    }

                    if (CheckPoolSizeMb())
                    {
                        RemoveOldestItem();
                    }
                }
                Logger.Log(
                    owner +
                    " Reserved '" +
                    dataRequest +
                    "' from pool, now " +
                    stack.Count +
                    " remain");
                return resource;
            }
        }

        public virtual void Close()
        {
            if (m_instancelist != null)
            {
                int size = m_instancelist.Count;
                for (int i = 0; i < size; i++)
                {
                    try
                    {
                        IResource resource = m_instancelist[i];
                        resource.Close();
                        Logger.Log(
                            "Factory : " + 
                            Factory.Name + 
                            ", Closed '" + 
                            resource.DataRequest + 
                            "'");
                    }
                    catch (Exception e)
                    {
                        Logger.Log(e);
                    }
                }
            }
        }

        public virtual void Release(IResource resource)
        {
            var stack =
                GetStack(resource.DataRequest);

            if (stack.Contains(resource))
            {
                string strMessage =
                    resource.DataRequest +
                    " already exists in pool";
                Logger.Log(
                    strMessage);
                return;
                //Debugger.Break();
                //throw new HCException(
                //    resource.DataRequest + 
                //    " already exists in pool");
            }
            PushTostack(
                resource,
                stack);
        }

        #endregion

        public void Replace(
            IResource resource)
        {
            lock (this)
            {
                if (resource.HasChanged)
                {
                    IDataRequest strName = resource.DataRequest;
                    ConcurrentStack<object> stack = GetStack(strName);

                    if (stack == null)
                    {
                        stack = new ConcurrentStack<object>();
                        AddStackToReadyList(
                            strName,
                            stack);
                        AddInstanceList(resource);
                        //AddSize(resource);
                    }
                    else
                    {
                        //
                        // iterate each instance and replace
                        //
                        for (int i = 0; i < m_instancelist.Count; i++)
                        {
                            IResource currentResource = m_instancelist[i];
                            if (currentResource.DataRequest.Equals(strName) &&
                                currentResource.TimeUsed == resource.TimeUsed)
                            {
                                m_instancelist[i] = resource;
                                break;
                            }
                        }
                        //
                        // iterate each element in the stack and replace
                        //
                        for (int i = 0; i < stack.Count; i++)
                        {
                            IResource currentResource =
                                PopFromStack(stack);

                            if (currentResource.DataRequest.Equals(strName) &&
                                currentResource.TimeUsed == resource.TimeUsed)
                            {
                                PushTostack(
                                    resource,
                                    stack);
                                break;
                            }
                            PushTostack(
                                currentResource,
                                stack);
                        }
                    }
                    resource.HasChanged = false;
                }
            }
        }

        private void AddInstanceList(IResource resource)
        {
            lock (m_instancelist)
            {
                m_instancelist.Add(resource);
            }
        }

        private IResource LoadResource(
            IDataRequest strName,
            ConcurrentStack<object> stack)
        {
            IResource resource = null;
            if (m_blnAllowSerialisation)
            {
                //
                // try to get value from Berkeley database
                //
                if (CacheDictionary.ContainsKey(
                    strName.Name))
                {
                    resource = CacheDictionary[strName.Name];
                }
            }

            if (resource == null)
            {
                resource = Factory.Create(strName);

                if (m_blnAllowSerialisation)
                {
                    CacheDictionary.Add(
                        strName.Name,
                        resource);

                    if (!CacheDictionary.ContainsKey(
                        strName.Name))
                    {
                        throw new HCException("Element not loaded into db");
                    }
                }
            }


            if (!Factory.MultipleInstances())
            {
                PushTostack(resource,
                            stack);
            }

            return resource;
        }

        private void AddStackToReadyList(
            IDataRequest strName,
            ConcurrentStack<object> stack)
        {
            lock (m_readylist)
            {
                m_readylist[Factory.Shared() ? Core.CoreConstants.SHARED : strName] = stack;
            }
        }

        private static void PushTostack(
            IResource resource,
            ConcurrentStack<object> stack)
        {
            lock (stack)
            {
                stack.Push(resource);
            }
        }

        private IResource PopFromStack(ConcurrentStack<object> stack)
        {
            lock (stack)
            {
                object obj;
                stack.TryPop(out obj);
                IResource resource = (IResource) obj;

                //
                // put back the resource in case multiple instances are not allowed
                //
                if (!Factory.MultipleInstances())
                {
                    PushTostack(resource, stack);
                }

                return resource;
            }
        }

        private ConcurrentStack<object> GetStack(
            IDataRequest strName)
        {
            IDataRequest strStackName =
                (Factory.Shared()) ? CoreConstants.SHARED : strName;
            ConcurrentStack<object> stack;
            //
            // only one thread
            //
            lock (m_lockObjectGetStack)
            {
                stack = (ConcurrentStack<object>)m_readylist[strStackName];
                if (stack == null)
                {
                    //
                    // a stack to store these types of resources has not been setup yet.
                    // So, set up the stack and instantiate the first such resource
                    //
                    stack = new ConcurrentStack<object>();
                    AddStackToReadyList(strName, stack);
                }
            }
            return stack;
        }

        private void ValdiateStack(
            IDataRequest dataRequest,
            ConcurrentStack<object> stack)
        {
            lock (stack)
            {
                if ((stack.Count == 0))
                {
                    IResource resource = LoadResource(
                        dataRequest,
                        stack);
                    resource.DataRequest = dataRequest;
                    AddInstanceList(resource);
                    //AddSize(resource);
                }
            }
        }


        private void RemoveOldestItem()
        {
            lock (m_instancelist)
            {
                IEnumerable<IResource> q =
                    from n in m_instancelist
                    orderby n.TimeUsed
                    select n;

                IResource oldestItem = q.First();

                Remove(oldestItem);
            }
        }

        //private void AddSize(IResource resource)
        //{
        //    //try
        //    //{
        //    //    BinaryFormatter bf = new BinaryFormatter();
        //    //    MemoryStream fs = new MemoryStream();
        //    //    bf.Serialize(fs, resource);
        //    //    m_dblMemorySize += fs.Length/1000000.0;
        //    //}
        //    //catch(Exception e)
        //    //{
        //    //    throw;
        //    //}
        //}

        //private void RemoveSize(IResource resource)
        //{
        //    //try
        //    //{
        //    //    BinaryFormatter bf = new BinaryFormatter();
        //    //    MemoryStream fs = new MemoryStream();
        //    //    bf.Serialize(fs, resource);
        //    //    m_dblMemorySize -= fs.Length/1000000.0;
        //    //}
        //    //catch
        //    //{
        //    //}
        //}

        private bool CheckPoolSizeMb()
        {
            return (m_dblMemorySize > 500);
        }

        public static ResourcePool CreateInstance(
            IResourceFactory factory,
            string strPoolName,
            bool blnAllowSerialisation,
            int intCapacity)
        {
            ResourcePool ownInstance = null;

            if (!m_resourceMap.ContainsKey(factory.GetType()))
            {
                ownInstance = new ResourcePool(
                    strPoolName,
                    blnAllowSerialisation,
                    intCapacity);
                ownInstance.Initialize(factory);
                m_resourceMap[factory.GetType()] = ownInstance;
            }
            return ownInstance;
        }

        public static ResourcePool GetInstance(Type type)
        {
            ResourcePool instance = null;
            if (m_resourceMap.ContainsKey(type))
            {
                instance = m_resourceMap[type];
            }
            return instance;
        }

        protected internal virtual void Initialize(IResourceFactory factory)
        {
            m_readylist = Hashtable.Synchronized(new Hashtable(
                new PoolDataComparer()));
            m_instancelist = new List<IResource>();
            Factory = factory;
        }

        public virtual void Remove(IResource resource)
        {
            lock (m_readylist)
            {
                m_readylist.Remove(resource.DataRequest);
            }
            lock (m_instancelist)
            {
                m_instancelist.Remove(resource);
            }
            //RemoveSize(resource);
        }

        #region Dispose

        public void Dispose()
        {
            Close();
        }

        #endregion
    }
}


