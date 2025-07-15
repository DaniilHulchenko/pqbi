using System;
using Abp.Collections.Extensions;
using PQBI.Tenants.Dashboard.Dto;
using PQS.Data.Events;
using PQS.Data.Events.Enums;
using PQS.Data.Events.Filters;

namespace PQBI.PQS.Cache.Calculation;

[Serializable]
public class CalculationCacheItem
{

    public DateTime Start { get; init; }
    public DateTime End { get; init; }
    public Guid ComponentId { get; init; }
    public int? FeederId { get; set; }
    public string Parameter { get; init; } // STD_.....
    public FiltersGroup FiltersGroup { get; set; }
    public PQBIAxisData PQBIAxisData { get; init; }

    public string CacheKey
    {
        get
        {
            if (ComponentId == Guid.Empty || Parameter.IsNullOrEmpty())
            {
                throw new Exception($"{nameof(CalculationCacheItem)} - Failed");
            }

            var key = string.Empty;

            string classList = string.Empty;
            if (FiltersGroup != null)
            {
                ClassFilter classFilter = (ClassFilter)FiltersGroup.GetFilter(FilterTypeEnum.CLASS);
                if (classFilter != null)
                    classList = string.Join(',', classFilter.ValueList);
            }

            if (FeederId is null)
            {
                key = $"{ComponentId}_{Parameter}_{Start.Year}.{Start.Month}.{Start.Day}##{Start.Hour}:{Start.Minute}:{Start.Second}_{End.Year}.{End.Month}.{End.Day}##{End.Hour}:{End.Minute}:{End.Second}##{classList}";
            }
            else
            {
                key = $"{ComponentId}_{FeederId}_{Parameter}_{Start.Year}.{Start.Month}.{Start.Day}##{Start.Hour}:{Start.Minute}:{Start.Second}_{End.Year}.{End.Month}.{End.Day}##{End.Hour}:{End.Minute}:{End.Second}##{classList}";
            }

            return key;
        }
    }
}