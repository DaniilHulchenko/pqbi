using PQBI.Configuration;
using PQBI.Infrastructure;
using PQS.Data.Measurements;
using PQS.Data.Measurements.Enums;


namespace PQBI.CalculationEngine
{
    public static class IntervalSynchronizedAlignment
    {
        /// <summary>
        /// Aligns a DateTime down to the nearest interval boundary.
        /// </summary>
        public static DateTimeOffset AlignFloorUtc(
             DateTimeOffset utc,
             IntervalSynchronized interval,
             double multiplier,
             TimeZoneInfo userTz,
             bool isMondayStartOfWeek)
        {
            // Calendar-in-user-tz intervals: floor to calendar boundary (multiplier does not redefine "calendar boundary")
            if (interval is IntervalSynchronized.IS1HOUR or IntervalSynchronized.IS1DAY or IntervalSynchronized.IS1WEEK
                or IntervalSynchronized.IS1MONTH or IntervalSynchronized.IS1YEAR)
            {
                var local = TimeZoneConversion.UtcToUserLocal(utc, userTz).DateTime;

                var localFloor = interval switch
                {
                    IntervalSynchronized.IS1HOUR => new DateTime(local.Year, local.Month, local.Day, local.Hour, 0, 0),
                    IntervalSynchronized.IS1DAY => local.Date,
                    IntervalSynchronized.IS1WEEK => FloorToWeek(local, isMondayStartOfWeek),
                    IntervalSynchronized.IS1MONTH => new DateTime(local.Year, local.Month, 1),
                    IntervalSynchronized.IS1YEAR => new DateTime(local.Year, 1, 1),
                    _ => local
                };

                return TimeZoneConversion.UserLocalToUtc(localFloor, userTz);
            }

            // Fixed-duration UTC intervals: multiplier changes the step size
            var baseSeconds = (decimal)new SyncInterval(interval).TimeIntervalInSec;
            var stepSeconds = baseSeconds * (decimal)multiplier;
            var stepTicks = (long)Math.Round(stepSeconds * TimeSpan.TicksPerSecond, MidpointRounding.AwayFromZero);

            var ticks = utc.UtcTicks;
            var alignedTicks = (ticks / stepTicks) * stepTicks;
            return new DateTimeOffset(alignedTicks, TimeSpan.Zero);
        }

        public static DateTimeOffset AlignFloorUtc(
           DateTimeOffset utc,
           IntervalSynchronized interval,
           TimeZoneInfo userTz,
           bool isMondayStartOfWeek)
           => AlignFloorUtc(utc, interval, 1.0, userTz, isMondayStartOfWeek);

        /// <summary>
        /// Aligns a DateTime up to the nearest interval boundary.
        /// </summary>
        public static DateTimeOffset AlignCeilUtc(
             DateTimeOffset utc,
             IntervalSynchronized interval,
             double multiplier,
             TimeZoneInfo userTz,
             bool isMondayStartOfWeek)
        {
            var floor = AlignFloorUtc(utc, interval, userTz, isMondayStartOfWeek);
            if (floor == utc)
                return utc;

            return AddIntervalUtc(floor, interval, multiplier, userTz);
        }

        /// <summary>
        /// If you want "advance from start boundary until you reach/exceed rangeEndUtc",
        /// keep this (but rename it because it's not a ceil of a single instant).
        /// </summary>
        public static DateTimeOffset AdvanceUntilAtOrAfterUtc(
            DateTimeOffset rangeStartUtc,
            DateTimeOffset rangeEndUtc,
            IntervalSynchronized interval,
            double multiplier,
            TimeZoneInfo userTz,
            bool isMondayStartOfWeek)
        {
            var cursor = AlignFloorUtc(rangeStartUtc, interval, userTz, isMondayStartOfWeek);

            while (cursor < rangeEndUtc)
                cursor = AddIntervalUtc(cursor, interval, multiplier, userTz);

            return cursor;
        }


        public static IEnumerable<(DateTimeOffset StartUtc, DateTimeOffset EndUtc)> GenerateBucketsUtc(
                     DateTimeOffset rangeStartUtc,
                     DateTimeOffset rangeEndUtc,
                     IntervalSynchronized interval,
                     double multiplier,
                     TimeZoneInfo userTz,
                     bool isMondayStartOfWeek)
        {
            var cursor = AlignFloorUtc(rangeStartUtc, interval, userTz, isMondayStartOfWeek);

            while (cursor < rangeEndUtc)
            {
                var next = AddIntervalUtc(cursor, interval, multiplier, userTz);
                yield return (cursor, next);
                cursor = next;
            }
        }

