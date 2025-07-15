using PQZTimeFormat;

namespace PQBI.Infrastructure.Extensions;

public static class PqbiDateTimeExtensions
{
    public static IEnumerable<long> ToTicksSafe(this IEnumerable<DateTime> list)
    {
        if (list.SafeAny())
        {
            return list.Select(x => x.Ticks).ToArray();
        }

        return [];
    }

    public static long ToDateTimeOffsetInSeconds(this DateTime dateTime)
    {
        var unixTimeSeconds = new DateTimeOffset(dateTime).ToUnixTimeSeconds();
        return unixTimeSeconds;
    }

    public static PQZDateTime ToPqzDateTime(this DateTime dateTime)
    {
        return new PQZDateTime(dateTime);
    }
}
