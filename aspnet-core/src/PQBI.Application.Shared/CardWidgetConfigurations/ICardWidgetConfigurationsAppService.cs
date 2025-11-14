using System;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using PQBI.CardWidgetConfigurations.Dtos;
using PQBI.Dto;

namespace PQBI.CardWidgetConfigurations;

public interface ICardWidgetConfigurationsAppService : IApplicationService
{
    Task<PagedResultDto<GetCardWidgetConfigurationForViewDto>> GetAll(GetAllCardWidgetConfigurationsInput input);

    Task<GetCardWidgetConfigurationForViewDto> GetCardWidgetConfigurationForView(EntityDto<int> input);

    Task<GetCardWidgetConfigurationForEditOutput> GetCardWidgetConfigurationForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditCardWidgetConfigurationDto input);

    Task<int> CreateAndGetId(CreateOrEditCardWidgetConfigurationDto input);

    Task Delete(EntityDto input);

}