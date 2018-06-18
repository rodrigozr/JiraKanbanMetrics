using System;
using System.Globalization;

namespace JiraKanbanMetrics.Core
{
    /// <summary>
    /// Extension methods for DateTime
    /// </summary>
    public static class DateTimeExtensions
    {
        private static readonly Calendar Calendar = (DateTimeFormatInfo.CurrentInfo ?? DateTimeFormatInfo.InvariantInfo).Calendar;

        /// <summary>
        /// Converts the date-time to "yy-MM-dd" format
        /// </summary>
        /// <param name="self">date-time</param>
        /// <returns>date-time in yy-MM-dd format</returns>
        public static string YYMMDD(this DateTime? self)
        {
            return self?.ToString("yy-MM-dd");
        }

        /// <summary>
        /// Gets the number of the week from a date-time
        /// </summary>
        /// <param name="self">date-time</param>
        /// <returns>number of the week within the year</returns>
        public static int GetWeekNumber(this DateTime self)
        {
            return Calendar.GetWeekOfYear(self, CalendarWeekRule.FirstDay, DayOfWeek.Sunday);
        }

        /// <summary>
        /// Gets the start date of a week, within the same year
        /// </summary>
        /// <param name="self">date-time (any date within the week to calculate)</param>
        /// <returns>the start date of that week, within the same year</returns>
        public static DateTime GetWeekStartDate(this DateTime self)
        {
            var year = self.Year;
            var week = self.GetWeekNumber();
            var yearStart = new DateTime(year, 1, 1);
            if (week <= 1)
                return yearStart;
            var date = yearStart.AddDays((week - 1) * 7);
            while (date.DayOfWeek != DayOfWeek.Sunday)
                date = date.AddDays(-1);
            return date;
        }

        /// <summary>
        /// Gets the end date of a week, within the same year
        /// </summary>
        /// <param name="self">date-time (any date within the week to calculate)</param>
        /// <returns>the end date of that week, within the same year</returns>
        public static DateTime GetWeekEndDate(this DateTime self)
        {
            var weekStartDate = self.GetWeekStartDate();
            var date = weekStartDate.AddDays(6);
            while (date.DayOfWeek != DayOfWeek.Saturday)
                date = date.AddDays(-1);
            while (date.Year > weekStartDate.Year)
                date = date.AddDays(-1);
            return date;
        }

        /// <summary>
        /// Converts a date-time to "yyyy-MM"
        /// </summary>
        /// <param name="self">date-time</param>
        /// <returns>formatted date-time</returns>
        public static string YearMonth(this DateTime self)
        {
            return $"{self.Year}-{self.Month:00}";
        }

        /// <summary>
        /// Converts a date-time to "yyyy-{weekNumber}"
        /// </summary>
        /// <param name="self">date-time</param>
        /// <returns>formatted date-time</returns>
        public static string YearWeek(this DateTime self)
        {
            return $"{self.Year}-{self.GetWeekNumber():00}";
        }
    }
}