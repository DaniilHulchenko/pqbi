using PQBI.BarChartWidgetConfigurations;

using System;
using Abp.Application.Services.Dto;

namespace PQBI.BarChartWidgetConfigurations.Dtos
{
    public class BarChartWidgetConfigurationDto : EntityDto
    {
        public BarChartType Type { get; set; }

        public string Components { get; set; }

        public string Events { get; set; }

        public string DateRange { get; set; }

    }
}