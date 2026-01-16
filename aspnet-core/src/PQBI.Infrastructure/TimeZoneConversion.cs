using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeZoneConverter;

namespace PQBI.Infrastructure
{
    public class TimeZoneConversion
    {
        public static TimeZoneInfo ResolveUserTimeZone(string? userTimeZoneId, int? utcOffsetMinutes)
        {
            if (!string.IsNullOrWhiteSpace(userTimeZoneId))
            {
                userTimeZoneId = userTimeZoneId.Trim();

                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(userTimeZoneId);
                }
                catch
                {
                    // ignore
                }

                if (TZConvert.TryGetTimeZoneInfo(userTimeZoneId, out var tz))
                    return tz;

                // fallback to offset if provided
                if (utcOffsetMinutes.HasValue)
                    return CreateFixedOffsetTimeZone(utcOffsetMinutes);

                return TimeZoneInfo.Utc;
            }
           
            return CreateFixedOffsetTimeZone(utcOffsetMinutes);
        }

        public static TimeZoneInfo CreateFixedOffsetTimeZone(int? utcOffsetMinutes)
        {
            TimeSpan offset = utcOffsetMinutes.HasValue
              ? TimeSpan.FromMinutes(utcOffsetMinutes.Value)
              : TimeSpan.Zero;

            var sign = offset >= TimeSpan.Zero ? "+" : "-";
            var abs = offset.Duration();
            var id = $"UTC{sign}{abs:hh\\:mm}";
            return TimeZoneInfo.CreateCustomTimeZone(id, offset, id, id);
        }

        public static DateTimeOffset UserLocalToUtc(
            DateTime userLocalWallTime,
            TimeZoneInfo userTz,
            AmbiguousLocalTimePolicy ambiguousPolicy = AmbiguousLocalTimePolicy.EarlierInstant)
        {
            var local = DateTime.SpecifyKind(userLocalWallTime, DateTimeKind.Unspecified);

            // Fixed-offset zone: no DST
            if (!userTz.SupportsDaylightSavingTime)
            {
                return new DateTimeOffset(local, userTz.BaseUtcOffset).ToUniversalTime();
            }

            if (userTz.IsInvalidTime(local))
            {
                for (int i = 0; i < 180 && userTz.IsInvalidTime(local); i++)
                    local = local.AddMinutes(1);
            }

            if (userTz.IsAmbiguousTime(local))
            {
                var offsets = userTz.GetAmbiguousTimeOffsets(local);
                var chosen = ambiguousPolicy == AmbiguousLocalTimePolicy.EarlierInstant
                    ? offsets.Max()
                    : offsets.Min();

                return new DateTimeOffset(local, chosen).ToUniversalTime();
            }

            var utc = TimeZoneInfo.ConvertTimeToUtc(local, userTz);
            return new DateTimeOffset(utc, TimeSpan.Zero);
        }

        /// <summary>UTC instant -> user's local time (as DateTimeOffset with correct local offset).</summary>
        public static DateTimeOffset UtcToUserLocal(DateTimeOffset utc, TimeZoneInfo userTz)
            => TimeZoneInfo.ConvertTime(utc, userTz);

        public static DateTime UtcToUserLocal(DateTime utc, TimeZoneInfo userTz)
        {
            var utcFixed = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(utcFixed, userTz);
        }

        /// <summary>
        /// Treat a DateTime as a UTC instant without shifting the clock.
        /// Use when you know the value is UTC but Kind may be wrong.
        /// </summary>
        public static DateTimeOffset AssumeUtc(DateTime utcClock)
            => new DateTimeOffset(DateTime.SpecifyKind(utcClock, DateTimeKind.Utc), TimeSpan.Zero);

        /// <summary>
        /// Convert DateTime to UTC instant respecting Kind (Local/Unspecified treated as local machine time).
        /// Use only if that is what you want.
        /// </summary>
        public static DateTimeOffset RespectKindToUtc(DateTime dt)
        {
            return dt.Kind switch
            {
                DateTimeKind.Utc => new DateTimeOffset(dt, TimeSpan.Zero),
                DateTimeKind.Local => new DateTimeOffset(dt).ToUniversalTime(),
                DateTimeKind.Unspecified => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Local)).ToUniversalTime(),
                _ => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc), TimeSpan.Zero)
            };
        }
    }

    public enum AmbiguousLocalTimePolicy
    {
        EarlierInstant, // choose the first occurrence (usually DST offset)
        LaterInstant    // choose the second occurrence (usually standard offset)
    }
}
