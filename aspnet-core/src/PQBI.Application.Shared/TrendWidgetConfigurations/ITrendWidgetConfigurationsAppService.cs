using System;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using PQBI.TrendWidgetConfigurations.Dtos;
using PQBI.Dto;

using System.Collections.Generic;

namespace PQBI.TrendWidgetConfigurations
{
    public interface ITrendWidgetConfigurationsAppService : IApplicationService
    {
        Task<PagedResultDto<GetTrendWidgetConfigurationForViewDto>> GetAll(GetAllTrendWidgetConfigurationsInput input);

        Task<GetTrendWidgetConfigurationForViewDto> GetTrendWidgetConfigurationForView(int id);

        Task<GetTrendWidgetConfigurationForEditOutput> GetTrendWidgetConfigurationForEdit(EntityDto input);

        Task CreateOrEdit(CreateOrEditTrendWidgetConfigurationDto input);

        Task<int> CreateAndGetId(CreateOrEditTrendWidgetConfigurationDto input);

        Task Delete(EntityDto input);

    }
}