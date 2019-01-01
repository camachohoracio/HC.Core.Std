#region

using System;
using HC.Core.Helpers;

#endregion

namespace HC.Core.Time
{
    public class TimeAndDate
    {
        private readonly string[] days = {"Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"};
        private readonly int[] monthDays = {31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31};

        private readonly string[] months = {
                                               "January", "February", "March", "April", "May", "June", "July", "August",
                                               "September", "October", "November", "December"
                                           };

        private bool backForw = true;
        // = true next BST change is back, i.e if date is within BST; = false next BST change = forward

        private DateTime cal = DateTime.Today; // instance of abstract class Calendar
        private string changeDate; // next BST change
        private string date; // 'day of month' 'month name' 'year'
        private int dayHold; // Last calculated day of the month
        private string dayNameHold; // Last calculated day name
        private int dayOfTheMonth; // day of the month
        private string dayOfTheWeek; // day of the week
        private int easterDay; // Easter Sunday day of the month
        private string easterDayName; // Easter Sunday day name, i.e. "Sunday" - calculated as a check of the method
        private int easterMonth; // Easter Sunday month
        private bool endCheck; // = true when timing end set
        private string fullDate; // date as 'day name', 'day of month' 'month name' year
        private string fullTime12; // time as hour.minute.second.millisecond AM OR PM (12 hour clock)
        private string fullTime24; // time as hour.minute.second.millisecond (24 hour clock)
        private string hour12; // hour of the day as am or pm (12 hour clock)
        private int hour24 = -1; // hour of the day (24 hour clock)
        private string midTime12; // time as hour.minute.second AM or PM (12 hour clock)
        private string midTime24; // time as hour.minute.second (24 hour clock)
        private int millisecond = -1; // milliseconds of the second
        private int minute = -1; // minute of the hour
        private int monthAsInteger; // month as integer
        private int monthHold; // Last calculated month
        private string monthOfTheYear; // month of the year
        private int second = -1; // seconds of the minute
        private string shortDateUK; // UK Format - 'day of month'.'month'.'year  two digits'
        private string shortDateUS; // US Format - 'month'/'day of month'/'year  two digits'
        private string shortTime12; // time as hour.minute AM or PM (12 hour clock)
        private string shortTime24; // time as hour.minute (24 hour clock)
        private bool startCheck; // = true when timing start set
        private long tEnd; // end time
        private long totalTime; // tEnd - tStart
        private long tStart; // start time
        private int year; // year

        private int yearHold; // Last calculated year


        // CONSTRUCTOR

        // Causes the program to wait for nSeconds seconds before continuing (int)
        public void waitFor(int nSeconds)
        {
            long t0, t1;

            t0 = DateTime.Today.Millisecond;
            do
            {
                t1 = DateTime.Today.Millisecond;
            } while ((t1 - t0) < nSeconds*1000);
        }

        // Causes the program to wait for nSeconds seconds before continuing (long)
        public void waitFor(long nSeconds)
        {
            if (nSeconds > long.MaxValue/1000)
            {
                PrintToScreen.WriteLine(
                    "Class: TimeAndDate, method: wait(long nSeconds), nSeconds is too large for this method - the value has been replaced by " +
                    long.MaxValue/1000);
                nSeconds = long.MaxValue/1000;
            }
            long t0, t1;
            t0 = DateTime.Today.Millisecond;
            do
            {
                t1 = DateTime.Today.Millisecond;
            } while ((t1 - t0) < nSeconds*1000);
        }

        // Causes the program to wait for nSeconds seconds before continuing (double)
        public void waitFor(double nSeconds)
        {
            long tt = 0L;
            if (nSeconds > Math.Pow(2.0, 63) - 1.0)
            {
                PrintToScreen.WriteLine(
                    "Class: TimeAndDate, method: wait(double nSeconds), nSeconds is too large for this method - the value has been replaced by " +
                    long.MaxValue/1000);
                tt = long.MaxValue;
            }
            else
            {
                tt = (long)nSeconds*1000;
            }
            long t0, t1;
            t0 = DateTime.Today.Millisecond;
            do
            {
                t1 = DateTime.Today.Millisecond;
            } while ((t1 - t0) < tt);
        }

        // Marker method for starting the timing of a bloc of code
        public void blocStart()
        {
            tStart = DateTime.Today.Millisecond;
            startCheck = true;
        }

        // Marker method for ending the timing of a bloc of code and for returning the total time
        public long blocEnd()
        {
            if (startCheck)
            {
                tEnd = DateTime.Today.Millisecond;
                totalTime = tEnd - tStart;
                endCheck = true;
            }
            else
            {
                throw new ArgumentException("No start marker has been set");
            }
            return totalTime;
        }

        // Returns total time taken to run a bloc of code
        public long blocTime()
        {
            if (endCheck)
            {
                return totalTime;
            }
            else
            {
                if (!startCheck)
                {
                    PrintToScreen.WriteLine(
                        "Class Time: method totalTime:  No start marker has been set - -9999 rturned");
                    return -9999L;
                }
                else
                {
                    PrintToScreen.WriteLine("Class Time: method totalTime:  No end marker has been set - -8888 rturned");
                    return -8888L;
                }
            }
        }

        // Get the hour of the day (24 hour clock)
        public int getHour24()
        {
            hour24 = cal.Hour;
            return hour24;
        }

        // Get the hour of the day, am or pm (12 hour clock)
        public string getHour12()
        {
            int hour = cal.Hour;
            int amPm = cal.Hour > 11 ? 0 : 1;
            if (amPm == 0)
            {
                hour12 = ((hour)) + " AM";
            }
            else
            {
                hour12 = ((hour)) + " PM";
            }
            return hour12;
        }

        // Get the minute of the hour
        public int getMinute()
        {
            minute = cal.Minute;
            return minute;
        }

        // Get the second of the minute
        public int getSecond()
        {
            second = cal.Second;
            return second;
        }

        // Get the millisecond of the second
        public int getMilliSecond()
        {
            millisecond = cal.Millisecond;
            return millisecond;
        }

        // Get time as hour.minute (24 hour clock)
        public string getShortTime24()
        {
            int hourI = getHour24();
            shortTime24 = ((hourI)).ToString();
            int minI = getMinute();
            if (minI < 10)
            {
                shortTime24 += ".0" + minI;
            }
            else
            {
                shortTime24 += "." + minI;
            }
            return shortTime24;
        }

        // Get time as hour.minute AM or PM (12 hour clock)
        public string getShortTime12()
        {
            int hourI = cal.Hour;
            int amPm = cal.Hour > 11 ? 0 : 1;
            shortTime12 = ((hourI)).ToString();
            int minI = getMinute();
            if (minI < 10)
            {
                shortTime12 += ".0" + minI;
            }
            else
            {
                shortTime12 += "." + minI;
            }
            if (amPm == 0)
            {
                shortTime12 += " " + "AM";
            }
            else
            {
                shortTime12 += " " + "PM";
            }
            return shortTime12;
        }

        // Get time as hour.minute.second (24 hour clock)
        public string getMidTime24()
        {
            int hourI = getHour24();
            midTime24 = ((hourI)).ToString();
            int minI = getMinute();
            if (minI < 10)
            {
                midTime24 += ".0" + minI;
            }
            else
            {
                midTime24 += "." + minI;
            }
            int secI = getSecond();
            if (secI < 10)
            {
                midTime24 += ".0" + secI;
            }
            else
            {
                midTime24 += "." + secI;
            }
            return midTime24;
        }

        // Get time as hour.minute.second AM or PM (12 hour clock)
        public string getMidTime12()
        {
            int hourI = cal.Hour;
            int amPm = cal.Hour > 11 ? 0 : 0;
            midTime12 = ((hourI)).ToString();
            int minI = getMinute();
            if (minI < 10)
            {
                midTime12 += ".0" + minI;
            }
            else
            {
                midTime12 += "." + minI;
            }
            int secI = getSecond();
            if (secI < 10)
            {
                midTime12 += ".0" + secI;
            }
            else
            {
                midTime12 += "." + secI;
            }
            if (amPm == 0)
            {
                midTime12 += " " + "AM";
            }
            else
            {
                midTime12 += " " + "PM";
            }
            return midTime12;
        }

