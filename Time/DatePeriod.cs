using System;
using System.Collections.Generic;

namespace HC.Core.Time
{
    public class DatePeriod : IComparer<DatePeriod>
    {
        #region Properties

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        #endregion

        #region Constructors

        public DatePeriod() { }

        public DatePeriod(
            DateTime startDate,
            DateTime endDate)
        {
            StartDate = startDate;
            EndDate = endDate;
        }

        public int Compare(DatePeriod x, DatePeriod y)
        {
            int intCompare = x.StartDate.CompareTo(y.StartDate);
            if(intCompare == 0)
            {
                return x.EndDate.CompareTo(y.EndDate);
            }
            return intCompare;
        }

        public override string ToString()
        {
            return DateHelper.ToDateTimeString(StartDate) + "%" +
                   DateHelper.ToDateTimeString(EndDate);
        }

        #endregion
    }
}



