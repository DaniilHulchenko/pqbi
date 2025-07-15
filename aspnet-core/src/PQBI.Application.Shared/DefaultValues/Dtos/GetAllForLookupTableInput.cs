using Abp.Application.Services.Dto;

namespace PQBI.DefaultValues.Dtos
{
    public class GetAllForLookupTableInput : PagedAndSortedResultRequestDto
    {
        public string Filter { get; set; }
    }
}