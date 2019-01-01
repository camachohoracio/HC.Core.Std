#region

using System;

#endregion

namespace HC.Core.Resources
{
    public interface IResourcePool : IDisposable
    {
        void Close();
        void Release(IResource resource);
        IResource Reserve(Object owner, IDataRequest name);
    }
}


