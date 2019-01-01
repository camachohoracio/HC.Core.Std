#region

using System;
using System.Collections.Generic;
using HC.Core.Cache;
using HC.Core.DynamicCompilation;
using HC.Core.Threading;

#endregion

namespace HC.Core.Distributed.Worker
{
    public interface ITsCalcWorker : ICalcWorker, IDisposable
    {
        #region Properties

        List<ITsEvent> TsEvents { get; set; }
        CacheDictionary<string, List<ITsEvent>> Cache { get; set; }
        bool DoCache { get; set; }
        ASelfDescribingClass Params { get; set; }

        #endregion

        #region Members

        string GetResourceName();

        #endregion
    }
}