        // Get time as hour.minute.second.millisecond (24 hour clock)
        public string getFullTime24()
        {
            int hourI = getHour24();
            fullTime24 = ((hourI)).ToString();
            int minI = getMinute();
            if (minI < 10)
            {
                fullTime24 += ".0" + minI;
            }
            else
            {
                fullTime24 += "." + minI;
            }
            int secI = getSecond();
            if (secI < 10)
            {
                fullTime24 += ".0" + secI;
            }
            else
            {
                fullTime24 += "." + secI;
            }
            int msecI = getMilliSecond();
            if (msecI < 10)
            {
                fullTime24 += ".00" + msecI;
            }
            else
            {
                if (msecI < 100)
                {
                    fullTime24 += ".0" + msecI;
                }
                else
                {
                    fullTime24 += "." + msecI;
                }
            }
            return fullTime24;
        }

        // Get time as hour.minute.second.millisecond AM OR PM (12 hour clock)
        public string getFullTime12()
        {
            int hourI = cal.Hour;
            int amPm = cal.Hour > 11 ? 0 : 1;
            fullTime12 = ((hourI)).ToString();
            int minI = getMinute();
            if (minI < 10)
            {
                fullTime12 += ".0" + minI;
            }
            else
            {
                fullTime12 += "." + minI;
            }
            int secI = getSecond();
            if (secI < 10)
            {
                fullTime12 += ".0" + secI;
            }
            else
            {
                fullTime12 += "." + secI;
            }
            int msecI = getMilliSecond();
            if (msecI < 10)
            {
                fullTime12 += ".00" + msecI;
            }
            else
            {
                if (msecI < 100)
                {
                    fullTime12 += ".0" + msecI;
                }
                else
                {
                    fullTime12 += "." + msecI;
                }
            }
            if (amPm == 0)
            {
                fullTime12 += " " + "AM";
            }
            else
            {
                fullTime12 += " " + "PM";
            }
            return fullTime12;
        }

        // Return the current computer time in milliseconds
        public long getComputerTime()
        {
            return DateTime.Today.Millisecond;
        }

        // Converts a date to milliseconds since 0 hours 0 minutes 0 seconds on 1 Jan 1970
        public long dateToJavaMilliSecondsUK(int year, int month, int dayOfTheMonth, string dayOfTheWeek, int hour,
                                             int min, int sec, int millisec)
        {
            long ms = 0L; // milliseconds since  0 hours 0 minutes 0 seconds and o milliseconds on 1 Jan 1970

            // Day of the week as integer
            int dayIndicator = getDayOfTheWeekAsInteger(dayOfTheWeek);

            // British Summer Time adjustment
            long bst = 0;
            backForw = checkBST(dayOfTheWeek, dayOfTheMonth, hour, month, dayIndicator);
            if (backForw)
            {
                bst = 1;
            }

            // millisecond calculation
            if (year >= 1970)
            {
                // DateWrapper after the zero computer time
                long yearDiff = 0L;
                int yearTest = year - 1;
                while (yearTest >= 1970)
                {
                    yearDiff += 365;
                    if (leapYear(yearTest))
                    {
                        yearDiff++;
                    }
                    yearTest--;
                }
                yearDiff *= 24L*60L*60L*1000L;

                long monthDiff = 0L;
                int monthTest = month - 1;
                while (monthTest > 0)
                {
                    monthDiff += monthDays[monthTest - 1];
                    if (leapYear(year) && monthTest == 2)
                    {
                        monthDiff++;
                    }
                    monthTest--;
                }
                monthDiff *= 24L*60L*60L*1000L;

                long dayDiff = (dayOfTheMonth - 1)*24L*60L*60L*1000L;

                ms = yearDiff + monthDiff + dayDiff + (hour - bst)*60L*60L*1000L + min*60L*1000L + sec*1000L + millisec;
            }
            else
            {
                // DateWrapper before the zero computer time
                long yearDiff = 0L;
                int yearTest = year + 1;
                while (yearTest < 1970)
                {
                    yearDiff += 365;
                    if (leapYear(yearTest))
                    {
                        yearDiff++;
                    }
                    yearTest++;
                }
                yearDiff *= 24L*60L*60L*1000L;

                long monthDiff = 0L;
                int monthTest = month - 1;
                while (monthTest > 0)
                {
                    monthDiff += monthDays[monthTest - 1];
                    if (leapYear(year) && monthTest == 2)
                    {
                        monthDiff++;
                    }
                    monthTest--;
                }

                monthDiff *= 24L*60L*60L*1000L;

                long dayDiff = (dayOfTheMonth - 1)*24L*60L*60L*1000L;

                monthDiff = monthDiff + dayDiff + (hour - bst)*60L*60L*1000L + min*60L*1000L + sec*1000L + millisec;

                long myear = 365L;
                if (leapYear(year))
                {
                    myear++;
                }
                myear *= 24L*60L*60L*1000L;

                ms = myear - monthDiff;
                ms += yearDiff;
                ms = -ms;
            }

            return ms;
        }

        // Check whether within British summer time period
        public bool checkBST()
        {
            string dayOfTheWeek = getDayOfTheWeek();
            int dayOfTheMonth = getDayOfTheMonth();
            int hour = getMonthAsInteger();
            int month = getMonthAsInteger();
            int dayIndicator = getDayOfTheWeekAsInteger(dayOfTheWeek);

            return checkBST(dayOfTheWeek, dayOfTheMonth, hour, month, dayIndicator);
        }

        // Check whether within British summer time period - private method for internal use
        private bool checkBST(string dayOfTheWeek, int dayOfTheMonth, int hour, int month, int dayIndicator)
        {
            if (month > 3 && month < 10)
            {
                backForw = true;
            }
            else
            {
                if (month == 3 && dayOfTheMonth > 24)
                {
                    if (dayIndicator == 0)
                    {
                        if (hour >= 1)
                        {
                            backForw = true;
                        }
                    }
                    else
                    {
                        if (dayIndicator > 0 && dayIndicator < dayOfTheMonth - 24)
                        {
                            backForw = true;
                        }
                    }
                }
                else
                {
                    if (month == 10 && dayOfTheMonth > 24)
                    {
                        if (dayIndicator == 0)
                        {
                            if (hour <= 2)
                            {
                                backForw = true;
                            }
                        }
                        else
                        {
                            backForw = true;
                            if (dayIndicator > 0 && dayIndicator < dayOfTheMonth - 24)
                            {
                                backForw = false;
                            }
                        }
                    }
                }
            }

            return backForw;
        }

        // Returns the day of the week as an integer (Sunday = 1, Monday = 1 etc)
        public int getDayOfTheWeekAsInteger()
        {
            string dayOfTheWeek = getDayOfTheWeek();

            return getDayOfTheWeekAsInteger(dayOfTheWeek) + 1;
        }

        // Returns the day of the week as an integer (Sunday = 0, Monday = 1 etc) - private method for internal use
        private int getDayOfTheWeekAsInteger(string dayOfTheWeek)
        {
            // Day of the week as integer
            int counter = 0;
            int dayIndicator = 0;
            bool test = true;
            while (test)
            {
                if (dayOfTheWeek.Equals(days[counter]))
                {
                    dayIndicator = counter;
                    test = false;
                }
                else
                {
                    counter++;
                    if (counter > 6)
                    {
                        throw new ArgumentException(dayOfTheWeek + " is not recognised as a day of the week");
                    }
                }
            }

            return dayIndicator;
        }

