using Abp.Application.Services.Dto;
using System;

namespace PQBI.BarChartWidgetConfigurations.Dtos
{
    public class GetAllBarChartWidgetConfigurationsInput : PagedAndSortedResultRequestDto
    {
        public string Filter { get; set; }

        public int? TypeFilter { get; set; }

        public string EventsFilter { get; set; }

    }
}