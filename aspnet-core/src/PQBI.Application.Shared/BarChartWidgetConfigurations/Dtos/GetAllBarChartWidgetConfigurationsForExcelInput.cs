using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;

namespace PQBI.BarChartWidgetConfigurations.Dtos
{
    public class GetAllBarChartWidgetConfigurationsForExcelInput
    {
        public string Filter { get; set; }

        public int? TypeFilter { get; set; }

        public string EventsFilter { get; set; }

    }
}