        // Calculates the next British Summer Time clock change for the current date
        public string nextBstClockChange()
        {
            backForw = true; // = true change back, i.e if date is within bst; = false change = forward

            string dayOfTheWeek = getDayOfTheWeek();
            int dayOfTheMonth = getDayOfTheMonth();
            int hour = getMonthAsInteger();
            int month = getMonthAsInteger();

            // Day of the week as integer
            int dayIndicator = getDayOfTheWeekAsInteger(dayOfTheWeek);

            // Check whether within British summer time period
            backForw = checkBST(dayOfTheWeek, dayOfTheMonth, hour, month, dayIndicator);

            // Find next Sunday to today's date
            int daysDiff = 0;
            int newDayOfTheMonth = dayOfTheMonth;
            int newMonth = month;
            int newYear = year;
            int oldNewDayOfTheMonth = newDayOfTheMonth;
            int oldMonth = newMonth;
            int oldYear = newYear;
            if (dayIndicator != 0)
            {
                daysDiff = 7 - dayIndicator;
            }
            newDayOfTheMonth = dayOfTheMonth + daysDiff;
            int monthD = monthDays[newMonth - 1];
            if (newMonth == 2 && leapYear(newYear))
            {
                monthD++;
            }
            if (newDayOfTheMonth > monthD)
            {
                newDayOfTheMonth -= monthD;
                newMonth++;
                if (newMonth == 13)
                {
                    newMonth = 1;
                    newYear = oldYear + 1;
                }
            }

            if (!backForw)
            {
                bool test = true;
                while (test)
                {
                    if (newMonth == 3 && newDayOfTheMonth > 24)
                    {
                        changeDate = "Sunday, " + newDayOfTheMonth + " March " + year + ", one hour forward";
                        test = false;
                    }
                    else
                    {
                        newDayOfTheMonth += 7;
                        monthD = monthDays[newMonth - 1];
                        if (newMonth == 2 && leapYear(newYear))
                        {
                            monthD++;
                        }
                        if (newDayOfTheMonth > monthD)
                        {
                            newDayOfTheMonth -= monthD;
                            newMonth++;
                            if (newMonth == 13)
                            {
                                newMonth = 1;
                                newYear = newYear + 1;
                            }
                        }
                    }
                }
            }
            else
            {
                bool test = true;
                while (test)
                {
                    if (newMonth == 10 && newDayOfTheMonth > 24)
                    {
                        changeDate = "Sunday, " + newDayOfTheMonth + " October " + year + ", one hour back";
                        test = false;
                    }
                    else
                    {
                        newDayOfTheMonth += 7;
                        monthD = monthDays[newMonth - 1];
                        if (newMonth == 2 && leapYear(newYear))
                        {
                            monthD++;
                        }
                        if (newDayOfTheMonth > monthD)
                        {
                            newDayOfTheMonth -= monthD;
                            newMonth++;
                            if (newMonth == 13)
                            {
                                newMonth = 1;
                                newYear = newYear + 1;
                            }
                        }
                    }
                }
            }
            return changeDate;
        }


        // Returns the the day of the week by name, e.g. Sunday
        public string getDayOfTheWeek()
        {
            int dayAsInt = cal.Day;
            dayOfTheWeek = days[dayAsInt - 1];
            return dayOfTheWeek;
        }

        // Returns the the day of the month as integer, e.g. 24 for the 24th day of the month
        public int getDayOfTheMonth()
        {
            dayOfTheMonth = cal.Day;
            return dayOfTheMonth;
        }

        // Returns the the month by name, e.g. January
        public string getMonth()
        {
            int monthAsInt = cal.Month;
            monthOfTheYear = months[monthAsInt];
            return monthOfTheYear;
        }

        // Returns the month as an integer, e.g. January as 1
        public int getMonthAsInteger()
        {
            monthAsInteger = cal.Month + 1;
            return monthAsInteger;
        }

        // Returns the month as an integer, e.g. January as 1, private method for internal use
        public int getMonthAsInteger(string month)
        {
            int monthI = 0;
            bool test = true;
            int counter = 0;
            while (test)
            {
                if (month.Equals(months[counter]))
                {
                    monthI = counter + 1;
                    test = false;
                }
                else
                {
                    counter++;
                    if (counter == 12)
                    {
                        throw new ArgumentException(month + " is not recognised as a valid month name");
                    }
                }
            }
            return monthI;
        }

        // Returns the year as four digit number
        public int getYear()
        {
            year = cal.Year;
            return year;
        }

        // Returns the date as 'day of month' 'month name' 'year'
        public string getDate()
        {
            date = ((getDayOfTheMonth())).ToString();
            date += " " + getMonth();
            date += " " + getYear();
            return date;
        }

        // Returns the date as 'day name', 'day of month' 'month name' year
        public string getFullDate()
        {
            fullDate = getDayOfTheWeek();
            fullDate += ", " + getDayOfTheMonth();
            fullDate += " " + getMonth();
            fullDate += " " + getYear();
            return fullDate;
        }

        // Returns the date as the UK short format - 'day of month'.'month number'.'year  two digits'
        public string getShortDateUK()
        {
            shortDateUK = ((getDayOfTheMonth())).ToString();
            if (shortDateUK.Length < 2)
            {
                shortDateUK = "0" + shortDateUK;
            }
            int monthI = getMonthAsInteger();
            if (monthI < 10)
            {
                shortDateUK += ".0" + monthI;
            }
            else
            {
                shortDateUK += "." + monthI;
            }
            string yearS = ((getYear())).ToString();
            shortDateUK += "." + yearS.Substring(2);
            return shortDateUK;
        }

        // Returns the date as the US short format - 'month number'/'day of month'/'year  two digits'
        public string getShortDateUS()
        {
            shortDateUS = ((getMonthAsInteger())).ToString();
            if (shortDateUS.Length < 2)
            {
                shortDateUS = "0" + shortDateUS;
            }
            int dayI = getDayOfTheMonth();
            if (dayI < 10)
            {
                shortDateUS += "/0" + dayI;
            }
            else
            {
                shortDateUS += "/" + dayI;
            }
            string yearS = ((getYear())).ToString();
            shortDateUS += "/" + yearS.Substring(2);
            return shortDateUS;
        }

        // Returns true if entered date (xxxT) is later than the current date (xxx) otherwise returns false, private method for internal use
        private bool direction(int dayOfTheMonthT, int monthT, int yearT, int dayOfTheMonth, int month, int year)
        {
            //bool test = true;
            bool direction_ = false;
            if (year > yearT)
            {
                direction_ = true;
            }
            else
            {
                if (year < yearT)
                {
                    direction_ = false;
                }
                else
                {
                    if (month > monthT)
                    {
                        direction_ = true;
                    }
                    else
                    {
                        if (month < monthT)
                        {
                            direction_ = false;
                        }
                        else
                        {
                            if (dayOfTheMonth >= dayOfTheMonthT)
                            {
                                direction_ = true;
                            }
                            else
                            {
                                direction_ = false;
                            }
                        }
                    }
                }
            }
            return direction_;
        }

        // Returns the day of the week for a given date - month as string, e.g. January
        public string getDayOfDate(int dayOfTheMonth, string month, int year)
        {
            int monthI = getMonthAsInteger(month);
            return getDayOfDate(dayOfTheMonth, monthI, year);
        }


