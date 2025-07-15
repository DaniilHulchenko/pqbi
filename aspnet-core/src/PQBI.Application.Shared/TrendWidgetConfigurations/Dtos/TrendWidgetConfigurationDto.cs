using System;
using Abp.Application.Services.Dto;

namespace PQBI.TrendWidgetConfigurations.Dtos
{
    public class TrendWidgetConfigurationDto : EntityDto
    {
        public string DateRange { get; set; }

        public string Resolution { get; set; }

        public string Parameters { get; set; }

    }
}