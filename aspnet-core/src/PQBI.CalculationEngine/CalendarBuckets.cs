using PQS.Data.Measurements.Enums;
using PQBI.Configuration;

namespace PQBI.CalculationEngine
{
    public enum TimeBucket
    {
        Hour, Day, Week, Month, Quarter, Year, FiveYears, TenYears
    }

    public static class CalendarBuckets
    {
        public static (TimeBucket, IEnumerable<(DateTime Start, DateTime End)>?) ChooseBucket(DateTime start, DateTime end, int maxBuckets)
        {
            IEnumerable<(DateTime Start, DateTime End)>? buckets = null;
            foreach (var candidate in Enum.GetValues<TimeBucket>())
            {
                buckets = GenerateBuckets(start, end, candidate);

                int count = buckets.Count();
                if (count <= maxBuckets)
                    return (candidate, buckets);
            }
            return (TimeBucket.TenYears, buckets); // fall-back: the coarsest unit
        }

        private static int CountBuckets(DateTime start, DateTime end, TimeBucket unit) =>
          GenerateBuckets(start, end, unit).Count();

        public static IEnumerable<(DateTime Start, DateTime End)> GenerateBuckets(
            DateTime rangeStartUtc,
            DateTime rangeEndUtc,
            TimeBucket unit)
        {
            var cursor = AlignFloor(rangeStartUtc, unit);
            while (cursor < rangeEndUtc)
            {
                var next = AddUnit(cursor, unit);
                yield return (cursor, next);
                cursor = next;
            }
        }

        public static DateTime AlignFloor(DateTime dt, TimeBucket unit) => unit switch
        {
            TimeBucket.Hour => dt.Date.AddHours(dt.Hour),
            TimeBucket.Day => dt.Date,
            TimeBucket.Week => FloorToWeek(dt, WeekConfiguration.StartOfWeek),
            TimeBucket.Month => new DateTime(dt.Year, dt.Month, 1, 0, 0, 0, dt.Kind),
            TimeBucket.Quarter => new DateTime(dt.Year, ((dt.Month - 1) / 3) * 3 + 1, 1, 0, 0, 0, dt.Kind),
            TimeBucket.Year => new DateTime(dt.Year, 1, 1, 0, 0, 0, dt.Kind),
            TimeBucket.FiveYears => new DateTime(dt.Year / 5 * 5, 1, 1, 0, 0, 0, dt.Kind),
            TimeBucket.TenYears => new DateTime(dt.Year / 10 * 10, 1, 1, 0, 0, 0, dt.Kind),
            _ => dt
        };

        public static DateTime AlignCeil(DateTime dt, TimeBucket unit)
        {
            var floor = CalendarBuckets.AlignFloor(dt, unit);
            return floor == dt ? dt : CalendarBuckets.AddUnit(floor, unit);
        }

        private static DateTime FloorToWeek(DateTime dt, DayOfWeek weekStart)
        {
            int delta = (7 + (dt.DayOfWeek - weekStart)) % 7;
            return dt.Date.AddDays(-delta);
        }

        // handles DST implicitly because everything is UTC; if you pass Local, CLR adjusts for you
        public static DateTime AddUnit(DateTime dt, TimeBucket unit) => unit switch
        {
            TimeBucket.Hour => dt.AddHours(1),
            TimeBucket.Day => dt.AddDays(1),
            TimeBucket.Week => dt.AddDays(7),
            TimeBucket.Month => dt.AddMonths(1),
            TimeBucket.Quarter => dt.AddMonths(3),
            TimeBucket.Year => dt.AddYears(1),
            TimeBucket.FiveYears => dt.AddYears(5),
            TimeBucket.TenYears => dt.AddYears(10),
            _ => dt
        };
    }

    public static class ScadaRequestPlanner
    {
        /// <summary>
        /// Decide which sync interval the SCADA must deliver and
        /// return a time window that is a clean multiple of that interval.
        /// </summary>
        public static (DateTime StartUtc, DateTime EndUtc) Plan(
            DateTime desiredStartUtc,
            DateTime desiredEndUtc,
            TimeBucket bucket)          // bucket chosen by your ChooseBucket(...)
        {
            var start = CalendarBuckets.AlignFloor(desiredStartUtc, bucket);
            var end = CalendarBuckets.AlignCeil(desiredEndUtc, bucket);   // round *to* bar width first
            end = CalendarBuckets.AlignCeil(end, bucket);          // then make sure it's full sync steps
            return (start, end);
        }

        private static (TimeBucket timeBucket, bool isChanged) MapToScadaSync(TimeBucket bucket) => bucket switch
        {
            TimeBucket.Quarter => (TimeBucket.Month, true),
            TimeBucket.FiveYears => (TimeBucket.Year, true),
            TimeBucket.TenYears => (TimeBucket.Year, true),
            _ => (bucket, false)          // already supported
        };

       
    }

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