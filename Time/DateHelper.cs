/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 
 This file is part of QLNet Project http://www.qlnet.org

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is  
 available online at <http://trac2.assembla.com/QLNet/wiki/License>.
  
 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.
 
 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/
using System;
using System.Collections.Generic;
using System.Globalization;
using HC.Core.Exceptions;
using HC.Core.Logging;
using NUnit.Framework;

namespace HC.Core.Time
{
    public class DateHelper 
    {

        #region Constants

        public const long TICKS_1_SEC = 10000000;
        public const long TICKS_1_MIN = 60 * TICKS_1_SEC;
        public const long TICKS_1_HOUR = 60 * TICKS_1_MIN;
        public const long TICKS_1_DAY = 24 * TICKS_1_HOUR;
        public const long TICKS_5_MIN = 5 * TICKS_1_MIN;
        private const string DATE_FORMAT = "yyyy_MM_dd";
        private const string SQL_DATE_TIME_FORMAT = "MM/dd/yyyy HH:mm:ss";
        private const string DATE_TIME_FORMAT = "yyyy_MM_dd_HH.mm.ss.fff";
        private const string TIME_FORMAT = "HH:mm:ss";
        public static IFormatProvider CULTURE = new CultureInfo("fr-FR", true);
        public const string TODAY_START_OF_DAY = "@TodayStartOfDay";
        public const string TODAY_END_OF_DAY = "@TodayEndOfDay";

        #endregion

        #region Properties

        public DateTime Date { get; private set; }

        #endregion

        #region Constructors

        public DateHelper() { }							//! Default constructor returning a null m_date.
        //! Constructor taking a serial number as given by Excel. 
        // Serial numbers in Excel have a known problem with leap year 1900
        public DateHelper(int serialNumber)
        {
            Date = (new DateTime(1899, 12, 31)).AddDays(serialNumber - 1);
        }
        public DateHelper(int d, int m, int y) :		//! More traditional constructor.
            this(new DateTime(y, m, d)) { }
        public DateHelper(DateTime d)
        {				//! System DateTime constructor
            Date = d;
        }

        #endregion

        #region Public

        public static List<DatePeriod> GetMonthPeriods(
            DateTime startDate,
            DateTime endDate)
        {
            List<DatePeriod> datePairs = GetDatePeriodList(
                GetStartOfDay(startDate),
                GetEndOfDay(endDate),
                false);
            var dateSet = new HashSet<DateTime>();
            var resultPairs = new List<DatePeriod>();
            foreach (DatePeriod datePeriod in datePairs)
            {
                DateTime startOfMonth = GetStartOfMonth(
                    datePeriod.StartDate);
                if (!dateSet.Contains(startOfMonth))
                {
                    dateSet.Add(startOfMonth);
                    DateTime endOfMonth = GetEndOfMonth(datePeriod.EndDate);
                    resultPairs.Add(
                        new DatePeriod
                        {
                            StartDate = startOfMonth,
                            EndDate = endOfMonth
                        });
                }
            }
            return resultPairs;
        }

        public static List<DatePeriod> GetWeekPeriods(
            DateTime startDate,
            DateTime endDate)
        {
            List<DatePeriod> datePairs = GetDatePeriodList(
                GetStartOfDay(startDate),
                GetEndOfDay(endDate),
                false);
            var dateSet = new HashSet<DateTime>();
            var resultPairs = new List<DatePeriod>();
            foreach (DatePeriod datePeriod in datePairs)
            {
                DateTime startOfWeek = GetStartOfWeek(
                    datePeriod.StartDate);
                if(!dateSet.Contains(startOfWeek))
                {
                    dateSet.Add(startOfWeek);
                    DateTime endOfWeek = GetEndOfWeek(datePeriod.EndDate);
                    resultPairs.Add(
                        new DatePeriod
                            {
                                StartDate = startOfWeek,
                                EndDate = endOfWeek
                            });
                }
            }
            return resultPairs;
        }

        public static int GetWeekNumber(DateTime dateTime)
        {
            return CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                dateTime, 
                CalendarWeekRule.FirstFourDayWeek,
                DayOfWeek.Monday);
        }

        public static DateTime GetEndOfWeek(DateTime dateTime)
        {
            while (dateTime.DayOfWeek != DayOfWeek.Friday)
            {
                dateTime = dateTime.AddDays(1);
            }
            return dateTime;
        }

