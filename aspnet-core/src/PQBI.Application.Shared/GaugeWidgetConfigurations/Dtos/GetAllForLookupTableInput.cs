using Abp.Application.Services.Dto;

namespace PQBI.GaugeWidgetConfigurations.Dtos;

public class GetAllForLookupTableInput : PagedAndSortedResultRequestDto
{
    public string Filter { get; set; }
}