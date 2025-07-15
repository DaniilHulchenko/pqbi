using Abp.Application.Services.Dto;
using System;

namespace PQBI.TrendWidgetConfigurations.Dtos
{
    public class GetAllTrendWidgetConfigurationsInput : PagedAndSortedResultRequestDto
    {
        public string Filter { get; set; }

    }
}