        private static DateTimeOffset AddIntervalUtc(
                   DateTimeOffset cursorUtc,
                   IntervalSynchronized interval,
                   double multiplier,
                   TimeZoneInfo userTz)
        {
            // Treat these as "local wall-time durations" so fractional multipliers work:
            if (interval is IntervalSynchronized.IS1HOUR or IntervalSynchronized.IS1DAY or IntervalSynchronized.IS1WEEK)
            {
                var local = TimeZoneConversion.UtcToUserLocal(cursorUtc, userTz).DateTime; // wall time (Kind usually Unspecified)

                // Use decimal to avoid double drift (3.4 cannot be represented exactly as binary double)
                decimal baseSeconds = (decimal)new SyncInterval(interval).TimeIntervalInSec; // 3600 / 86400 / 604800
                decimal seconds = baseSeconds * (decimal)multiplier;

                // Add duration in local wall-time
                var nextLocal = local.AddTicks((long)Math.Round(seconds * TimeSpan.TicksPerSecond, MidpointRounding.AwayFromZero));

                // Convert local wall time -> UTC safely (must handle invalid/ambiguous DST times)
                return TimeZoneConversion.UserLocalToUtc(nextLocal, userTz);
            }

            // Months/years: fractional is ambiguous (what is 0.4 month?) — pick a policy.
            if (interval is IntervalSynchronized.IS1MONTH or IntervalSynchronized.IS1YEAR)
            {
                var local = TimeZoneConversion.UtcToUserLocal(cursorUtc, userTz).DateTime;
                if (Math.Abs(multiplier - Math.Round(multiplier)) <= 1e-9)
                {
                    int n = (int)Math.Round(multiplier);                  
                    var nextLocal = interval == IntervalSynchronized.IS1MONTH
                        ? local.AddMonths(n)
                        : local.AddYears(n);
                    return TimeZoneConversion.UserLocalToUtc(nextLocal, userTz);
                }
                else
                {                                      
                    decimal baseDays = interval == IntervalSynchronized.IS1MONTH ? 30m : 365m; // average days
                    decimal days = baseDays * (decimal)multiplier;
                    var nextLocal = local.AddTicks((long)Math.Round(days * TimeSpan.TicksPerDay, MidpointRounding.AwayFromZero));
                    return TimeZoneConversion.UserLocalToUtc(nextLocal, userTz);
                }
            }

            // Everything else: fixed-duration UTC (sub-hour/minute/etc) with multiplier
            {
                decimal baseSeconds = (decimal)new SyncInterval(interval).TimeIntervalInSec;
                decimal seconds = baseSeconds * (decimal)multiplier;
                return cursorUtc.AddTicks((long)Math.Round(seconds * TimeSpan.TicksPerSecond, MidpointRounding.AwayFromZero));
            }
        }

        //private static DateTimeOffset AddIntervalUtc(DateTimeOffset cursorUtc, IntervalSynchronized interval, TimeZoneInfo userTz, DayOfWeek? weekStart)
        //{
        //    // Calendar in TZ
        //    if (interval is IntervalSynchronized.IS1DAY or IntervalSynchronized.IS1WEEK
        //        or IntervalSynchronized.IS1MONTH or IntervalSynchronized.IS1YEAR)
        //    {
        //        var local = TimeZoneInfo.ConvertTime(cursorUtc, userTz).DateTime;

        //        var nextLocal = interval switch
        //        {
        //            IntervalSynchronized.IS1HOUR => new DateTime(local.Year, local.Month, local.Day, local.Hour, 0, 0),
        //            IntervalSynchronized.IS1DAY => local.AddDays(1),
        //            IntervalSynchronized.IS1WEEK => local.AddDays(7),
        //            IntervalSynchronized.IS1MONTH => local.AddMonths(1),
        //            IntervalSynchronized.IS1YEAR => local.AddYears(1),
        //            _ => local
        //        };

        //        return TimeZoneConversion.UserLocalToUtc(nextLocal, userTz);
        //    }

        //    // Fixed-duration UTC
        //    var seconds = new SyncInterval(interval).TimeIntervalInSec;
        //    return cursorUtc.AddSeconds(seconds);
        //}

        private static DateTime FloorToWeek(DateTime dt, bool isMondayStartOfWeek)
        {
            DayOfWeek ws = isMondayStartOfWeek ? DayOfWeek.Monday : DayOfWeek.Sunday;
          
            int delta = (7 + (dt.DayOfWeek - ws)) % 7;
            return dt.Date.AddDays(-delta);
        }
    }
}