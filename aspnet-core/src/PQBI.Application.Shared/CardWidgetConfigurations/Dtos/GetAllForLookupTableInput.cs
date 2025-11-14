using Abp.Application.Services.Dto;

namespace PQBI.CardWidgetConfigurations.Dtos;

public class GetAllForLookupTableInput : PagedAndSortedResultRequestDto
{
    public string Filter { get; set; }
}