        // Returns the day of the week for a given date - month as integer, January = 1
        public string getDayOfDate(int dayOfTheMonth, int month, int year)
        {
            string dayOfDate = null;
            int yearT = getYear();
            int monthT = getMonthAsInteger();
            int dayOfTheMonthT = getDayOfTheMonth();
            int dayI = getDayOfTheWeekAsInteger();
            int febOrRest = 0;


            bool blnDirection = direction(
                dayOfTheMonthT,
                monthT,
                yearT,
                dayOfTheMonth,
                month,
                year);

            if (blnDirection)
            {
                bool test = true;
                while (test)
                {
                    if (yearT == year && monthT == month && dayOfTheMonthT == dayOfTheMonth)
                    {
                        dayOfDate = days[dayI - 1];
                        test = false;
                    }
                    else
                    {
                        dayOfTheMonthT++;
                        febOrRest = monthDays[monthT - 1];
                        if (leapYear(yearT) && monthT == 2)
                        {
                            febOrRest++;
                        }
                        if (dayOfTheMonthT > febOrRest)
                        {
                            dayOfTheMonthT -= febOrRest;
                            monthT++;
                        }
                        if (monthT == 13)
                        {
                            monthT = 1;
                            yearT++;
                        }
                        dayI++;
                        if (dayI == 8)
                        {
                            dayI = 1;
                        }
                    }
                }
            }
            else
            {
                bool test = true;
                while (test)
                {
                    if (yearT == year && monthT == month && dayOfTheMonthT == dayOfTheMonth)
                    {
                        dayOfDate = days[dayI - 1];
                        test = false;
                    }
                    else
                    {
                        dayOfTheMonthT--;
                        int monthIndex = monthT - 2;
                        if (monthIndex < 0)
                        {
                            monthIndex = 11;
                        }
                        febOrRest = monthDays[monthIndex];
                        if (leapYear(yearT) && monthT == 3)
                        {
                            febOrRest++;
                        }
                        if (dayOfTheMonthT == 0)
                        {
                            dayOfTheMonthT = febOrRest;
                            monthT--;
                        }
                        if (monthT == 0)
                        {
                            monthT = 12;
                            yearT--;
                        }
                        dayI--;
                        if (dayI == 0)
                        {
                            dayI = 7;
                        }
                    }
                }
            }


            return dayOfDate;
        }

        // Returns date of next Easter Sunday  (1700 - 2299)
        // Western Church   - Gregorian calendar
        // Uses the 'BBC algorithm' (http://www.bbc.co.uk/dna/h2g2/A653267) - checked only between 1700 and 2299
        public string easterSunday()
        {
            int year = getYear();
            if (year > 2299)
            {
                PrintToScreen.WriteLine(year +
                                  " is outside the range for which this algorithm has been checked, 1700 - 2299");
            }
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();

            int rem1 = year%19;
            int quo1 = year/100;
            int rem2 = year%100;
            int quo2 = quo1/4;
            int rem3 = quo1%4;
            int quo3 = rem2/4;
            int rem4 = rem2%4;

            int quo4 = (quo1 + 8)/25;
            int quo5 = (quo1 - quo4 + 1)/3;
            int rem5 = (19*rem1 + quo1 - quo2 - quo5 + 15)%30;
            int rem6 = (32 + 2*rem3 + 2*quo3 - rem5 - rem4)%7;
            int quo6 = (rem1 + 11*rem5 + 22*rem6)/451;
            int sum1 = rem5 + rem6 - 7*quo6 + 114;

            easterMonth = sum1/31;
            easterDay = (sum1%31) + 1;

            bool direction_ = direction(day, month, year, easterDay, easterMonth, year);
            if (direction_)
            {
                dayHold = easterDay;
                monthHold = easterMonth;
                yearHold = year;
                dayNameHold = "Sunday";
                easterDayName = getDayOfDate(easterDay, easterMonth, year);
                return easterDayName + ", " + easterDay + " " + months[easterMonth - 1] + " " + year;
            }
            else
            {
                return easterSunday(++year);
            }
        }

        // Returns date of the Easter Sunday  (1700 - 2299)
        // Western Church   - Gregorian calendar
        // Uses the 'BBC algorithm' (http://www.bbc.co.uk/dna/h2g2/A653267) - checked only between 1700 and 2299
        public string easterSunday(int year)
        {
            if (year < 1700 || year > 2299)
            {
                PrintToScreen.WriteLine(year +
                                  " is outside the range for which this algorithm has been checked, 1700 - 2299");
            }

            int rem1 = year%19;
            int quo1 = year/100;
            int rem2 = year%100;
            int quo2 = quo1/4;
            int rem3 = quo1%4;
            int quo3 = rem2/4;
            int rem4 = rem2%4;

            int quo4 = (quo1 + 8)/25;
            int quo5 = (quo1 - quo4 + 1)/3;
            int rem5 = (19*rem1 + quo1 - quo2 - quo5 + 15)%30;
            int rem6 = (32 + 2*rem3 + 2*quo3 - rem5 - rem4)%7;
            int quo6 = (rem1 + 11*rem5 + 22*rem6)/451;
            int sum1 = rem5 + rem6 - 7*quo6 + 114;

            easterMonth = sum1/31;
            easterDay = (sum1%31) + 1;
            dayHold = easterDay;
            monthHold = easterMonth;
            yearHold = year;
            dayNameHold = "Sunday";

            easterDayName = getDayOfDate(easterDay, easterMonth, year);

            return easterDayName + ", " + easterDay + " " + months[easterMonth - 1] + " " + year;
        }

        // Returns date of next Good Friday
        // See easterDay() for limitations of the method
        public string goodFriday()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            easterSunday(year);
            int monthGF = easterMonth;
            int dayGF = easterDay - 2;
            if (dayGF < 1)
            {
                int dayCheck = monthDays[monthGF - 2];
                if (leapYear(year) && monthGF == 3)
                {
                    dayCheck++;
                }
                dayGF = dayCheck + dayGF;
                monthGF--;
            }
            bool direction_ = direction(day, month, year, dayGF, monthGF, year);
            if (!direction_)
            {
                year++;
            }
            return goodFriday(year);
        }

        // Returns date of Good Friday for the entered year
        // See easterDay() for limitations of the method
        public string goodFriday(int year)
        {
            easterSunday(year);
            int monthGF = easterMonth;
            int dayGF = easterDay - 2;
            if (dayGF < 1)
            {
                int dayCheck = monthDays[monthGF - 2];
                if (leapYear(year) && monthGF == 3)
                {
                    dayCheck++;
                }
                dayGF = dayCheck + dayGF;
                monthGF--;
            }
            dayHold = dayGF;
            monthHold = monthGF;
            yearHold = year;
            dayNameHold = "Friday";
            return "Friday, " + dayGF + " " + months[monthGF - 1] + " " + year;
        }

