using System;

namespace Gcpe.Hub.BusinessInsights.API.Guards
{
    public static class DateGuard
    {
        public static void ThrowIfNullOrWhitespace(string startDate = "", string endDate = "")
        {
            if (string.IsNullOrWhiteSpace(startDate) || string.IsNullOrWhiteSpace(endDate)) throw new ArgumentNullException("Start and end date cannot be null or blank.");
        }

        public static void ThrowIfNullOrEmpty(string startDate = "", string endDate = "")
        {
            if (string.IsNullOrEmpty(startDate) || string.IsNullOrEmpty(endDate)) throw new ArgumentNullException("Start and end date cannot be null or empty.");
        }

        public static void ThrowIfEndIsBeforeStart(DateTimeOffset start, DateTimeOffset end)
        {
            if (end < start)
            {
                throw new ArgumentException("End date cannot occur before start date.");
            }
        }

        public static void ThrowIfStartAndEndAreEqual(DateTimeOffset start, DateTimeOffset end)
        {
            if (start == end)
            {
                throw new ArgumentException("Start date and end date must not be the same.");
            }
        }

        public static void ThrowIfNotFirstOfTheMonth(DateTimeOffset start, DateTimeOffset end)
        {
            if (start.Day != 1) throw new ArgumentException("Start day must be the first of the month.");
            if (end.Day != 1) throw new ArgumentException("End day must be the first of the month.");
        }

        public static void ThrowIfDateRangeNotOneMonth(DateTimeOffset start, DateTimeOffset end)
        {
            if (start.AddMonths(1).Month != end.Month) throw new ArgumentException("End date must be the first of the month immediately after the start month.");
        }
    }
}
