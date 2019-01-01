using System;
using System.Threading;
using HC.Core.Logging;
using HC.Core.Threading;

namespace HC.Core
{
    public static class GCMonitor
    {
        public static void Start()
        {
            var worker = new ThreadWorker();
            worker.OnExecute += () =>
                {
                    try
                    {
                        Thread.Sleep(10*60*1000);
                        GC.Collect();
                        Console.WriteLine("GC collectd[" + DateTime.Now + "]");
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                        Thread.Sleep(5000);
                    }
                };
            worker.Work();
        }
    }
}
