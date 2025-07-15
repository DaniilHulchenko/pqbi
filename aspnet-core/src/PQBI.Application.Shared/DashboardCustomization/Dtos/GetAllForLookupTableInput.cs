using Abp.Application.Services.Dto;

namespace PQBI.DashboardCustomization.Dtos
{
    public class GetAllForLookupTableInput : PagedAndSortedResultRequestDto
    {
        public string Filter { get; set; }
    }
}