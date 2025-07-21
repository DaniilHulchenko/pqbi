using PQBI.BarChartWidgetConfigurations;

using System;
using Abp.Application.Services.Dto;
using System.ComponentModel.DataAnnotations;

namespace PQBI.BarChartWidgetConfigurations.Dtos
{
    public class CreateOrEditBarChartWidgetConfigurationDto : EntityDto<int?>
    {

        public BarChartType Type { get; set; }

        [Required]
        public string Configuration { get; set; }

        [Required]
        public string Components { get; set; }

        public string DateRange { get; set; }

    }
}