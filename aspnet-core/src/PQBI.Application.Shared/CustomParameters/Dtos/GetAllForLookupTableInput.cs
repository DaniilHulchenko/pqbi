using Abp.Application.Services.Dto;

namespace PQBI.CustomParameters.Dtos;

public class GetAllForLookupTableInput : PagedAndSortedResultRequestDto
{
    public string Filter { get; set; }
}