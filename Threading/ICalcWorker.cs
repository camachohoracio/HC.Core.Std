#region

using System;
using HC.Core.Io.KnownObjects.KnownTypes;

#endregion

namespace HC.Core.Threading
{
    [IsAKnownTypeAttr]
    public interface ICalcWorker : IDisposable
    {
        #region Properties

        string Resource { get; set; }

        #endregion

        #region Members

        void Work();

        #endregion

    }
}



