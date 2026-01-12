using PQBI.Tenants.Dashboard.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PQBI.CalculationEngine
{
    public static class PQBIAxisDataTimeZoneExtensions
    {
        public static PQBIAxisData ToUserTime(this PQBIAxisData axisUtc, TimeZoneInfo userTz)
        {
            if (axisUtc == null) throw new ArgumentNullException(nameof(axisUtc));
            if (userTz == null) throw new ArgumentNullException(nameof(userTz));

            // If you have a special "empty" axis type, keep it as-is
            if (axisUtc is PQBIAxisDataEmpty)
                return axisUtc;

            var src = axisUtc.DataTimeStamps;
            var converted = new PQBIDataTimeStampDto[src.Length];

            for (int i = 0; i < src.Length; i++)
            {
                var s = src[i];

                var tsUtc = DateTime.SpecifyKind(s.DateTime, DateTimeKind.Utc);
                var tsLocal = TimeZoneInfo.ConvertTimeFromUtc(tsUtc, userTz);

                converted[i] = new PQBIDataTimeStampDto(tsLocal, s.Point, s.DataValueStatus);
            }

            // rebuild axis with converted timestamps
            return new PQBIAxisData(
                axisUtc.ComponentId,
                axisUtc.FeederID,        // or FeederId
                axisUtc.ParameterName,
                axisUtc.Nominal,
                converted,
                axisUtc.PQZStatus,
                axisUtc.DataUnitType);
        }
    }
}