        public static DateTime GetStartOfWeek(DateTime dateTime)
        {
            while (dateTime.DayOfWeek != DayOfWeek.Monday)
            {
                dateTime = dateTime.AddDays(-1);
            }
            return dateTime;
        }

        public static DateTime GetLastDayOfWeek(
            DateTime dateTime,
            DayOfWeek dayOfWeek)
        {
            while (dateTime.DayOfWeek != dayOfWeek)
            {
                dateTime = dateTime.AddDays(-1);
            }
            return dateTime;
        }

        public static DateTime GetNextDayOfWeek(
            DateTime dateTime,
            DayOfWeek dayOfWeek)
        {
            while (dateTime.DayOfWeek != dayOfWeek)
            {
                dateTime = dateTime.AddDays(1);
            }
            return dateTime;
        }

        public static List<DateTime> GetDailyWorkingDays(
            DateTime startTime,
            DateTime endTime)
        {
            var dateList = new List<DateTime>();
            var currDate = startTime;
            while (currDate <= endTime)
            {
                if (!IsAWeekendDay(currDate) &&
                    !IsAHoliday(currDate))
                {
                    dateList.Add(GetStartOfDay(currDate));
                }
                currDate = currDate.AddDays(1);
            }
            return dateList;
        }

        public static List<DateTime> GetDailyWorkingDays(
            int intLength,
            DateTime endTime)
        {
            var dateList = new List<DateTime>();
            var currDate = endTime;
            while (dateList.Count < intLength)
            {
                if (!IsAWeekendDay(currDate))
                {
                    dateList.Add(GetStartOfDay(currDate));
                }
                currDate = currDate.AddDays(-1);
            }
            dateList.Reverse();
            return dateList;
        }

        public static string ToTimeString(DateTime dateTime)
        {
            return dateTime.ToString(TIME_FORMAT);
        }

        public static DateTime GetNextDate(
            DateTime dateTime,
            DayOfWeek dayOfWeek)
        {
            DateTime currentDate =
                new DateTime(dateTime.Ticks);

            while (currentDate.DayOfWeek != dayOfWeek)
            {
                currentDate = currentDate.AddDays(1);
            }
            return currentDate;
        }

        public static DateTime GePrevDayWeek(
            DateTime time,
            int intFutureJumpsDesired,
            DayOfWeek dayOfWeekDesired,
            int intHours,
            int intMinutes,
            int intSeconds,
            int intMilliSeconds)
        {
            DayOfWeek dayOfWeek = time.DayOfWeek;
            int intJumps = 0;
            while (intJumps != intFutureJumpsDesired || dayOfWeek != dayOfWeekDesired)
            {
                time = time.AddDays(-1);
                dayOfWeek = time.DayOfWeek;
                if(dayOfWeek == dayOfWeekDesired)
                {
                    intJumps++;
                }
            }
            DateTime dateTime = new DateTime(
                time.Year, 
                time.Month, 
                time.Day, 
                intHours, 
                intMinutes, 
                intMilliSeconds);

            return dateTime;
        }

        public static long GetTicksFromSeconds(long lngSeconds)
        {
            return TICKS_1_SEC * lngSeconds;
        }

        public static int ParseMonthPrefix(
            string strMonthPrefix)
        {
            strMonthPrefix = strMonthPrefix.ToLower();
            
            if(strMonthPrefix.Equals("jan"))
            {
                return 1;
            }
            if (strMonthPrefix.Equals("feb"))
            {
                return 2;
            }
            if (strMonthPrefix.Equals("mar"))
            {
                return 3;
            }
            if (strMonthPrefix.Equals("arp"))
            {
                return 4;
            }
            if (strMonthPrefix.Equals("may"))
            {
                return 5;
            }
            if (strMonthPrefix.Equals("jun"))
            {
                return 6;
            }
            if (strMonthPrefix.Equals("jul"))
            {
                return 7;
            }
            if (strMonthPrefix.Equals("aug"))
            {
                return 8;
            }
            if (strMonthPrefix.Equals("sep"))
            {
                return 9;
            }
            if (strMonthPrefix.Equals("oct"))
            {
                return 10;
            }
            if (strMonthPrefix.Equals("nov"))
            {
                return 11;
            }
            if (strMonthPrefix.Equals("dec"))
            {
                return 12;
            }
            throw  new HCException("Date prefix not found");
        }

