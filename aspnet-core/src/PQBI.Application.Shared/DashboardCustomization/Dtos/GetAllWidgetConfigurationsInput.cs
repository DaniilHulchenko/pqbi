using Abp.Application.Services.Dto;
using System;

namespace PQBI.DashboardCustomization.Dtos
{
    public class GetAllWidgetConfigurationsInput : PagedAndSortedResultRequestDto
    {
        public string Filter { get; set; }

        public string WidgetGuidFilter { get; set; }

    }
}