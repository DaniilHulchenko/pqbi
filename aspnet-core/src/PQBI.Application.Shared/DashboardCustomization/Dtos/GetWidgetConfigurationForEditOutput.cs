using System;
using Abp.Application.Services.Dto;
using System.ComponentModel.DataAnnotations;

namespace PQBI.DashboardCustomization.Dtos
{
    public class GetWidgetConfigurationForEditOutput
    {
        public CreateOrEditWidgetConfigurationDto WidgetConfiguration { get; set; }

    }
}