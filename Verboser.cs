using System;
using HC.Core.Logging;

namespace HC.Core
{
    public class Verboser
    {
        private static DateTime m_prevTalk = DateTime.Now;
        private DateTime m_prevTalkInst = DateTime.Now;
        private readonly int m_intSeconds;
        private static readonly object m_logLock = new object();
        private const int DEFAULT_SECONDS = 3;
        public static bool MUTE = false;
        public static double TALK_SECONDS = 3;

        public Verboser() : this(DEFAULT_SECONDS)
        {
        }

        public Verboser(int intSeconds)
        {
            m_intSeconds = intSeconds;
        }

        public void DoTalk(String strMessage)
        {
            try
            {
                bool blnDoLog;
                lock (m_logLock)
                {
                    double dblSeconds = (DateTime.Now - m_prevTalkInst).TotalSeconds;
                    blnDoLog = (dblSeconds > m_intSeconds);
                }
                if(blnDoLog)
                {
                    Console.WriteLine(strMessage);
                    Logger.Log(strMessage);
                    m_prevTalkInst = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public static void Talk(String strMessage)
        {
            try
            {
                if (MUTE)
                {
                    return;
                }
                double dblSeconds = (DateTime.Now - m_prevTalk).TotalSeconds;
                if (dblSeconds >= TALK_SECONDS)
                {

                    Console.WriteLine(strMessage);
                    m_prevTalk = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public static void WriteLine(string s)
        {
            Talk(s);
        }
    }
}
