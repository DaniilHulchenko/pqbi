using System.Collections.Generic;
using Abp.Application.Services.Dto;
using PQBI.Editions.Dto;

namespace PQBI.MultiTenancy.Dto
{
    public class GetTenantFeaturesEditOutput
    {
        public List<NameValueDto> FeatureValues { get; set; }

        public List<FlatFeatureDto> Features { get; set; }
    }
}