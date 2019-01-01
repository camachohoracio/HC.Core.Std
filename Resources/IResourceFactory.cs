#region

using System;

#endregion

namespace HC.Core.Resources
{
    public interface IResourceFactory
    {
        string Name { get; set; }
        IResource Create(IDataRequest dataRequest);
        String[] Resources();
        bool Shared();
        bool MultipleInstances();
    }
}


