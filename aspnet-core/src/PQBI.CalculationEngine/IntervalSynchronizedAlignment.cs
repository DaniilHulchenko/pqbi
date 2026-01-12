using PQBI.Configuration;
using PQS.Data.Measurements;
using PQS.Data.Measurements.Enums;


namespace PQBI.CalculationEngine
{
    public static class IntervalSynchronizedAlignment
    {
        /// <summary>
        /// Aligns a DateTime down to the nearest interval boundary.
        /// </summary>
        public static DateTime AlignFloor(DateTime dt, IntervalSynchronized interval, DayOfWeek? weekStart = null)
        {
            // Use configured week start if not explicitly provided
            var effectiveWeekStart = weekStart ?? WeekConfiguration.StartOfWeek;

            // Extract the interval value from the enum (e.g., IS1MIN -> 60 seconds)
            var syncInterval = new SyncInterval(interval);
            long intervalInSeconds = (long)syncInterval.TimeIntervalInSec;

            // Handle calendar-based intervals (Week, Month, Year)
            if (interval == IntervalSynchronized.IS1WEEK)
            {
                return FloorToWeek(dt, effectiveWeekStart);
            }
            else if (interval == IntervalSynchronized.IS1MONTH)
            {
                return new DateTime(dt.Year, dt.Month, 1, 0, 0, 0, dt.Kind);
            }
            else if (interval == IntervalSynchronized.IS1YEAR)
            {
                return new DateTime(dt.Year, 1, 1, 0, 0, 0, dt.Kind);
            }
            // Handle fixed-duration intervals using tick arithmetic
            else
            {
                long ticks = dt.Ticks;
                long intervalTicks = TimeSpan.FromSeconds(intervalInSeconds).Ticks;
                long alignedTicks = (ticks / intervalTicks) * intervalTicks;
                return new DateTime(alignedTicks, dt.Kind);
            }
        }

        /// <summary>
        /// Aligns a DateTime up to the nearest interval boundary.
        /// </summary>
        public static DateTime AlignCeil(DateTime dt, IntervalSynchronized interval, DayOfWeek? weekStart = null)
        {
            var floor = AlignFloor(dt, interval, weekStart);

            // If already aligned, return as-is
            if (floor == dt)
                return dt;

            // Otherwise, add one interval
            return AddInterval(floor, interval);
        }

        /// <summary>
        /// Adds interval units to the given DateTime.
        /// </summary>
        /// <param name="dt">The DateTime to add to</param>
        /// <param name="interval">The base interval type</param>
        /// <param name="multiplier">The multiplier for the interval (e.g., 3.4 for 3.4x the interval)</param>
        /// <returns>The new DateTime</returns>
        public static DateTime AddInterval(DateTime dt, IntervalSynchronized interval, double multiplier = 1.0)
        {
            var syncInterval = new SyncInterval(interval);
            double intervalInSeconds = syncInterval.TimeIntervalInSec * multiplier;

            // Handle calendar-based intervals
            if (interval == IntervalSynchronized.IS1WEEK)
            {
                // For weeks, multiply by 7 days
                int wholeDays = (int)Math.Floor(multiplier * 7);
                double remainingFraction = (multiplier * 7) - wholeDays;

                var result = dt.AddDays(wholeDays);

                // If there's a fractional part, add remaining hours
                if (remainingFraction > 0)
                {
                    double remainingSeconds = remainingFraction * 24 * 3600;
                    result = result.AddSeconds(remainingSeconds);
                }

                return result;
            }
            else if (interval == IntervalSynchronized.IS1MONTH)
            {
                // For months, we can only add whole months, then add remaining seconds
                int wholeMonths = (int)Math.Floor(multiplier);
                double remainingFraction = multiplier - wholeMonths;

                var result = dt.AddMonths(wholeMonths);

                // If there's a fractional part, estimate it as 30 days per month
                if (remainingFraction > 0)
                {
                    double remainingSeconds = remainingFraction * 30 * 24 * 3600;
                    result = result.AddSeconds(remainingSeconds);
                }

                return result;
            }
            else if (interval == IntervalSynchronized.IS1YEAR)
            {
                // For years, we can only add whole years, then add remaining seconds
                int wholeYears = (int)Math.Floor(multiplier);
                double remainingFraction = multiplier - wholeYears;

                var result = dt.AddYears(wholeYears);

                // If there's a fractional part, estimate it as 365 days per year
                if (remainingFraction > 0)
                {
                    double remainingSeconds = remainingFraction * 365 * 24 * 3600;
                    result = result.AddSeconds(remainingSeconds);
                }

                return result;
            }
            else
            {
                // For all other intervals (seconds, minutes, hours, days)
                return dt.AddSeconds(intervalInSeconds);
            }
        }

        /// <summary>
        /// Generates time buckets for the given interval with optional multiplier.
        /// </summary>
        /// <param name="rangeStartUtc">Start of the time range</param>
        /// <param name="rangeEndUtc">End of the time range</param>
        /// <param name="interval">The base interval type</param>
        /// <param name="multiplier">Multiplier for the interval (e.g., 3.4 for 3.4 seconds when interval is IS1SEC)</param>
        /// <param name="weekStart">Optional week start day (defaults to configured value)</param>
        /// <returns>Enumerable of (Start, End) tuples representing each bucket</returns>
        public static IEnumerable<(DateTime Start, DateTime End)> GenerateBuckets(
            DateTime rangeStartUtc,
            DateTime rangeEndUtc,
            IntervalSynchronized interval,
            double multiplier = 1.0,
            DayOfWeek? weekStart = null)
        {
            var cursor = AlignFloor(rangeStartUtc, interval, weekStart);
            while (cursor < rangeEndUtc)
            {
                var next = AddInterval(cursor, interval, multiplier);
                yield return (cursor, next);
                cursor = next;
            }
        }

        /// <summary>
        /// Generates time buckets for the given interval in seconds.
        /// This is a convenience method when you want to specify the bucket size directly in seconds.
        /// </summary>
        /// <param name="rangeStartUtc">Start of the time range</param>
        /// <param name="rangeEndUtc">End of the time range</param>
        /// <param name="bucketSizeInSeconds">The size of each bucket in seconds (e.g., 3.4)</param>
        /// <returns>Enumerable of (Start, End) tuples representing each bucket</returns>
        public static IEnumerable<(DateTime Start, DateTime End)> GenerateBucketsInSeconds(
            DateTime rangeStartUtc,
            DateTime rangeEndUtc,
            double bucketSizeInSeconds)
        {
            // Use IS1SEC as base and apply multiplier
            return GenerateBuckets(rangeStartUtc, rangeEndUtc, IntervalSynchronized.IS1SEC, bucketSizeInSeconds);
        }

        /// <summary>
        /// Helper method to floor a DateTime to the start of the week.
        /// </summary>
        private static DateTime FloorToWeek(DateTime dt, DayOfWeek weekStart)
        {
            int delta = (7 + (dt.DayOfWeek - weekStart)) % 7;
            return dt.Date.AddDays(-delta);
        }
    }
}