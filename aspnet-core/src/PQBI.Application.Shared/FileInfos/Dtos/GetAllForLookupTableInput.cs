using Abp.Application.Services.Dto;

namespace PQBI.FileInfos.Dtos;

public class GetAllForLookupTableInput : PagedAndSortedResultRequestDto
{
    public string Filter { get; set; }
}