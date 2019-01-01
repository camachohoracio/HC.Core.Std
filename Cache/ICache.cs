#region

using System.Collections.Generic;
using HC.Core.Resources;

#endregion

namespace HC.Core.Cache
{
    public interface ICache : IResource
    {
        #region Properties

        int Count { get; }
        bool CompressItems { get; set; }

        #endregion

        void Add(object oKey, object oValue);
        bool ContainsKey(object oKey);
        IEnumerator<KeyValuePair<object, object>> GetEnumerator();
        void Delete(object oKey);
        void Clear();
        object Get(object oKey);
        void Update(object oKey, object oValue);
    }
}



