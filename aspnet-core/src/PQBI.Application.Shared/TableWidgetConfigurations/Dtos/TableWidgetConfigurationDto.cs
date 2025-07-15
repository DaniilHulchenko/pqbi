using System;
using Abp.Application.Services.Dto;

namespace PQBI.TableWidgetConfigurations.Dtos
{
    public class TableWidgetConfigurationDto : EntityDto
    {
        public string Configuration { get; set; }

        public string Components { get; set; }

        public string DateRange { get; set; }

        public string DesignOptions { get; set; }

    }
}