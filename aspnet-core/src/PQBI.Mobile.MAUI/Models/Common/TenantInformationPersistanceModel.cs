﻿using Abp.AutoMapper;
using PQBI.ApiClient;

namespace PQBI.Models.Common
{
    [AutoMapFrom(typeof(TenantInformation)),
     AutoMapTo(typeof(TenantInformation))]
    public class TenantInformationPersistanceModel
    {
        public string TenancyName { get; set; }

        public int TenantId { get; set; }
    }
}