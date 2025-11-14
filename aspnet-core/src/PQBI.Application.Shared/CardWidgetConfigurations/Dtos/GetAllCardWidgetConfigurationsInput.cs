using Abp.Application.Services.Dto;
using System;

namespace PQBI.CardWidgetConfigurations.Dtos;

public class GetAllCardWidgetConfigurationsInput : PagedAndSortedResultRequestDto
{
    public string Filter { get; set; }

}