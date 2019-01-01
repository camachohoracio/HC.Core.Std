#region

using System;

#endregion

namespace HC.Core.Resources
{
    public interface IResource : IDisposable
    {
        #region Properties

        IDataRequest DataRequest { get; set; }
        DateTime TimeUsed { get; set; }
        Object Owner { get; set; }

        /// <summary>
        /// Set true if the resource has changed and it will need to be stored again
        /// </summary>
        bool HasChanged { get; set; }

        #endregion

        #region Methods

        void Close();

        #endregion
    }
}


