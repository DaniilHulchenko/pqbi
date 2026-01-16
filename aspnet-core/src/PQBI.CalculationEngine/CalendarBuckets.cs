using PQS.Data.Measurements.Enums;
using PQBI.Configuration;
using PQBI.Infrastructure;

namespace PQBI.CalculationEngine
{
    public enum TimeBucket
    {
        Hour, Day, Week, Month, Quarter, Year, FiveYears, TenYears
    }

    public static class CalendarBuckets
    {
        public static (TimeBucket Bucket, IReadOnlyList<(DateTimeOffset StartUtc, DateTimeOffset EndUtc)> BucketsUtc)
           ChooseBucket(DateTimeOffset desiredStartUtc, DateTimeOffset desiredEndUtc, TimeZoneInfo userTz, int maxBuckets, bool isMondayStartOfWeek)
        {
            List<(DateTimeOffset, DateTimeOffset)>? buckets = null;

            foreach (var candidate in Enum.GetValues<TimeBucket>())
            {
                buckets = GenerateBucketsUtc(desiredStartUtc, desiredEndUtc, candidate, userTz, isMondayStartOfWeek).ToList();
                if (buckets.Count <= maxBuckets)
                    return (candidate, buckets);
            }

            return (TimeBucket.TenYears, buckets ?? new List<(DateTimeOffset, DateTimeOffset)>());
        }

        // Main API: outputs UTC instants for safe membership tests
        public static IEnumerable<(DateTimeOffset StartUtc, DateTimeOffset EndUtc)> GenerateBucketsUtc(
            DateTimeOffset rangeStartUtc,
            DateTimeOffset rangeEndUtc,
            TimeBucket unit,
            TimeZoneInfo userTz,
            bool isMondayStartOfWeek)
        {
            if (unit == TimeBucket.Hour)
            {
                // Hour buckets: fixed-duration UTC buckets are simplest and DST-safe.
                // (Local labels may skip/repeat, which is expected behavior.)
                return GenerateFixedUtcBuckets(rangeStartUtc, rangeEndUtc, TimeSpan.FromHours(1));
            }

            return GenerateCalendarBuckets(rangeStartUtc, rangeEndUtc, unit, userTz, isMondayStartOfWeek);
        }

        //// Align start/end to bucket boundaries, but in the USER timezone for calendar units
        //public static DateTimeOffset AlignFloorUtc(DateTimeOffset utc, TimeBucket unit, TimeZoneInfo userTz)
        //{
        //    if (unit == TimeBucket.Hour)
        //        return AlignFloorUtcFixed(utc, TimeSpan.FromHours(1));

        //    var local = TimeZoneInfo.ConvertTime(utc, userTz).DateTime; // wall time
        //    var localFloor = AlignFloorLocal(local, unit);
        //    return TimeZoneConversion.UserLocalToUtc(localFloor, userTz);
        //}

        //public static DateTimeOffset AlignCeilUtc(DateTimeOffset utc, TimeBucket unit, TimeZoneInfo userTz)
        //{
        //    var floor = AlignFloorUtc(utc, unit, userTz);
        //    if (floor == utc) return utc;

        //    if (unit == TimeBucket.Hour) return floor + TimeSpan.FromHours(1);

        //    var localFloor = TimeZoneInfo.ConvertTime(floor, userTz).DateTime;
        //    var nextLocal = AddUnitLocal(localFloor, unit);
        //    return TimeZoneConversion.UserLocalToUtc(nextLocal, userTz);
        //}

        private static IEnumerable<(DateTimeOffset StartUtc, DateTimeOffset EndUtc)> GenerateFixedUtcBuckets(
            DateTimeOffset startUtc,
            DateTimeOffset endUtc,
            TimeSpan step)
        {
            var cursor = AlignFloorUtcFixed(startUtc, step);
            while (cursor < endUtc)
            {
                var next = cursor + step;
                yield return (cursor, next);
                cursor = next;
            }
        }

        private static DateTimeOffset AlignFloorUtcFixed(DateTimeOffset utc, TimeSpan step)
        {
            var ticks = utc.UtcTicks;
            var stepTicks = step.Ticks;
            var aligned = (ticks / stepTicks) * stepTicks;
            return new DateTimeOffset(aligned, TimeSpan.Zero);
        }

        private static IEnumerable<(DateTimeOffset StartUtc, DateTimeOffset EndUtc)> GenerateCalendarBuckets(
            DateTimeOffset rangeStartUtc,
            DateTimeOffset rangeEndUtc,
            TimeBucket unit,
            TimeZoneInfo userTz,
            bool isMondayStartOfWeek)
        {
            var startLocal = TimeZoneConversion.UtcToUserLocal(rangeStartUtc, userTz).DateTime;
            var cursorLocal = AlignFloorLocal(startLocal, unit, isMondayStartOfWeek);

            while (true)
            {
                var nextLocal = AddUnitLocal(cursorLocal, unit);

                var cursorUtc = TimeZoneConversion.UserLocalToUtc(cursorLocal, userTz);
                var nextUtc = TimeZoneConversion.UserLocalToUtc(nextLocal, userTz);

                if (nextUtc <= rangeStartUtc)
                {
                    cursorLocal = nextLocal;
                    continue;
                }

                if (cursorUtc >= rangeEndUtc)
                    yield break;

                yield return (cursorUtc, nextUtc);
                cursorLocal = nextLocal;
            }
        }

