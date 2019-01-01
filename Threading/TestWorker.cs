using System;
using System.Threading;

namespace HC.Core.Threading
{
    public class TestWorker : ICalcWorker, IDisposable
    {
        public string Resource { get; set; }

        private readonly int m_intSleepSeconds;

        /// <summary>
        /// Used for serlialization
        /// </summary>
        public TestWorker()
        {
        }

        public TestWorker(int intSleepSeconds)
        {
            m_intSleepSeconds = intSleepSeconds;
        }

        public void Work()
        {
            Resource += " | before = " + DateTime.Now;
            Thread.Sleep(m_intSleepSeconds * 1000);
            Resource += " | after = " + DateTime.Now;
        }

        public void Dispose()
        {
            Resource = null;
        }
    }
}



