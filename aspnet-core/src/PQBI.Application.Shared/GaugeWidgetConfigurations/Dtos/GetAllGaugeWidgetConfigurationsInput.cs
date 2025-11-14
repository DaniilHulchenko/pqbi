using Abp.Application.Services.Dto;
using System;

namespace PQBI.GaugeWidgetConfigurations.Dtos;

public class GetAllGaugeWidgetConfigurationsInput : PagedAndSortedResultRequestDto
{
    public string Filter { get; set; }

}