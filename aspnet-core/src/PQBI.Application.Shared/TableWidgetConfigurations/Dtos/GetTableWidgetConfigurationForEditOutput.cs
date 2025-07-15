using System;
using Abp.Application.Services.Dto;
using System.ComponentModel.DataAnnotations;

namespace PQBI.TableWidgetConfigurations.Dtos
{
    public class GetTableWidgetConfigurationForEditOutput
    {
        public CreateOrEditTableWidgetConfigurationDto TableWidgetConfiguration { get; set; }

    }
}