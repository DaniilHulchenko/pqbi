using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using PQBI.GaugeWidgetConfigurations.Dtos;

namespace PQBI.GaugeWidgetConfigurations;

public interface IGaugeWidgetConfigurationsAppService : IApplicationService
{
    Task<PagedResultDto<GetGaugeWidgetConfigurationForViewDto>> GetAll(GetAllGaugeWidgetConfigurationsInput input);

    Task<GetGaugeWidgetConfigurationForViewDto> GetGaugeWidgetConfigurationForView(EntityDto<int> input);

    Task<GetGaugeWidgetConfigurationForEditOutput> GetGaugeWidgetConfigurationForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditGaugeWidgetConfigurationDto input);

    Task<int> CreateAndGetId(CreateOrEditGaugeWidgetConfigurationDto input);

    Task Delete(EntityDto input);

}