        private static DateTime AlignFloorLocal(DateTime local, TimeBucket unit, bool isMondayStartOfWeek) => unit switch
        {
            TimeBucket.Hour => new DateTime(local.Year, local.Month, local.Day, local.Hour, 0, 0),
            TimeBucket.Day => local.Date,
            TimeBucket.Week => FloorToWeek(local, isMondayStartOfWeek),
            TimeBucket.Month => new DateTime(local.Year, local.Month, 1),
            TimeBucket.Quarter => new DateTime(local.Year, ((local.Month - 1) / 3) * 3 + 1, 1),
            TimeBucket.Year => new DateTime(local.Year, 1, 1),
            TimeBucket.FiveYears => new DateTime((local.Year / 5) * 5, 1, 1),
            TimeBucket.TenYears => new DateTime((local.Year / 10) * 10, 1, 1),
            _ => local
        };

        private static DateTime AddUnitLocal(DateTime local, TimeBucket unit) => unit switch
        {
            TimeBucket.Hour => local.AddHours(1),
            TimeBucket.Day => local.AddDays(1),
            TimeBucket.Week => local.AddDays(7),
            TimeBucket.Month => local.AddMonths(1),
            TimeBucket.Quarter => local.AddMonths(3),
            TimeBucket.Year => local.AddYears(1),
            TimeBucket.FiveYears => local.AddYears(5),
            TimeBucket.TenYears => local.AddYears(10),
            _ => local
        };

        private static DateTime FloorToWeek(DateTime dt, bool isMondayStartOfWeek)
        {
            DayOfWeek weekStart = isMondayStartOfWeek ? DayOfWeek.Monday : DayOfWeek.Sunday;
            int delta = (7 + (dt.DayOfWeek - weekStart)) % 7;
            return dt.Date.AddDays(-delta);
        }
    }

    //public static class ScadaRequestPlanner
    //{
    //    /// <summary>
    //    /// Decide which sync interval the SCADA must deliver and
    //    /// return a time window that is a clean multiple of that interval.
    //    /// </summary>
    //    public static (DateTime StartUtc, DateTime EndUtc) Plan(
    //        DateTime desiredStartUtc,
    //        DateTime desiredEndUtc,
    //        TimeBucket bucket)          // bucket chosen by your ChooseBucket(...)
    //    {
    //        var start = CalendarBuckets.AlignFloor(desiredStartUtc, bucket);
    //        var end = CalendarBuckets.AlignCeil(desiredEndUtc, bucket);   // round *to* bar width first
    //        end = CalendarBuckets.AlignCeil(end, bucket);          // then make sure it's full sync steps
    //        return (start, end);
    //    }

    //    private static (TimeBucket timeBucket, bool isChanged) MapToScadaSync(TimeBucket bucket) => bucket switch
    //    {
    //        TimeBucket.Quarter => (TimeBucket.Month, true),
    //        TimeBucket.FiveYears => (TimeBucket.Year, true),
    //        TimeBucket.TenYears => (TimeBucket.Year, true),
    //        _ => (bucket, false)          // already supported
    //    };

       
    //}

    public static class TimeBucketMapping
    {
        /// <summary>
        /// Map a chart bucket to the closest SCADA sync interval.
        /// Quarter   ➜ Month      (3 monthly samples per bar)
        /// 5 / 10 yr ➜ Year       (5 or 10 yearly samples per bar)
        /// </summary>
        public static IntervalSynchronized ToIntervalSync(this TimeBucket bucket) => bucket switch
        {
            TimeBucket.Hour => IntervalSynchronized.IS1HOUR,
            TimeBucket.Day => IntervalSynchronized.IS1DAY,
            TimeBucket.Week => IntervalSynchronized.IS1WEEK,
            TimeBucket.Month => IntervalSynchronized.IS1MONTH,
            TimeBucket.Quarter => IntervalSynchronized.IS1MONTH,  // fallback
            TimeBucket.Year => IntervalSynchronized.IS1YEAR,
            TimeBucket.FiveYears => IntervalSynchronized.IS1YEAR,   // fallback
            TimeBucket.TenYears => IntervalSynchronized.IS1YEAR,   // fallback
            _ => throw new ArgumentOutOfRangeException(nameof(bucket))
        };

        /// <summary>
        /// Reverse mapping (best effort).
        /// Month maps to Month (not Quarter); Year maps to Year (not 5 / 10 yr).
        /// </summary>
        public static TimeBucket ToTimeBucket(this IntervalSynchronized sync) => sync switch
        {
            IntervalSynchronized.IS1HOUR => TimeBucket.Hour,
            IntervalSynchronized.IS1DAY => TimeBucket.Day,
            IntervalSynchronized.IS1WEEK => TimeBucket.Week,
            IntervalSynchronized.IS1MONTH => TimeBucket.Month,
            IntervalSynchronized.IS1YEAR => TimeBucket.Year,
            _ => throw new ArgumentOutOfRangeException(nameof(sync))
        };
    }
}