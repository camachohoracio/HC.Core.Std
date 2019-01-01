using System;
using System.Collections.Generic;
using System.Threading;
using HC.Core.Logging;
using HC.Core.Time;

namespace HC.Core.Threading
{
    public class PoisonPill
    {
        public const string END_OF_WEEKEND = "EndOfWeekend";
        public const string END_OF_WEEK = "EndOfWeek";
        public const string SHORTLY_BEFORE_END_OF_DAY = "EndOfDay";
        public const string SHORTLY_AFTER_END_OF_DAY = "ShortlyEndOfDay";
        public const string AFTER_END_OF_DAY = "AfterEndOfDay";
        public const string EARLY_MORNING = "EarlyMorning";

        private readonly static List<PoisonPill> m_poissonPills;
        private readonly string m_strPillName;
        private readonly ThreadWorker m_worker;
        private static readonly object m_lockObj = new object();
        private readonly DateTime m_creationDate;
        private readonly DateTime m_dateLimit;

        public static void SwallowPoisonPill(string strName)
        {
            try
            {
                var poissonPill = new PoisonPill(strName);
                lock (m_lockObj)
                {
                    m_poissonPills.Add(poissonPill);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        static PoisonPill()
        {
            m_poissonPills = new List<PoisonPill>();
        }

        private PoisonPill(string strName)
        {
            try
            {
                m_strPillName = strName;
                
                if (string.IsNullOrEmpty(m_strPillName))
                {
                    return;
                }

                m_creationDate = DateTime.Now;
                if (m_strPillName.Equals(END_OF_WEEKEND))
                {
                    m_dateLimit = DateHelper.GetNextDayOfWeek(
                        DateTime.Now,
                        DayOfWeek.Sunday);
                    m_dateLimit = DateHelper.GetEndOfDay(
                        m_dateLimit).AddMinutes(-5);
                }
                else if (m_strPillName.Equals(END_OF_WEEK))
                {
                    m_dateLimit = DateHelper.GetNextDayOfWeek(
                        DateTime.Now,
                        DayOfWeek.Friday);
                    m_dateLimit = DateHelper.GetEndOfDay(
                        m_dateLimit).AddMinutes(-5);
                }
                else if (m_strPillName.Equals(SHORTLY_BEFORE_END_OF_DAY))
                {
                    m_dateLimit = DateHelper.GetEndOfDay(DateTime.Now);
                    m_dateLimit = DateHelper.GetEndOfDay(
                        m_dateLimit).AddMinutes(-5);
                }
                else if (m_strPillName.Equals(AFTER_END_OF_DAY))
                {
                    m_dateLimit = DateHelper.GetEndOfDay(DateTime.Now);
                    m_dateLimit = DateHelper.GetEndOfDay(
                        m_dateLimit).AddMinutes(20);
                }
                else if (m_strPillName.Equals(SHORTLY_AFTER_END_OF_DAY))
                {
                    m_dateLimit = DateHelper.GetEndOfDay(DateTime.Now);
                    m_dateLimit = DateHelper.GetEndOfDay(
                        m_dateLimit).AddMinutes(2.5);
                }
                else if (m_strPillName.Equals(EARLY_MORNING))
                {
                    DateTime baseDate =
                        DateTime.Now;
                    if (baseDate.Hour >= 6)
                    {
                        baseDate = 
                            baseDate.AddDays(1);
                    }
                    m_dateLimit = 
                        new DateTime(
                            baseDate.Year,
                            baseDate.Month,
                            baseDate.Day,
                            6,
                            0,
                            0);
                }
                else
                {
                    return;
                }
                m_strPillName = strName;
                m_worker = new ThreadWorker(ThreadPriority.Highest);
                m_worker.OnExecute += () =>
                                      {
                                          try
                                          {
                                              while (true)
                                              {
                                                  try
                                                  {
                                                      if(DateHelper.IsSameDay(
                                                          DateTime.Now,
                                                          m_creationDate))
                                                      {
                                                          continue;
                                                      }
                                                      if (DateTime.Now >= m_dateLimit)
                                                      {
                                                          TakeThePill();
                                                      }
                                                  }
                                                  catch (Exception ex)
                                                  {
                                                      Logger.Log(ex);
                                                  }
                                                  finally
                                                  {
                                                      Thread.Sleep(1000 * 10);
                                                  }
                                              }
                                          }
                                          catch (Exception ex)
                                          {
                                              Logger.Log(ex);
                                          }
                                      };
                m_worker.Work();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public static void TakeThePill()
        {
            Environment.Exit(0);
        }
    }
}