        public int serialNumber() { return (Date - new DateTime(1899, 12, 31).Date).Days + 1; }
        public int Day { get { return Date.Day; } }
        public int Month { get { return Date.Month; } }
        public int month() { return Date.Month; }
        public int Year { get { return Date.Year; } }
        public int year() { return Date.Year; }
        public int DayOfYear { get { return Date.DayOfYear; } }
        public int weekday() { return (int)Date.DayOfWeek + 1; }       // QL compatible definition
        public DayOfWeek DayOfWeek { get { return Date.DayOfWeek; } }

        // static properties
        public static DateHelper minDate() { return new DateHelper(1, 1, 1901); }
        public static DateHelper maxDate() { return new DateHelper(31, 12, 2199); }
        public static DateHelper Today { get { return new DateHelper(DateTime.Today); } }
        public static bool IsLeapYear(int y) { return DateTime.IsLeapYear(y); }
        public static int DaysInMonth(int y, int m) { return DateTime.DaysInMonth(y, m); }
        public static bool isEndOfMonth(DateHelper d) { return (d.Day == DaysInMonth(d.Year, d.Month)); }


        public static int monthOffset(int m, bool leapYear)
        {
            int[] MonthOffset = { 0,  31,  59,  90, 120, 151,   // Jan - Jun
                                  181, 212, 243, 273, 304, 334,   // Jun - Dec
                                  365     // used in dayOfMonth to bracket day
                                };
            return (MonthOffset[m - 1] + ((leapYear && m > 1) ? 1 : 0));
        }



        public string ToLongDateString() { return Date.ToLongDateString(); }
        public string ToShortDateString() { return Date.ToShortDateString(); }
        public override string ToString() { return this.ToShortDateString(); }
        public override bool Equals(object o) { return (this == (DateHelper)o); }
        public override int GetHashCode() { return 0; }

