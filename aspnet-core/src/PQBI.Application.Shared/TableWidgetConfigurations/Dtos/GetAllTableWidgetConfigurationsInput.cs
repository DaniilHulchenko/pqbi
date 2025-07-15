using Abp.Application.Services.Dto;
using System;

namespace PQBI.TableWidgetConfigurations.Dtos
{
    public class GetAllTableWidgetConfigurationsInput : PagedAndSortedResultRequestDto
    {
        public string Filter { get; set; }

        public string ConfigurationFilter { get; set; }

    }
}