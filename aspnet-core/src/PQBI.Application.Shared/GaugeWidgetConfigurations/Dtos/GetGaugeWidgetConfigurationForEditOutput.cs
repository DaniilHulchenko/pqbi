using System;
using Abp.Application.Services.Dto;
using System.ComponentModel.DataAnnotations;

namespace PQBI.GaugeWidgetConfigurations.Dtos;

public class GetGaugeWidgetConfigurationForEditOutput
{
    public CreateOrEditGaugeWidgetConfigurationDto GaugeWidgetConfiguration { get; set; }

}