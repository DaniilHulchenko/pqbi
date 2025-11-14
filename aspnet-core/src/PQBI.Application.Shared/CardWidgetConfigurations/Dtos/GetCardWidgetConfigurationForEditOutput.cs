using System;
using Abp.Application.Services.Dto;
using System.ComponentModel.DataAnnotations;

namespace PQBI.CardWidgetConfigurations.Dtos;

public class GetCardWidgetConfigurationForEditOutput
{
    public CreateOrEditCardWidgetConfigurationDto CardWidgetConfiguration { get; set; }

}