using PQS.Data.Events.Enums;
using PQS.Data.Events.Filters;
using PQS.Data.Events;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PQBI.PQS.Cache.Calculation
{
    public static class RealtimeCacheKey
    {
        public static string For(Guid componentId, int? feederId, int durationInSec, int refreshInSec, string parameter, FiltersGroup filtersGroup)
        {
            var filterHash = filtersGroup == null || filtersGroup.FiltersCount == 0 ? "nofilters" : HashFilters(filtersGroup);
            //return $"{componentId:D}:{(feederId ?? -1)}:{parameter}:{filterHash}";
            return $"{componentId:D}:{(feederId ?? -1)}:{durationInSec}:{refreshInSec}:{parameter}:{filterHash}";
        }

        private static string HashFilters(FiltersGroup fg)
        {
            var sb = new StringBuilder();

            // Include CLASS filter deterministically (extend if you use more filters)
            var classFilter = (ClassFilter)fg.GetFilter(FilterTypeEnum.CLASS);
            if (classFilter?.ValueList != null)
            {
                var sorted = classFilter.ValueList.OrderBy(v => v);
                sb.Append("CLASS=").Append(string.Join(",", sorted)).Append("|");
            }

            sb.Append("COUNT=").Append(fg.FiltersCount);

            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }
}
