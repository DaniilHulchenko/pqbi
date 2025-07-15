using Abp.Application.Services.Dto;

namespace PQBI.BarChartWidgetConfigurations.Dtos
{
    public class GetAllForLookupTableInput : PagedAndSortedResultRequestDto
    {
        public string Filter { get; set; }
    }
}