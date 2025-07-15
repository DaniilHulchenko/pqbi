using Abp.Application.Services.Dto;

namespace PQBI.Groups.Dtos;

public class GetAllForLookupTableInput : PagedAndSortedResultRequestDto
{
    public string Filter { get; set; }
}