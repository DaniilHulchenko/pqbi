using System;
using Abp.Application.Services.Dto;

namespace PQBI.GaugeWidgetConfigurations.Dtos;

public class GaugeWidgetConfigurationDto : EntityDto
{
    public string DateRange { get; set; }

    public string Parameter { get; set; }

    public string Style { get; set; }

    public int RefreshRate { get; set; }

}