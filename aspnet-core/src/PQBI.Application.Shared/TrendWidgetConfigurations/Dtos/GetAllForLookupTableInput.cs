using Abp.Application.Services.Dto;

namespace PQBI.TrendWidgetConfigurations.Dtos
{
    public class GetAllForLookupTableInput : PagedAndSortedResultRequestDto
    {
        public string Filter { get; set; }
    }
}