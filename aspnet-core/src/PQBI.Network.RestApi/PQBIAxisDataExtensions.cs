using PQBI.Tenants.Dashboard.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PQBI.Network.RestApi
{
    public static class PQBIAxisDataExtensions
    {
        /// <summary>
        /// Returns a new PQBIAxisData containing only samples in [startUtc, endUtc] (inclusive).
        /// Preserves identity (ComponentId/FeederID/ParameterName/Nominal/DataUnitType/PQZStatus).
        /// </summary>
        public static PQBIAxisData Slice(this PQBIAxisData src, DateTime startUtc, DateTime endUtc)
        {
            if (src == null) throw new ArgumentNullException(nameof(src));
            var arr = src.DataTimeStamps;
            if (arr.Length == 0) return CloneWith(src, arr);

            int i = 0;
            while (i < arr.Length && arr[i].DateTime < startUtc) i++;

            if (arr[i].DateTime > startUtc && i > 0)
                i--;

            if (i == 0) return src; // nothing to cut
            if (i >= arr.Length) return CloneWith(src, Array.Empty<PQBIDataTimeStampDto>());

            var slice = new PQBIDataTimeStampDto[arr.Length - i];
            Array.Copy(arr, i, slice, 0, slice.Length);
            return CloneWith(src, slice);
        }

        /// <summary>
        /// Merge two series by timestamp. If a timestamp exists in both,
        /// the sample from <paramref name="right"/> replaces the sample in <paramref name="left"/>.
        /// Output is chronologically sorted and deduplicated.
        /// </summary>
        public static PQBIAxisData MergeAppendWithTailOverride(this PQBIAxisData left, PQBIAxisData right)
        {
            if (left == null) return right;
            if (right == null) return left;

            var a = left.DataTimeStamps;
            var b = right.DataTimeStamps;
            if (b.Length == 0) return left;
            if (a.Length == 0) return right;

            //EnsureSameIdentity(left, right);

            int i = 0;
            for (; i < a.Length; i++)
            {
                if (b[0].DateTime <= a[i].DateTime)
                    break;               
            }

            List<PQBIDataTimeStampDto> list = null;
            if (i > 0)
            {              
                list = a.Take(i).ToList();
                list.AddRange(b);
            }
            else
            {
                list = b.ToList();              
            }

            return CloneWith(right, list.ToArray());
        }

        /// <summary>
        /// Get min/max DateTime of the series; returns (DateTime.MinValue, DateTime.MinValue) if empty.
        /// </summary>
        public static (DateTime MinUtc, DateTime MaxUtc) GetRangeUtc(this PQBIAxisData src)
        {
            if (src?.DataTimeStamps == null || src.DataTimeStamps.Length == 0)
                return (DateTime.MinValue, DateTime.MinValue);

            var min = src.DataTimeStamps.Min(d => d.DateTime);
            var max = src.DataTimeStamps.Max(d => d.DateTime);
            return (min, max);
        }

        private static PQBIAxisData CloneWith(PQBIAxisData src, PQBIDataTimeStampDto[] stamps)
            => new PQBIAxisData(
                    src.ComponentId,
                    src.FeederID,
                    src.ParameterName,
                    src.Nominal,
                    stamps,
                    src.PQZStatus,
                    src.DataUnitType);

        /// <summary>
        /// Defensive guard to avoid silent cross-identity merges.
        /// </summary>
        private static void EnsureSameIdentity(PQBIAxisData a, PQBIAxisData b)
        {
            if (a.ComponentId != b.ComponentId ||
                a.FeederID != b.FeederID ||
                !string.Equals(a.ParameterName, b.ParameterName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Merging different series is not allowed. " +
                    $"A=({a.ComponentId},{a.FeederID},{a.ParameterName}) vs " +
                    $"B=({b.ComponentId},{b.FeederID},{b.ParameterName})");
            }
        }
    }
}
