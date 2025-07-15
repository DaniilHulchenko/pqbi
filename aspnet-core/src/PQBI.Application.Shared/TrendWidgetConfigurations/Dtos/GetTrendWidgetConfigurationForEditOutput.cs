using System;
using Abp.Application.Services.Dto;
using System.ComponentModel.DataAnnotations;

namespace PQBI.TrendWidgetConfigurations.Dtos
{
    public class GetTrendWidgetConfigurationForEditOutput
    {
        public CreateOrEditTrendWidgetConfigurationDto TrendWidgetConfiguration { get; set; }

    }
}