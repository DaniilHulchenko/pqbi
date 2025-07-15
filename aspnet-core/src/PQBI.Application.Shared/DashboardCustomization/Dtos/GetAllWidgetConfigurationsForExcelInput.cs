using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;

namespace PQBI.DashboardCustomization.Dtos
{
    public class GetAllWidgetConfigurationsForExcelInput
    {
        public string Filter { get; set; }

        public string WidgetGuidFilter { get; set; }

    }
}