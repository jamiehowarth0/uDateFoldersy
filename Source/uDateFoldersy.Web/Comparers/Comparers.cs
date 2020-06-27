namespace uDateFoldersy.Comparers
{
    using System;
    using System.Collections.Generic;

    using Umbraco.Core.Models;

    using uDateFoldersy.Extensions;
    using uDateFoldersy.Helpers;

    /// <summary>
    /// Comparer for post dates.
    /// </summary>
    public class PostDateComparer : IComparer<IContent>
    {
        public int Compare(IContent x, IContent y)
        {
            var dateAlias = ConfigReader.Instance.GetDatePropertyAlias();
            DateTime d1 = x.GetValue<DateTime>(dateAlias);
            DateTime d2 = y.GetValue<DateTime>(dateAlias);

            if (d1 < d2) { return -1; }
            if (d1 == d2) { return 0; }

            return 1;
        }
    }


    /// <summary>
    /// Comparer for date folder.
    /// </summary>
    public class YearComparer : IComparer<IContent>
    {
        public int Compare(IContent x, IContent y)
        {
            int year1 = int.TryParse(x.Name, out year1) ? year1 : -1;
            int year2 = int.TryParse(y.Name, out year2) ? year2 : -1;

            if (year1 < year2)
                return -1;

            if (year1 == year2)
                return 0;

            return 1;
        }
    }



    /// <summary>
    /// Comparer for date folder.
    /// </summary>
    public class MonthComparer : IComparer<IContent>
    {
        public int Compare(IContent x, IContent y)
        {
            int month1 = x.Name.GetMonthNumberFromName();
            int month2 = y.Name.GetMonthNumberFromName();

            if (month1 < month2)
                return -1;

            if (month1 == month2)
                return 0;

            return 1;
        }
    }


    /// <summary>
    /// Comparer for date folder.
    /// </summary>
    public class DayComparer : IComparer<IContent>
    {
        public int Compare(IContent x, IContent y)
        {
            int day1 = x.Name.GetDayNumberFromString();
            int day2 = y.Name.GetDayNumberFromString();

            if (day1 < day2)
                return -1;

            if (day1 == day2)
                return 0;

            return 1;
        }
    }
}
