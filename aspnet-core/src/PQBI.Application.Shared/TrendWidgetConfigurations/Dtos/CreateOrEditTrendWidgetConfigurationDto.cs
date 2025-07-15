using System;
using Abp.Application.Services.Dto;
using System.ComponentModel.DataAnnotations;

namespace PQBI.TrendWidgetConfigurations.Dtos
{
    public class CreateOrEditTrendWidgetConfigurationDto : EntityDto<int?>
    {

        [Required]
        public string DateRange { get; set; }

        [Required]
        public string Resolution { get; set; }

        public string Parameters { get; set; }

    }
}