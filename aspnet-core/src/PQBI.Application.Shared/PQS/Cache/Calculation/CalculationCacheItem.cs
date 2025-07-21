using System;
using Abp.Collections.Extensions;
using PQBI.Tenants.Dashboard.Dto;

namespace PQBI.PQS.Cache.Calculation;

[Serializable]
public class CalculationCacheItem
{

    public DateTime Start { get; init; }
    public DateTime End { get; init; }
    public Guid ComponentId { get; init; }
    public int? FeederId { get; set; }
    public string Parameter { get; init; } // STD_.....

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

            if (FeederId is null)
            {
                key = $"{ComponentId}_{Parameter}_{Start.Year}.{Start.Month}.{Start.Day}##{Start.Hour}:{Start.Minute}:{Start.Second}_{End.Year}.{End.Month}.{End.Day}##{End.Hour}:{End.Minute}:{End.Second}";
            }
            else
            {
                key = $"{ComponentId}_{FeederId}_{Parameter}_{Start.Year}.{Start.Month}.{Start.Day}##{Start.Hour}:{Start.Minute}:{Start.Second}_{End.Year}.{End.Month}.{End.Day}##{End.Hour}:{End.Minute}:{End.Second}";
            }

            return key;
        }
    }
}