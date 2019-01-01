using System;

namespace HC.Core.Time
{
    public class TimeRounding
    {
        public enum RoundingDirection
        {
            RoundUp,
            RoundDown,
            Round
        }

        public static void Test()
        {
            var t = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 15, 34, 12);
            DateTime roundD = RoundDateToSecondInterval(t, 5, RoundingDirection.RoundDown);
            DateTime roundU = RoundDateToSecondInterval(t, 5, RoundingDirection.RoundUp);
            TimeSpan d1 = t - roundD;
            TimeSpan d2 = roundU - t;


            DateTime result = d1 < d2 ? roundD : roundU;
            Console.WriteLine("t = {0} roundd = {1} roundu = {2} d1 = {3} d1 = {3} d2 = {4} result = {5}",
                              t,roundD,roundU,d1,d2,result);
            var t2 = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 10, 58, 45);
            DateTime result2 = RoundDateToSecondInterval(t2, 60);
            Console.WriteLine("t2 = {0} result2 = {1}", t2, result2);
        }

        public static DateTime RoundDateToSecondInterval(DateTime t, int secondInterval)
        {
            DateTime roundD = RoundDateToSecondInterval(t, secondInterval, RoundingDirection.RoundDown);
            DateTime roundU = RoundDateToSecondInterval(t, secondInterval, RoundingDirection.RoundUp);

            TimeSpan d1 = t - roundD;
            TimeSpan d2 = roundU - t;

            DateTime result = d1 < d2 ? roundD : roundU;

            return result;
        }

        public static DateTime RoundDateToSecondInterval(
            DateTime time, 
            int secondInterval, 
            RoundingDirection roundingDirection)
        {
            if (secondInterval == 0)
            {
                return time;
            }

            var interval = (decimal) secondInterval;
            var actSecond = (decimal) time.Second;

            if (actSecond == 0.00M)
            {
                return time;
            }
            int newSeconds = 0;

            switch (roundingDirection)
            {
                case RoundingDirection.Round:
                    newSeconds = (int) (Math.Round(actSecond/interval, 0)*interval);
                    break;
                case RoundingDirection.RoundDown:
                    newSeconds = (int) (Math.Truncate(actSecond/interval)*interval);
                    break;
                case RoundingDirection.RoundUp:
                    newSeconds = (int) (Math.Ceiling(actSecond/interval)*interval);
                    break;
            }
            time = time.AddSeconds(time.Second*-1);
            time = time.AddMilliseconds(time.Millisecond*-1);
            return time.AddSeconds(newSeconds);
        }

        public static DateTime RoundDateToMinuteInterval(
            DateTime time,
            int minuteInterval,
            RoundingDirection roundingDirection)
        {
            if (minuteInterval == 0)
            {
                return time;
            }

            var interval = (decimal)minuteInterval;
            var actMinute = (decimal)time.Second;

            if (actMinute == 0.00M)
            {
                return time;
            }
            int newMinutes = 0;

            switch (roundingDirection)
            {
                case RoundingDirection.Round:
                    newMinutes = (int)(Math.Round(actMinute / interval, 0) * interval);
                    break;
                case RoundingDirection.RoundDown:
                    newMinutes = (int)(Math.Truncate(actMinute / interval) * interval);
                    break;
                case RoundingDirection.RoundUp:
                    newMinutes = (int)(Math.Ceiling(actMinute / interval) * interval);
                    break;
            }
            time = time.AddMinutes(time.Minute * -1);
            time = time.AddSeconds(time.Second * -1);
            time = time.AddMilliseconds(time.Millisecond * -1);
            // add the minutures back
            return time.AddSeconds(newMinutes);
        }

        public static decimal RoundDateToMinuteInterval(
            decimal hours,
            int minuteInterval,
            RoundingDirection roundingDirection)
        {
            if (minuteInterval == 0)
            {
                return hours;
            }

            decimal fraction = (decimal) 60/minuteInterval;

            switch (roundingDirection)
            {
                case RoundingDirection.Round:
                    return (Math.Round(hours*fraction, 0)*fraction);
                case RoundingDirection.RoundDown:
                    return (Math.Truncate(hours*fraction)*fraction);
            }
            return Math.Ceiling(hours*fraction)/fraction;
        }
    }
}


