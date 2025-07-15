using System;
using Abp.Application.Services.Dto;
using System.ComponentModel.DataAnnotations;

namespace PQBI.BarChartWidgetConfigurations.Dtos
{
    public class GetBarChartWidgetConfigurationForEditOutput
    {
        public CreateOrEditBarChartWidgetConfigurationDto BarChartWidgetConfiguration { get; set; }

    }
}