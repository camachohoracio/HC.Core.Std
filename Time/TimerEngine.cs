using System;
using System.Collections.Generic;
using System.Threading;

namespace HC.Core.Time
{
    /// <summary>
    /// 
    /// </summary>
    /// 
    // Thread.Timer does not handle large numbers of timers with short time-spans.  
    // - thread safe.  
    // - work for large (several hundred) numbers of timers in the region of seconds.
    // - It uses the threadpool to tick 
    // - does not requires a thread.sleep. 
    // - align to the system clock to allow timers to target specific time points
    // i.e. to fire at 10:10:10, 10:11:10, 10:12:10 etc.
    public delegate void SimpleTimerCb(object closure);

    public class TimerEngine : IDisposable
    {
        #region Events

        public event EventHandler E;

        #endregion

        #region Properties

        public static TimerEngine OwnInstance { get; private set; }

        #endregion

        #region Members

        private readonly object m_mylock = new object();
        private readonly Dictionary<string, SimpleTimer> m_timers;
        private readonly EventHandler m_expiredHandler;
        private readonly RegisteredWaitHandle m_waitHandle;
        private readonly EventWaitHandle m_ewh;
        private int m_reentrancy;
        private readonly int m_intervalMs;

        #endregion

        static TimerEngine()
        {
            OwnInstance = new TimerEngine();
        }

        private TimerEngine()
        {
            m_timers = new Dictionary<string, SimpleTimer>();
            m_intervalMs = 5;
            m_expiredHandler = StExpiredEvent;
            m_ewh = new AutoResetEvent(false);
            m_waitHandle = ThreadPool.RegisterWaitForSingleObject(m_ewh, Tick, this, m_intervalMs, false);
        }

        #region IDisposable Members

        public void Dispose()
        {
            m_waitHandle.Unregister(null);
        }

        #endregion

        // the TickCB now is invoked by the threadpool which saves a threads - in cases of large
        // number of strategies in system running this will significantly reduce number of threads

        private void Tick(object o, bool s)
        {
            Interlocked.Increment(ref m_reentrancy);
            if (m_reentrancy == 1)
            {
                FireTickEvent();
            }
            Interlocked.Decrement(ref m_reentrancy);
        }

        public event EventHandler TickEvent
        {
            add
            {
                //lock (m_mylock)
                {
                    E += value;
                }
            }
            remove
            {
                //lock (m_mylock)
                {
                    E -= value;
                }
            }
        }

        public ISimpleTimer CreateTimer(
            long lngTicks,
            long lngOffsetTicks,
            SimpleTimerCb cb,
            object closure,
            bool repeat,
            DateTime start = new DateTime())
        {
            if(start == new DateTime())
            {
                start = DateTime.Now;
            }

            var intMills = (int)((1000 * lngTicks) / DateHelper.TICKS_1_SEC);
            var intOffsetMills = (int)((1000 * lngOffsetTicks) / DateHelper.TICKS_1_SEC);

            return CreateTimer(
                intMills,
                intOffsetMills,
                cb,
                closure,
                repeat,
                start);
        }

        private ISimpleTimer CreateTimer(
            int intMilliSeconds,
            SimpleTimerCb cb,
            object closure,
            bool repeat,
            DateTime start)
        {
            return CreateTimer(
                intMilliSeconds, 
                0, 
                cb, 
                closure, 
                repeat,
                start);
        }

        private ISimpleTimer CreateTimer(
            int intMilliSeconds,
            int intOffsetMills,
            SimpleTimerCb cb,
            object closure,
            bool repeat,
            DateTime start)
        {
            var st = new SimpleTimer(
                (intMilliSeconds + "_" + intOffsetMills),
                this,
                intMilliSeconds,
                intOffsetMills,
                cb,
                closure,
                repeat,
                start);
            st.ExpiredEvent += m_expiredHandler;
            lock (m_timers)
            {
                m_timers[st.Id] = st;
            }
            return st;
        }

        public ISimpleTimer CreateTimer(
            int intMilliSeconds, 
            SimpleTimerCb cb,
            object closure,
            DateTime start)
        {
            return CreateTimer(
                intMilliSeconds,
                cb, 
                closure, 
                false,
                start);
        }

        private void FireTickEvent()
        {
            lock (m_mylock)
            {
                if (E != null)
                {
                    E(this, new EventArgs());
                }
            }
        }

        private void StExpiredEvent(object sender, EventArgs e)
        {
            var args = e as SimpleTimerEventArgs;
            if (args == null)
                return;
            SimpleTimer st;
            lock (m_mylock)
            {
                if (m_timers.TryGetValue(args.Id, out st))
                {
                    st = m_timers[args.Id];
                    m_timers.Remove(args.Id);
                }
            }

            if (st != null)
            {
                st.ExpiredEvent -= m_expiredHandler;
            }
        }
    }
}