        public static DateTime ParseDateTimeString(string strToken)
        {
            try
            {
                DateTime dateTime;
                if (ParseWildCardDate(
                    strToken,
                    out dateTime))
                {
                    return dateTime;
                }
                DateTime.TryParseExact(
                    strToken,
                    DATE_TIME_FORMAT,
                    CULTURE,
                    DateTimeStyles.None,
                    out dateTime);
                return dateTime;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return new DateTime();
        }

        private static bool ParseWildCardDate(
            string strToken,
            out DateTime dateTime)
        {
            dateTime = new DateTime();
            try
            {
                if (string.Equals(strToken,
                                  TODAY_START_OF_DAY,
                                  StringComparison.InvariantCultureIgnoreCase))
                {
                    dateTime = DateTime.Today;
                    return true;
                }
                if (string.Equals(strToken,
                                  TODAY_END_OF_DAY,
                                  StringComparison.InvariantCultureIgnoreCase))
                {
                    dateTime = GetEndOfDay(DateTime.Today);
                    return true;
                }
                return false;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return false;
        }

        public static DateTime ParseDateString(string str)
        {
            DateTime dateTime;
            if (ParseWildCardDate(
                str,
                out dateTime))
            {
                return dateTime;
            }
            return DateTime.ParseExact(str, DATE_FORMAT, CULTURE);
        }

        public static string ToDateTimeString(DateTime dateTime)
        {
            return dateTime.ToString(DATE_TIME_FORMAT);
        }

        public static string ToDateString(DateTime dateTime)
        {
            return dateTime.ToString(DATE_FORMAT);
        }

        public static string ToSqlDateString(DateTime dateTime)
        {
            return dateTime.ToString(SQL_DATE_TIME_FORMAT);
        }

        public static List<DateTime> GetWeekDayList(
            DateTime startDate,
            DateTime endDate,
            DayOfWeek dayOfWeek)
        {
            List<DateTime> weekDayList =
                new List<DateTime>();
            DateTime currentDate = startDate;
            while(currentDate < endDate)
            {
                if(currentDate.DayOfWeek == dayOfWeek)
                {
                    weekDayList.Add(currentDate);
                }
                currentDate = currentDate.AddDays(1);
            }
            return weekDayList;
        }

        public static DateTime GetStartOfDay(DateTime dateTime)
        {
            return new DateTime(
                dateTime.Year,
                dateTime.Month,
                dateTime.Day,
                0,
                0,
                0);
        }

        public static DateTime GetEndOfDay(DateTime dateTime)
        {
            try
            {
                if(dateTime == DateTime.MaxValue)
                {
                    return dateTime;
                }

                dateTime = GetStartOfDay(dateTime);
                DateTime eod = dateTime.AddDays(1).AddSeconds(-1);
                return eod;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return dateTime;
        }

        public static bool IsAWeekendDay(DateTime dateTime)
        {
            if (dateTime.DayOfWeek == DayOfWeek.Saturday)
                return true;

            if (dateTime.DayOfWeek == DayOfWeek.Sunday)
                return true;

            return false;
        }

        public static DateTime GetNextWorkingDate(
            DateTime dateTime)
        {
            return GetNextWorkingDate(dateTime, true);
        }

        public static DateTime GetNextWorkingDate(
            DateTime dateTime,
            bool blnIncludeProvidedDate,
            int intDays)
        {
            if (!blnIncludeProvidedDate)
            {
                dateTime = GetNextWorkingDate(dateTime, false);
                //dateTime = dateTime.AddDays(1);
            }

            for (int i = 0; i < intDays; i++)
            {
                dateTime = GetNextWorkingDate(dateTime, false);
                //dateTime = dateTime.AddDays(-1);
            }
            return dateTime;
        }


        public static DateTime GetNextWorkingDate(
            DateTime dateTime,
            bool blnIncludeProvidedDate)
        {
            if(!blnIncludeProvidedDate)
            {
                DateTime currDateTime = MoveToWeekday(dateTime);
                if (IsSameDay(dateTime, currDateTime))
                {
                    dateTime = dateTime.AddDays(1);
                }
                else
                {
                    dateTime = currDateTime;
                }
            }
            dateTime = MoveToWeekday(dateTime);
            return dateTime;
        }

        private static DateTime MoveToWeekday(DateTime dateTime)
        {
            while (IsAWeekendDay(dateTime))
            {
                dateTime = dateTime.AddDays(1);
            }
            return dateTime;
        }

        public static DateTime GetLastWorkingDate(
            DateTime dateTime,
            bool blnIncludeProvidedDate,
            int intDays)
        {
            try
            {
                if (!blnIncludeProvidedDate)
                {
                    dateTime = dateTime.AddDays(-1);
                }

                for (int i = 0; i < intDays; i++)
                {
                    if (i > 0)
                    {
                        dateTime = dateTime.AddDays(-1);
                    }
                    dateTime = GetLastWorkingDate(dateTime);
                }
                return dateTime;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return new DateTime();
        }

        public static DateTime GetLastWorkingDate(
            DateTime dateTime,
            bool blnIncludeProvidedDate = true)
        {
            if(!blnIncludeProvidedDate)
            {
                dateTime = dateTime.AddDays(-1);
            }
            while(IsAWeekendDay(dateTime) ||
                IsAHoliday(dateTime))
            {
                dateTime = dateTime.AddDays(-1);
            }
            return dateTime;
        }

        private static bool IsAHoliday(DateTime dateTime)
        {
            return (dateTime.Day == 1 && dateTime.Month == 1) ||
                   (dateTime.Day == 25 && dateTime.Month == 12);
        }

        public static List<DateTime> GetDateList(
            DateTime startTime,
            DateTime endTime,
            bool blnIncludeWeekends)
        {
            var dateList = new List<DateTime>();
            startTime = GetStartOfDay(startTime);
            endTime = GetStartOfDay(endTime);

            if (blnIncludeWeekends || !IsAWeekendDay(startTime))
            {
                dateList.Add(startTime);
            }
            // move to next day
            startTime = startTime.AddDays(1);

            while (startTime <= endTime)
            {
                if (blnIncludeWeekends || !IsAWeekendDay(startTime))
                {
                    dateList.Add(startTime);
                }
                startTime = startTime.AddDays(1);

                //if (startTime >= endTime)
                //{
                //    if (blnIncludeWeekends || !IsAWeekendDay(startTime))
                //    {
                //        if (blnIncludeWeekends || !IsAWeekendDay(endTime))
                //        {
                //            if (startTime <= endTime)
                //            {
                //                dateList.Add(endTime);
                //            }
                //        }
                //    }
                //    break;
                //}
            }
            return dateList;
        }

        public static bool IsTodaysMonth(DateTime dateTime)
        {
            return IsTodaysMonth(dateTime.Month);
        }

        public static bool IsTodaysMonth(int intMonth)
        {
            DateTime today = DateTime.Today;
            if (today.Month == intMonth)
            {
                return true;
            }
            return false;
        }

        public static bool IsTodaysDate(DateTime dateTime)
        {
            DateTime today = DateTime.Today;
            if (today.Year == dateTime.Year &&
                today.Month == dateTime.Month &&
                today.Day == dateTime.Day)
            {
                return true;
            }
            return false;
        }

        public static List<DatePeriod> GetDatePeriodList(
            DateTime startTime, 
            DateTime endTime,
            bool includeWeekends)
        {
            List<DateTime> dateList = GetDateList(
                startTime,
                endTime,
                includeWeekends);

            var datePairs =
                new List<DatePeriod>();

            foreach (DateTime dateTime in dateList)
            {
                DateTime currentEndDate =
                    GetEndOfDay(dateTime);

                DateTime currentStartDate = dateTime;

                if(startTime > dateTime)
                {
                    currentStartDate = startTime;
                }
                if(endTime < currentEndDate)
                {
                    currentEndDate = endTime;
                }

                datePairs.Add(
                    new DatePeriod(currentStartDate,
                        currentEndDate));
            }
            return datePairs;
        }

        public static DateTime GetEndOfMonth(DateTime dateTime)
        {
            int intMonth = dateTime.Month;
            while (dateTime.Month == intMonth)
            {
                dateTime = dateTime.AddDays(1);
            }
            return dateTime.AddDays(-1);
        }

        public static bool IsSameDay(DateTime dateTime1, DateTime dateTime2)
        {
            return dateTime1.Day == dateTime2.Day &&
                   dateTime1.Month == dateTime2.Month &&
                   dateTime1.Year == dateTime2.Year;
        }

        public static DateTime ParseTimeString(string strStartTime)
        {
            strStartTime = ToDateString(DateTime.Today) + "_" + strStartTime;
            DateTime dateTime;
            DateTime.TryParseExact(
                strStartTime,
                DATE_TIME_FORMAT,
                CULTURE,
                DateTimeStyles.None,
                out dateTime);
            return dateTime;
        }
        
        #endregion

        public static DateTime GetEndOfQuarter(DateTime dateTime)
        {
            int intDaysInYear = GetDaysInYear(dateTime);
            double dblQuarter = (dateTime.DayOfYear)/(double) intDaysInYear;

            if (dblQuarter < 0.25)
            {
                return new DateTime(new DateTime(dateTime.Year, 4, 1).Ticks - 1);
            }
            if (dblQuarter < 0.5)
            {
                return new DateTime(new DateTime(dateTime.Year, 7, 1).Ticks - 1);
            }
            if (dblQuarter < 0.75)
            {
                return new DateTime(new DateTime(dateTime.Year, 10, 1).Ticks - 1);
            }
            return new DateTime(new DateTime(dateTime.Year + 1, 1, 1).Ticks - 1);
        }

        private static int GetDaysInYear(DateTime dateTime)
        {
            if(dateTime.Equals(DateTime.MinValue))
            {
                return -1;
            }
            var thisYear = new DateTime(dateTime.Year, 1, 1);
            var nextYear = new DateTime(dateTime.Year + 1, 1, 1);

            return (nextYear - thisYear).Days;
        }

        public static DateTime GetStartOfMonth(DateTime dateTime)
        {
            return new DateTime(
                dateTime.Year,
                dateTime.Month,
                1);
        }

        public static DateTime GetStartOfYear(DateTime dateTime)
        {
            return new DateTime(dateTime.Year,
                1,1);
        }

        public static DateTime GetEndOfYear(DateTime dateTime)
        {
            return new DateTime(dateTime.Year,
                12, 31);
        }

        [Test]
        public static void TestNextWorkingDate()
        {
            var currDate = new DateTime(2014, 2, 23);
            Assert.IsTrue(IsAWeekendDay(currDate), "It should be a weekend day");
            DateTime nextWorkingDate1 = GetNextWorkingDate(currDate, false);
            DateTime nextWorkingDate2 = GetNextWorkingDate(currDate, true);
            Assert.IsTrue(IsSameDay(nextWorkingDate1, nextWorkingDate2), "invalid working date");
            DateTime workingDate = currDate.AddDays(1);
            Assert.IsTrue(!IsAWeekendDay(workingDate), "It shoud be a working date");
            nextWorkingDate1 = GetNextWorkingDate(workingDate, false);
            nextWorkingDate2 = GetNextWorkingDate(workingDate, true);
            Assert.IsTrue(!IsSameDay(nextWorkingDate1, nextWorkingDate2), "invalid working date");
            Assert.IsTrue(nextWorkingDate1 > nextWorkingDate2, "Date not less");
        }

        [Test]
        public static void TestNonWorkingDate()
        {
            DateTime currDate = DateTime.Today;
            int intDateCount = 0;
            for (int i = 0; i < 20; i++)
            {
                DateTime lastForecastDate = GetNextWorkingDate(
                    currDate,
                    false,
                    30);
                List<DateTime> dateList = GetDateList(
                    currDate,
                    lastForecastDate,
                    false);
                if (intDateCount == 0)
                {
                    intDateCount = dateList.Count;
                }
                Assert.IsTrue(intDateCount == dateList.Count,
                    "Missmatch [" + intDateCount + "] vs [" + dateList.Count + "][" +
                    i + "]");
            }

        }

        [Test]
        public static void TestWorkingDates()
        {
            var entryDate = new DateTime(2005,6,17);
            DateTime lastForecastDate = GetNextWorkingDate(
                entryDate,
                false,
                8);

            List<DateTime> dateList = GetDateList(
                entryDate,
                lastForecastDate,
                false);

            Console.WriteLine(dateList.Count);

            var entryDate2 = new DateTime(2005, 2, 23);
            var lastForecastDate2 = GetNextWorkingDate(
                entryDate2,
                false,
                8);

            List<DateTime> dateList2 = GetDateList(
                entryDate2,
                lastForecastDate2,
                false);

            Assert.IsTrue(dateList.Count == dateList2.Count);

        }

        [Test]
        public static void TestFloorDate()
        {
            var testDate = new DateTime(2012, 2, 2, 18, 15, 5);
            DateTime floorDate = GetFloorDate(testDate, 3);
            double dblTotalSeconds = (new DateTime(2012, 2, 2, 18, 0, 0) - floorDate).TotalSeconds;
            Assert.IsTrue(dblTotalSeconds == 0);
        }

        public static DateTime GetFloorDate(DateTime dateTime, int intHoursFloor)
        {
            try
            {
                var startOfDay = GetStartOfDay(dateTime);
                while (((dateTime.Hour*1.0)%intHoursFloor) != 0)
                {
                    dateTime = dateTime.AddHours(-1);
                    if (startOfDay > dateTime)
                    {
                        return startOfDay;
                    }
                }
                return new DateTime(
                    dateTime.Year,
                    dateTime.Month,
                    dateTime.Day,
                    dateTime.Hour,
                    0,
                    0);

            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return dateTime;
        }

        public static DateTime GetStartOfQuarter(DateTime dateTime)
        {
            int intDaysInYear = GetDaysInYear(dateTime);
            double dblQuarter = (dateTime.DayOfYear) / (double)intDaysInYear;

            if (dblQuarter < 0.25)
            {
                return new DateTime(new DateTime(dateTime.Year, 1, 1).Ticks - 1);
                //return new DateTime(new DateTime(dateTime.Year, 4, 1).Ticks - 1);
            }
            if (dblQuarter < 0.5)
            {
                return new DateTime(new DateTime(dateTime.Year, 4, 1).Ticks - 1);
                //return new DateTime(new DateTime(dateTime.Year, 7, 1).Ticks - 1);
            }
            if (dblQuarter < 0.75)
            {
                return new DateTime(new DateTime(dateTime.Year, 7, 1).Ticks - 1);
                //return new DateTime(new DateTime(dateTime.Year, 10, 1).Ticks - 1);
            }
            return new DateTime(new DateTime(dateTime.Year, 10, 1).Ticks - 1);
            //return new DateTime(new DateTime(dateTime.Year + 1, 1, 1).Ticks - 1);
        }

        public static bool IsSameMonthYear(DateTime date1, DateTime date2)
        {
            return date1.Year == date2.Year && date1.Month == date2.Month;
        }

        public static DateTime GetStartOfMinute(DateTime time)
        {
            return new DateTime(time.Year,
                time.Month,
                time.Day,
                time.Hour,
                time.Minute,
                0);
        }

        public static DateTime GetStartOfSecond(DateTime time)
        {
            return new DateTime(
                time.Year,
                time.Month,
                time.Day,
                time.Hour,
                time.Minute,
                time.Second);
        }

        public static DateTime GetStartOfHour(DateTime time)
        {
            return new DateTime(time.Year,
                time.Month,
                time.Day,
                time.Hour,
                0,
                0);
        }

        public static bool IsStartOfHour(DateTime dateTime)
        {
            return dateTime.Minute == 0 &&
                   dateTime.Second == 0;
        }

        public static DateTime GetNow()
        {
            return DateTime.Now;
            ;
        }
    }
}




