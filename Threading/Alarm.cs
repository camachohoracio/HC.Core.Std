using System;
using System.Threading;
using HC.Core.Cache.SqLite;
using HC.Core.Logging;
using HC.Core.Threading.ProducerConsumerQueues;

namespace HC.Core.Threading
{
    public delegate void AlarmDel(DateTime dateTime);
    public class Alarm
    {
        private readonly AlarmDel m_alarmDel;
        private readonly ProducerConsumerQueue<ObjWrapper> m_safeQueue;

        public Alarm(
            AlarmDel alarmDel)
        {
            try
            {
                m_alarmDel = alarmDel;
                m_safeQueue =
                    new ProducerConsumerQueue<ObjWrapper>(1);
                m_safeQueue.SetAutoDisposeTasks(true);
                m_safeQueue.OnWork += objWrapper =>
                {
                    try
                    {
                        var alarmTime =
                            (DateTime) objWrapper.Obj;
                        while (DateTime.Now < alarmTime)
                        {
                            Thread.Sleep(1000);
                        }
                        //
                        // trigger alaram 
                        //
                        m_alarmDel(DateTime.Now);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                    }
                };
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public void SetAlarm(
            DateTime dateTime)
        {
            m_safeQueue.EnqueueTask(
                new ObjWrapper(dateTime));
        }
    }
}