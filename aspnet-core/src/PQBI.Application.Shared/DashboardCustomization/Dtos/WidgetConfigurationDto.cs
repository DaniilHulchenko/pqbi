using System;
using Abp.Application.Services.Dto;

namespace PQBI.DashboardCustomization.Dtos
{
    public class WidgetConfigurationDto : EntityDto<Guid>
    {
        public string WidgetGuid { get; set; }

        public string Name { get; set; }

        public string Configuration { get; set; }

    }
}