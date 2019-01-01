using System;
using HC.Core.Threading;
using HC.Core.Logging;
using System.Threading;

namespace HC.Core
{
    public static class Clock
    {
        public static DateTime LastTime { get; private set; }

        static Clock()
        {
            GCMonitor.Start();
            var worker = new ThreadWorker();
            worker.OnExecute += () =>
            {
                try
                {
                    LastTime = DateTime.Now;
                    Thread.Sleep(200);
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