        // Returns date of next Maundy Thursday
        // See easterDay() for limitations of the method
        public string maundyThursday()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            maundyThursday(year);
            int monthMT = monthHold;
            int dayMT = dayHold;
            bool direction_ = direction(day, month, year, dayMT, monthMT, year);
            if (!direction_)
            {
                year++;
            }
            return maundyThursday(year);
        }

        // Returns date of Maundy Thursday for the entered year
        // See easterDay() for limitations of the method
        public string maundyThursday(int year)
        {
            goodFriday(year);
            int monthMT = monthHold;
            int dayMT = dayHold - 1;
            if (dayMT < 1)
            {
                int dayCheck = monthDays[monthMT - 2];
                if (leapYear(year) && monthMT == 3)
                {
                    dayCheck++;
                }
                dayMT = dayCheck + dayMT;
                monthMT--;
            }
            dayHold = dayMT;
            monthHold = monthMT;
            yearHold = year;
            dayNameHold = "Friday";
            return "Thursday, " + dayMT + " " + months[monthMT - 1] + " " + year;
        }

        // Returns date of next Ash Wednesday
        // See easterDay() for limitations of the method
        public string ashWednesday()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            ashWednesday(year);
            int monthAW = monthHold;
            int dayAW = dayHold;
            bool direction_ = direction(day, month, year, dayAW, monthAW, year);
            if (!direction_)
            {
                year++;
            }
            return ashWednesday(year);
        }

        // Returns date of Ash Wednesday for the entered year
        // See easterDay() for limitations of the method
        public string ashWednesday(int year)
        {
            easterSunday(year);
            int monthAW = easterMonth;
            int dayAW = easterDay;
            int counter = 1;
            while (counter <= 40)
            {
                dayAW--;
                if (dayAW < 1)
                {
                    int dayCheck = monthDays[monthAW - 2];
                    if (leapYear(year) && monthAW == 3)
                    {
                        dayCheck++;
                    }
                    dayAW = dayCheck + dayAW;
                    monthAW--;
                }
                if (getDayOfDate(dayAW, monthAW, year).Equals("Sunday"))
                {
                    // Sunday - day does not counts
                }
                else
                {
                    // Not a Sunday - day counts
                    counter++;
                }
            }
            dayHold = dayAW;
            monthHold = monthAW;
            yearHold = year;
            dayNameHold = "Wednesday";

            return "Wednesday, " + dayAW + " " + months[monthAW - 1] + " " + year;
        }

        // Returns date of next Shrove Tuesday
        // See easterDay() for limitations of the method
        public string shroveTuesday()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            shroveTuesday(year);
            int monthST = monthHold;
            int dayST = dayHold;
            bool direction_ = direction(day, month, year, dayST, monthST, year);
            if (!direction_)
            {
                year++;
            }
            return shroveTuesday(year);
        }

        // Returns date of Shrove Tuesday for the entered year
        // See easterDay() for limitations of the method
        public string shroveTuesday(int year)
        {
            ashWednesday(year);
            int monthST = monthHold;
            int dayST = dayHold - 1;
            if (dayST < 1)
            {
                int dayCheck = monthDays[monthST - 2];
                if (leapYear(year) && monthST == 3)
                {
                    dayCheck++;
                }
                dayST = dayCheck + dayST;
                monthST--;
            }
            dayHold = dayST;
            monthHold = monthST;
            yearHold = year;
            dayNameHold = "Tuesday";

            return "Tuesday, " + dayST + " " + months[monthST - 1] + " " + year;
        }

        // Returns date of next Palm Sunday
        // See easterDay() for limitations of the method
        public string palmSunday()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            palmSunday(year);
            int monthPS = monthHold;
            int dayPS = dayHold;
            bool direction_ = direction(day, month, year, dayPS, monthPS, year);
            if (!direction_)
            {
                year++;
            }
            return palmSunday(year);
        }

        // Returns date of Palm Sunday for the entered year
        // See easterDay() for limitations of the method
        public string palmSunday(int year)
        {
            easterSunday(year);
            int monthPS = easterMonth;
            int dayPS = easterDay - 7;
            if (dayPS < 1)
            {
                int dayCheck = monthDays[monthPS - 2];
                if (leapYear(year) && monthPS == 3)
                {
                    dayCheck++;
                }
                dayPS = dayCheck + dayPS;
                monthPS--;
            }
            dayHold = dayPS;
            monthHold = monthPS;
            yearHold = year;
            dayNameHold = "Sunday";

            return "Sunday, " + dayPS + " " + months[monthPS - 1] + " " + year;
        }

        // Returns date of next Advent Sunday
        // See easterDay() for limitations of the method
        public string adventSunday()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            adventSunday(year);
            int monthPS = monthHold;
            int dayPS = dayHold;
            bool direction_ = direction(day, month, year, dayPS, monthPS, year);
            if (!direction_)
            {
                year++;
            }
            return adventSunday(year);
        }

        // Returns date of Advent Sunday for the entered year
        // See easterDay() for limitations of the method
        public string adventSunday(int year)
        {
            saintAndrewsDay(year);
            int monthAS = monthHold;
            int dayAS = dayHold;
            string dayNameAS = dayNameHold;
            int dayASI = getDayOfTheWeekAsInteger(dayNameAS);
            if (dayASI < 4)
            {
                dayAS -= dayASI;
                if (dayAS < 1)
                {
                    int dayCheck = monthDays[monthAS - 2];
                    if (leapYear(year) && monthAS == 3)
                    {
                        dayCheck++;
                    }
                    dayAS = dayCheck + dayAS;
                    monthAS--;
                }
            }
            else
            {
                dayAS += (7 - dayASI);
                int dayCheck = monthDays[monthAS - 1];
                if (leapYear(year) && monthAS == 2)
                {
                    dayCheck++;
                }
                if (dayAS > dayCheck)
                {
                    dayAS = dayAS - dayCheck;
                    monthAS++;
                }
            }

            dayHold = dayAS;
            monthHold = monthAS;
            yearHold = year;
            dayNameHold = "Sunday";

            return "Sunday, " + dayAS + " " + months[monthAS - 1] + " " + year;
        }


        // Returns date of next Trinity Sunday
        // See easterDay() for limitations of the method
        public string trinitySunday()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            trinitySunday(year);
            int monthTS = monthHold;
            int dayTS = dayHold;
            bool direction_ = direction(day, month, year, dayTS, monthTS, year);
            if (!direction_)
            {
                year++;
            }
            return trinitySunday(year);
        }

        // Returns date of Trinity Sunday for the entered year
        // See easterDay() for limitations of the method
        public string trinitySunday(int year)
        {
            whitSunday(year);
            int monthTS = monthHold;
            int dayTS = dayHold + 7;
            int dayCheck = monthDays[monthTS - 1];
            if (leapYear(year) && monthTS == 2)
            {
                dayCheck++;
            }
            if (dayTS > dayCheck)
            {
                dayTS = dayTS - dayCheck;
                monthTS++;
            }
            dayHold = dayTS;
            monthHold = monthTS;
            yearHold = year;
            dayNameHold = "Sunday";

            return "Sunday, " + dayTS + " " + months[monthTS - 1] + " " + year;
        }

        // Returns date of next Corpus Christi
        // See easterDay() for limitations of the method
        public string corpusChristi()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            corpusChristi(year);
            int monthTS = monthHold;
            int dayTS = dayHold;
            bool direction_ = direction(day, month, year, dayTS, monthTS, year);
            if (!direction_)
            {
                year++;
            }
            return corpusChristi(year);
        }

        // Returns date of Corpus Christi for the entered year
        // See easterDay() for limitations of the method
        public string corpusChristi(int year)
        {
            trinitySunday(year);
            int monthTS = monthHold;
            int dayTS = dayHold + 4;
            int dayCheck = monthDays[monthTS - 1];
            if (leapYear(year) && monthTS == 2)
            {
                dayCheck++;
            }
            if (dayTS > dayCheck)
            {
                dayTS = dayTS - dayCheck;
                monthTS++;
            }
            dayHold = dayTS;
            monthHold = monthTS;
            yearHold = year;
            dayNameHold = "Thursday";

            return "Thursday, " + dayTS + " " + months[monthTS - 1] + " " + year;
        }

        // Returns date of next Sunday after Corpus Christi
        // See easterDay() for limitations of the method
        public string sundayAfterCorpusChristi()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            sundayAfterCorpusChristi(year);
            int monthTS = monthHold;
            int dayTS = dayHold;
            bool direction_ = direction(day, month, year, dayTS, monthTS, year);
            if (!direction_)
            {
                year++;
            }
            return sundayAfterCorpusChristi(year);
        }

        // Returns date of Sunday after Corpus Christi for the entered year
        // See easterDay() for limitations of the method
        public string sundayAfterCorpusChristi(int year)
        {
            corpusChristi(year);
            int monthTS = monthHold;
            int dayTS = dayHold + 3;
            int dayCheck = monthDays[monthTS - 1];
            if (leapYear(year) && monthTS == 2)
            {
                dayCheck++;
            }
            if (dayTS > dayCheck)
            {
                dayTS = dayTS - dayCheck;
                monthTS++;
            }
            dayHold = dayTS;
            monthHold = monthTS;
            yearHold = year;
            dayNameHold = "Sunday";

            return "Sunday, " + dayTS + " " + months[monthTS - 1] + " " + year;
        }

        // Returns date of next Ascension Thursday
        // See easterDay() for limitations of the method
        public string ascensionThursday()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            ascensionThursday(year);
            int monthAT = monthHold;
            int dayAT = dayHold;
            bool direction_ = direction(day, month, year, dayAT, monthAT, year);
            if (!direction_)
            {
                year++;
            }
            return ascensionThursday(year);
        }

        // Returns date of Ascension Thursday for the entered year
        // See easterDay() for limitations of the method
        public string ascensionThursday(int year)
        {
            easterSunday(year);
            int monthAT = easterMonth;
            int dayAT = easterDay + 39;
            int dayCheck1 = monthDays[monthAT - 1];
            if (leapYear(year) && monthAT == 2)
            {
                dayCheck1++;
            }
            int dayCheck2 = monthDays[monthAT];
            if (leapYear(year) && monthAT == 1)
            {
                dayCheck2++;
            }
            if (dayAT > (dayCheck1 + dayCheck2))
            {
                dayAT = dayAT - (dayCheck1 + dayCheck2);
                monthAT += 2;
            }
            else
            {
                if (dayAT > dayCheck1)
                {
                    dayAT = dayAT - dayCheck1;
                    monthAT += 1;
                }
            }

            dayHold = dayAT;
            monthHold = monthAT;
            yearHold = year;
            dayNameHold = "Thursday";

            return "Thursday, " + dayAT + " " + months[monthAT - 1] + " " + year;
        }

        // Returns date of next Sunday after Ascension Thursday
        // See easterDay() for limitations of the method
        public string sundayAfterAscension()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            sundayAfterAscension(year);
            int monthAT = monthHold;
            int dayAT = dayHold;
            bool direction_ = direction(day, month, year, dayAT, monthAT, year);
            if (!direction_)
            {
                year++;
            }
            return sundayAfterAscension(year);
        }

        // Returns date of Sunday after Ascension Thursday for the entered year
        // See easterDay() for limitations of the method
        public string sundayAfterAscension(int year)
        {
            ascensionThursday(year);
            int monthAT = monthHold;
            int dayAT = dayHold + 3;
            int dayCheck1 = monthDays[monthAT - 1];
            if (leapYear(year) && monthAT == 2)
            {
                dayCheck1++;
            }
            if (dayAT > dayCheck1)
            {
                dayAT = dayAT - dayCheck1;
                monthAT += 1;
            }

            dayHold = dayAT;
            monthHold = monthAT;
            yearHold = year;
            dayNameHold = "Sunday";

            return "Sunday, " + dayAT + " " + months[monthAT - 1] + " " + year;
        }

        // Returns date of next Whit Sunday (Pentecost)
        // See easterDay() for limitations of the method
        public string whitSunday()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            whitSunday(year);
            int monthWS = monthHold;
            int dayWS = dayHold;
            bool direction_ = direction(day, month, year, dayWS, monthWS, year);
            if (!direction_)
            {
                year++;
            }
            return whitSunday(year);
        }

        // Returns date of Whit Sunday (Pentecost)for the entered year
        // See easterDay() for limitations of the method
        public string whitSunday(int year)
        {
            easterSunday(year);
            int dayWS = easterDay + 49;
            int monthWS = easterMonth;
            int dayCheck1 = monthDays[monthWS - 1];
            if (leapYear(year) && monthWS == 2)
            {
                dayCheck1++;
            }
            int dayCheck2 = monthDays[monthWS];
            if (leapYear(year) && monthWS == 1)
            {
                dayCheck2++;
            }

            if (dayWS > (dayCheck1 + dayCheck2))
            {
                dayWS -= (dayCheck1 + dayCheck2);
                monthWS += 2;
            }
            else
            {
                if (dayWS > dayCheck1)
                {
                    dayWS -= dayCheck1;
                    monthWS += 1;
                }
            }
            dayHold = dayWS;
            monthHold = monthWS;
            yearHold = year;
            dayNameHold = "Sunday";

            return "Sunday, " + dayWS + " " + months[monthHold - 1] + " " + year;
        }

        // Returns date of next Mother's Day (Mothering Sunday) in the UK
        // See easterDay() for limitations of the method
        public string mothersDayUK()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            mothersDayUK(year);
            int monthMS = monthHold;
            int dayMS = dayHold;
            bool direction_ = direction(day, month, year, dayMS, monthMS, year);
            if (!direction_)
            {
                year++;
            }
            return mothersDayUK(year);
        }

        // Returns date of Mother's Day (Mothering Sunday) in the UK for the entered year
        // See easterDay() for limitations of the method
        public string mothersDayUK(int year)
        {
            ashWednesday(year);
            int dayMS = dayHold + 25;
            int monthMS = monthHold;
            int dayCheck = monthDays[monthMS - 1];
            if (leapYear(year) && monthMS == 2)
            {
                dayCheck++;
            }
            if (dayMS > dayCheck)
            {
                dayMS -= dayCheck;
                monthMS++;
            }
            dayHold = dayMS;
            monthHold = monthMS;
            yearHold = year;
            dayNameHold = "Sunday";

            return "Sunday, " + dayMS + " " + months[monthHold - 1] + " " + year;
        }

        // Returns date of next Mothers Day in the US
        public string mothersDayUS()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            mothersDayUS(year);
            bool direction_ = direction(day, month, year, dayHold, monthHold, year);
            if (!direction_)
            {
                year++;
            }
            return mothersDayUS(year);
        }

        // Returns date of Mother's Day (Mothering Sunday) in the US for the entered year
        public string mothersDayUS(int year)
        {
            string dayMSN = getDayOfDate(1, "May", year);
            int monthMS = 5;
            int dayOwI = getDayOfTheWeekAsInteger(dayMSN) + 1;
            int dayMS = 0;
            if (dayOwI == 1)
            {
                dayMS = dayOwI + 7;
            }
            else
            {
                dayMS = 16 - dayOwI;
            }
            dayHold = dayMS;
            monthHold = monthMS;
            yearHold = year;
            dayNameHold = "Sunday";

            return "Sunday, " + dayMS + " May " + year;
        }

        // Returns date of next Father's Day
        public string fathersDay()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            fathersDay(year);
            bool direction_ = direction(day, month, year, dayHold, monthHold, year);
            if (!direction_)
            {
                year++;
            }
            return fathersDay(year);
        }

        // Returns date of Father's Day  for the entered year
        public string fathersDay(int year)
        {
            string dayMSN = getDayOfDate(1, "June", year);
            int monthFD = 6;
            int dayOwI = getDayOfTheWeekAsInteger(dayMSN) + 1;
            int dayFD = 0;
            if (dayOwI == 1)
            {
                dayFD = dayOwI + 14;
            }
            else
            {
                dayFD = 23 - dayOwI;
            }
            dayHold = dayFD;
            monthHold = monthFD;
            yearHold = year;
            dayNameHold = "Sunday";

            return "Sunday, " + dayFD + " June " + year;
        }

        // Returns date of next Christmas Day
        public string christmasDay()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            bool direction_ = direction(day, month, year, 25, 12, year);
            if (!direction_)
            {
                year++;
            }
            return christmasDay(year);
        }

        // Returns date of Christmas Day for the entered year
        public string christmasDay(int year)
        {
            string day = getDayOfDate(25, 12, year);
            dayHold = 25;
            monthHold = 12;
            yearHold = year;
            dayNameHold = day;
            return day + ", 25 December " + year;
        }

        // Returns date of next New Year's Day
        public string newYearsDay()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            bool direction_ = direction(day, month, year, 1, 1, year);
            if (!direction_)
            {
                year++;
            }
            return newYearsDay(year);
        }

        // Returns date of New Year's Day for the entered year
        public string newYearsDay(int year)
        {
            string day = getDayOfDate(1, 1, year);
            dayHold = 1;
            monthHold = 1;
            yearHold = year;
            dayNameHold = day;
            return day + ", 1 January " + year;
        }

        // Returns date of next Epiphany day
        public string epiphany()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            bool direction_ = direction(day, month, year, 6, 1, year);
            if (!direction_)
            {
                year++;
            }
            return epiphany(year);
        }

        // Returns date of Epiphany day for the entered year
        public string epiphany(int year)
        {
            string day = getDayOfDate(6, 1, year);
            dayHold = 6;
            monthHold = 1;
            yearHold = year;
            dayNameHold = day;
            return day + ", 6 January " + year;
        }

        // Returns date of next Sunday after Epiphany day
        public string sundayAfterEpiphany()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            string dayName = getDayOfDate(6, 1, year);
            int dayI = getDayOfTheWeekAsInteger(dayName);
            int day6plus = 6;
            if (dayI > 0)
            {
                day6plus += (7 - dayI);
            }
            bool direction_ = direction(day, month, year, day6plus, 1, year);
            if (!direction_)
            {
                year++;
            }
            return sundayAfterEpiphany(year);
        }

        // Returns date of Sunday after Epiphany day for the entered year
        public string sundayAfterEpiphany(int year)
        {
            string dayName = getDayOfDate(6, 1, year);
            int dayI = getDayOfTheWeekAsInteger(dayName);
            int day6plus = 6;
            if (dayI > 0)
            {
                day6plus += (7 - dayI);
            }
            dayHold = day6plus;
            monthHold = 1;
            yearHold = year;
            dayNameHold = "Sunday";

            return "Sunday, " + day6plus + " January " + year;
        }

        // Returns date of the next Feast of the Annunciation
        public string annunciation()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            bool direction_ = direction(day, month, year, 25, 3, year);
            if (!direction_)
            {
                year++;
            }
            return annunciation(year);
        }

        // Returns date of the Feast of the Annunciation for the entered year
        public string annunciation(int year)
        {
            string day = getDayOfDate(25, 3, year);
            dayHold = 25;
            monthHold = 3;
            yearHold = year;
            dayNameHold = day;
            return day + ", 25 March " + year;
        }

        // Returns date of the next Feast of the Assumption
        public string assumption()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            bool direction_ = direction(day, month, year, 15, 8, year);
            if (!direction_)
            {
                year++;
            }
            return assumption(year);
        }

        // Returns date of the Feast of the Assumption for the entered year
        public string assumption(int year)
        {
            string day = getDayOfDate(15, 8, year);
            dayHold = 15;
            monthHold = 8;
            yearHold = year;
            dayNameHold = day;
            return day + ", 15 August " + year;
        }

        // Returns date of the next Feast of the Nativity of the Blessed Virgin
        public string nativityBlessedVirgin()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            bool direction_ = direction(day, month, year, 8, 9, year);
            if (!direction_)
            {
                year++;
            }
            return nativityBlessedVirgin(year);
        }

        // Returns date of the Feast of the Nativity of the Blessed Virgin for the entered year
        public string nativityBlessedVirgin(int year)
        {
            string day = getDayOfDate(8, 9, year);
            dayHold = 8;
            monthHold = 9;
            yearHold = year;
            dayNameHold = day;
            return day + ", 8 September " + year;
        }

        // Returns date of the next Feast of the Immaculate Conception
        public string immaculateConception()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            bool direction_ = direction(day, month, year, 8, 12, year);
            if (!direction_)
            {
                year++;
            }
            return immaculateConception(year);
        }

        // Returns date of the Feast of the Immaculate Conception for the entered year
        public string immaculateConception(int year)
        {
            string day = getDayOfDate(8, 12, year);
            dayHold = 8;
            monthHold = 12;
            yearHold = year;
            dayNameHold = day;
            return day + ", 8 December " + year;
        }

        // Returns date of the next Feast of the Purification of the Virgin
        // [Candlemas, Feast of the Presentation of Jesus at the Temple]
        public string purification()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            bool direction_ = direction(day, month, year, 2, 2, year);
            if (!direction_)
            {
                year++;
            }
            return purification(year);
        }

        public string presentation()
        {
            return purification();
        }

        public string candlemas()
        {
            return purification();
        }

        // Returns date of the next Feast of the Purification of the Virgin
        // [Candlemas, Feast of the Presentation of Jesus at the temple]
        public string purification(int year)
        {
            string day = getDayOfDate(2, 2, year);
            dayHold = 2;
            monthHold = 2;
            yearHold = year;
            dayNameHold = day;
            return day + ", 2 February " + year;
        }

        public string presentation(int year)
        {
            return purification(year);
        }

        public string candlemas(int year)
        {
            return purification(year);
        }

        // Returns date of the next Feast of the Transfiguration of Christ
        public string transfiguration()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            bool direction_ = direction(day, month, year, 6, 8, year);
            if (!direction_)
            {
                year++;
            }
            return transfiguration(year);
        }

        // Returns date of the Feast of the Transfiguration of Christ for the entered year
        public string transfiguration(int year)
        {
            string day = getDayOfDate(6, 8, year);
            dayHold = 6;
            monthHold = 8;
            yearHold = year;
            dayNameHold = day;
            return day + ", 6 August " + year;
        }

        // Returns date of next Remembrance Sunday
        public string remembranceSunday()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            remembranceSunday(year);
            int monthRS = monthHold;
            int dayRS = dayHold;
            bool direction_ = direction(day, month, year, dayRS, monthRS, year);
            if (!direction_)
            {
                year++;
            }
            return remembranceSunday(year);
        }

        // Returns date of Remembrance Sunday for the entered year
        public string remembranceSunday(int year)
        {
            int monthRS = 11;
            int dayRS = 11;
            string dayNameRS = getDayOfDate(11, 11, year);
            int dayRSI = getDayOfTheWeekAsInteger(dayNameRS);
            if (dayRSI < 4)
            {
                dayRS -= dayRSI;
                if (dayRS < 1)
                {
                    int dayCheck = monthDays[monthRS - 2];
                    if (leapYear(year) && monthRS == 3)
                    {
                        dayCheck++;
                    }
                    dayRS = dayCheck + dayRS;
                    monthRS--;
                }
            }
            else
            {
                dayRS += (7 - dayRSI);
                int dayCheck = monthDays[monthRS - 1];
                if (leapYear(year) && monthRS == 2)
                {
                    dayCheck++;
                }
                if (dayRS > dayCheck)
                {
                    dayRS = dayRS - dayCheck;
                    monthRS++;
                }
            }

            dayHold = dayRS;
            monthHold = monthRS;
            yearHold = year;
            dayNameHold = "Sunday";

            return "Sunday, " + dayRS + " " + months[monthRS - 1] + " " + year;
        }


        // Returns date of next Holocaust Memorial Day
        public string holocaustMemorialDay()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            bool direction_ = direction(day, month, year, 27, 1, year);
            if (!direction_)
            {
                year++;
            }
            return holocaustMemorialDay(year);
        }

        // Returns date of Holocaust Memorial Day for the entered year
        public string holocaustMemorialDay(int year)
        {
            string day = getDayOfDate(27, 1, year);
            dayHold = 25;
            monthHold = 12;
            yearHold = year;
            dayNameHold = day;
            return day + ", 27 January " + year;
        }

        // Returns date of next St Patrick's Day
        public string saintPatricksDay()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            bool direction_ = direction(day, month, year, 17, 3, year);
            if (!direction_)
            {
                year++;
            }
            return saintPatricksDay(year);
        }

        // Returns date of St Patrick's Day for the entered year
        public string saintPatricksDay(int year)
        {
            string day = getDayOfDate(17, 3, year);
            dayHold = 17;
            monthHold = 3;
            yearHold = year;
            dayNameHold = day;
            return day + ", 17 March " + year;
        }

        // Returns date of next St Brigid's Day
        public string saintBrigidsDay()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            bool direction_ = direction(day, month, year, 1, 2, year);
            if (!direction_)
            {
                year++;
            }
            return saintBrigidsDay(year);
        }

        // Returns date of St Brigid's Day for the entered year
        public string saintBrigidsDay(int year)
        {
            string day = getDayOfDate(1, 2, year);
            dayHold = 1;
            monthHold = 2;
            yearHold = year;
            dayNameHold = day;
            return day + ", 1 February " + year;
        }

        // Returns date of next St Colm Cille's (St Columba's)Day
        public string saintColmCillesDay()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            bool direction_ = direction(day, month, year, 9, 6, year);
            if (!direction_)
            {
                year++;
            }
            return saintColmCillesDay(year);
        }

        public string saintColumbasDay()
        {
            return saintColmCillesDay();
        }

        public string saintColmcillesDay()
        {
            return saintColmCillesDay();
        }

        // Returns date of St Colm Cille's (St Columba's) Day for the entered year
        public string saintColmCillesDay(int year)
        {
            string day = getDayOfDate(9, 6, year);
            dayHold = 9;
            monthHold = 6;
            yearHold = year;
            dayNameHold = day;
            return day + ", 9 June " + year;
        }

        public string saintColumbasDay(int year)
        {
            return saintColmCillesDay(year);
        }

        public string saintColmcillesDay(int year)
        {
            return saintColmCillesDay(year);
        }

        // Returns date of next St Georges's day
        public string saintGeorgesDay()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            bool direction_ = direction(day, month, year, 23, 4, year);
            if (!direction_)
            {
                year++;
            }
            return saintGeorgesDay(year);
        }

        // Returns date of St George's day for the entered year
        public string saintGeorgesDay(int year)
        {
            string day = getDayOfDate(23, 4, year);
            dayHold = 23;
            monthHold = 4;
            yearHold = year;
            dayNameHold = day;
            return day + ", 23 April " + year;
        }

        // Returns date of next St Andrew's day
        public string saintAndrewsDay()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            bool direction_ = direction(day, month, year, 30, 11, year);
            if (!direction_)
            {
                year++;
            }
            return saintAndrewsDay(year);
        }

        // Returns date of St Andrew's day for the entered year
        public string saintAndrewsDay(int year)
        {
            string day = getDayOfDate(30, 11, year);
            dayHold = 30;
            monthHold = 11;
            yearHold = year;
            dayNameHold = day;
            return day + ", 30 November " + year;
        }

        // Returns date of next St David's day
        public string saintDavidsDay()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            bool direction_ = direction(day, month, year, 1, 3, year);
            if (!direction_)
            {
                year++;
            }
            return saintDavidsDay(year);
        }

        // Returns date of St David's day for the entered year
        public string saintDavidsDay(int year)
        {
            string day = getDayOfDate(1, 3, year);
            dayHold = 1;
            monthHold = 3;
            yearHold = year;
            dayNameHold = day;
            return day + ", 1 March " + year;
        }

        // Returns date of next St Stephen's day
        public string saintStephensDay()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            bool direction_ = direction(day, month, year, 26, 12, year);
            if (!direction_)
            {
                year++;
            }
            return saintStephensDay(year);
        }

        // Returns date of St Stephen's day for the entered year
        public string saintStephensDay(int year)
        {
            string day = getDayOfDate(26, 12, year);
            dayHold = 26;
            monthHold = 12;
            yearHold = year;
            dayNameHold = day;
            return day + ", 26 December " + year;
        }

        // Returns date of next St Valentine's day
        public string saintValentinesDay()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            bool direction_ = direction(day, month, year, 14, 2, year);
            if (!direction_)
            {
                year++;
            }
            return saintValentinesDay(year);
        }

        // Returns date of St Valentines's day for the entered year
        public string saintValentinesDay(int year)
        {
            string day = getDayOfDate(14, 2, year);
            dayHold = 14;
            monthHold = 2;
            yearHold = year;
            dayNameHold = day;
            return day + ", 14 February " + year;
        }


        // Returns date of next Burns' night
        public string burnsNight()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            bool direction_ = direction(day, month, year, 25, 1, year);
            if (!direction_)
            {
                year++;
            }
            return burnsNight(year);
        }

        // Returns date of Burns night for the entered year
        public string burnsNight(int year)
        {
            string day = getDayOfDate(25, 1, year);
            dayHold = 25;
            monthHold = 1;
            yearHold = year;
            dayNameHold = day;
            return day + ", 25 January " + year;
        }

        // Returns date of next Twelfth of July
        public string twelfthJuly()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            bool direction_ = direction(day, month, year, 12, 7, year);
            if (!direction_)
            {
                year++;
            }
            return twelfthJuly(year);
        }

        // Returns date of the Twelfth of July for the entered year
        public string twelfthJuly(int year)
        {
            string day = getDayOfDate(12, 7, year);
            dayHold = 12;
            monthHold = 7;
            yearHold = year;
            dayNameHold = day;
            return day + ", 12 July " + year;
        }

        // Returns date of next Fourth of July (US Independence Day)
        public string fourthJuly()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            bool direction_ = direction(day, month, year, 12, 7, year);
            if (!direction_)
            {
                year++;
            }
            return fourthJuly(year);
        }

        // Returns date of the Fourth of July (US Independence Day)for the entered year
        public string fourthJuly(int year)
        {
            string day = getDayOfDate(4, 7, year);
            dayHold = 4;
            monthHold = 7;
            yearHold = year;
            dayNameHold = day;
            return day + ", 4 July " + year;
        }

        // Returns date of next US Thanksgiving Day
        public string thanksgivingDay()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();

            string day1 = getDayOfDate(1, "November", year);
            int day1I = getDayOfTheWeekAsInteger(day1) + 1;
            int day2I = 6 - day1I;
            if (day2I <= 0)
            {
                day2I += 7;
            }
            day2I += 14;
            bool direction_ = direction(day, month, year, day2I, 11, year);
            if (direction_)
            {
                return "Thursday, " + day2I + " November " + year;
            }
            else
            {
                return thanksgivingDay(++year);
            }
        }

        // Returns date of the US Thanksgiving Day
        public string thanksgivingDay(int year)
        {
            string day1 = getDayOfDate(1, "November", year);
            int day1I = getDayOfTheWeekAsInteger(day1) + 1;
            int day2I = 6 - day1I;
            if (day2I <= 0)
            {
                day2I += 7;
            }
            day2I += 14;
            dayHold = day2I;
            monthHold = 11;
            yearHold = year;
            dayNameHold = "Thursday";
            return "Thursday, " + day2I + " November " + year;
        }

        // Returns date of next Commonwealth Day
        public string commonwealthDay()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();

            string day1 = getDayOfDate(1, "March", year);
            int day1I = getDayOfTheWeekAsInteger(day1);

            int day2I = 0;
            if (day1I > 1)
            {
                day2I = 15 - day1I;
            }
            else
            {
                if (day1I == 0)
                {
                    day2I = 8;
                }
                else
                {
                    day2I = 9;
                }
            }
            bool direction_ = direction(day, month, year, day2I, 3, year);
            if (direction_)
            {
                dayHold = day2I;
                monthHold = 3;
                yearHold = year;
                dayNameHold = "Monday";
                return "Monday, " + day2I + " November " + year;
            }
            else
            {
                return commonwealthDay(++year);
            }
        }

        // Returns date of the Commonwealth Day
        public string commonwealthDay(int year)
        {
            string day1 = getDayOfDate(1, "March", year);
            int day1I = getDayOfTheWeekAsInteger(day1);

            int day2I = 0;
            if (day1I > 1)
            {
                day2I = 16 - day1I;
            }
            else
            {
                if (day1I == 0)
                {
                    day2I = 9;
                }
                else
                {
                    day2I = 8;
                }
            }
            dayHold = day2I;
            monthHold = 3;
            yearHold = year;
            dayNameHold = "Monday";
            return "Monday, " + day2I + " March " + year;
        }

        // Returns date of next Armed Forces Day (UK Veterans' Day)
        public string armedForcesDay()
        {
            int year = getYear();
            int month = getMonthAsInteger();
            int day = getDayOfTheMonth();
            bool direction_ = direction(day, month, year, 27, 6, year);
            if (!direction_)
            {
                year++;
            }
            return armedForcesDay(year);
        }

        public string veteransDayUK()
        {
            return armedForcesDay();
        }

        // Returns date of the Armed Forces Day (UK Veterans' Day)for the entered year
        public string armedForcesDay(int year)
        {
            string day = getDayOfDate(27, 6, year);
            dayHold = 27;
            monthHold = 6;
            yearHold = year;
            dayNameHold = day;
            return day + ", 27 June " + year;
        }

        public string veteransDayUK(int year)
        {
            return armedForcesDay(year);
        }

        // Returns true if year (argument) is a leap year
        public bool leapYear(int year)
        {
            bool test = false;

            if (year%4 != 0)
            {
                test = false;
            }
            else
            {
                if (year%400 == 0)
                {
                    test = true;
                }
                else
                {
                    if (year%100 == 0)
                    {
                        test = false;
                    }
                    else
                    {
                        test = true;
                    }
                }
            }
            return test;
        }
    }
}


