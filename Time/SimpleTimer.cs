using System;
using HC.Core.Logging;

namespace HC.Core.Time
{
    class SimpleTimer : ISimpleTimer
    {
        #region Members

        private readonly object m_mylock = new object();
        private readonly object m_closure;
        private readonly bool m_blnRepeat;
        private DateTime m_measureFrom;
        private readonly EventHandler m_handle;
        private readonly TimerEngine m_engine;
        private readonly SimpleTimerCb m_callBack;

        #endregion

        private void ComputeMeasureFrom(
            DateTime start)
        {
            //var start = DateTime.Now;

            // if offset=-1 indicate no rounding
            if (OffSet == -1)
            {
                m_measureFrom = start;
            }
            else
            {
                if (m_blnRepeat)
                {
                    start = TimeRounding.RoundDateToSecondInterval(start, TimeSpan / 1000);
                }

                m_measureFrom = start.AddMilliseconds(OffSet);
            }
        }


        public SimpleTimer(
            string id,
            TimerEngine eng,
            int timeSpan,
            int offset,
            SimpleTimerCb cb,
            object closure,
            bool blnRepeat,
            DateTime start)
        {
            TimeSpan = timeSpan;
            OffSet = offset;
            m_blnRepeat = blnRepeat;
            ComputeMeasureFrom(start);
            try
            {
                m_closure = closure;
                m_handle = EngTickEvent;
                m_engine = eng;

                Id = id;

                m_callBack = cb;
                eng.TickEvent += m_handle;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        #region ISimpleTimer Members

        public string Id { get; private set; }
        public int TimeSpan { get; private set; }
        private int OffSet { get; set; }

        public void Dispose()
        {
            FireExpiredEvent();
            m_engine.TickEvent -= m_handle;
        }

        #endregion

        private event EventHandler e;

        public event EventHandler ExpiredEvent
        {
            add
            {
                lock (m_mylock)
                {
                    e += value;
                }
            }
            remove
            {
                lock (m_mylock)
                {
                    e -= value;
                }
            }
        }

        private void FireExpiredEvent()
        {
            if (e != null)
            {
                e(m_closure, new SimpleTimerEventArgs(Id));
            }
        }

        private void EngTickEvent(object sender, EventArgs e)
        {
            try
            {
                TimeSpan ts = DateTime.Now - m_measureFrom;
                if ((int)ts.TotalMilliseconds >= TimeSpan)
                {
                    m_callBack(m_closure);
                    if (!m_blnRepeat)
                    {
                        FireExpiredEvent();
                        m_engine.TickEvent -= m_handle;
                    }
                    else
                    {
                        m_measureFrom = m_measureFrom.AddMilliseconds(TimeSpan);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }
    }
}


