using PQBI.Tenants.Dashboard.Dto;
using System;

namespace PQBI.PQS.Cache.Calculation
{
    public sealed class RealtimeCalculationCacheRecord
    {
        public Guid ComponentId { get; set; }
        public int? FeederId { get; set; }
        public string Parameter { get; set; } = default!;
        public DateTime CoveredStartUtc { get; set; }
        public DateTime CoveredEndUtc { get; set; }
        public PQBIAxisData PqbAxis { get; set; } = default!;
   
    }
}
