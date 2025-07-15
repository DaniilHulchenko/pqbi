using System;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using PQBI.DashboardCustomization.Dtos;
using PQBI.Dto;

using System.Collections.Generic;

namespace PQBI.DashboardCustomization
{
    public interface IWidgetConfigurationsAppService : IApplicationService
    {
        Task<PagedResultDto<GetWidgetConfigurationForViewDto>> GetAll(GetAllWidgetConfigurationsInput input);

        Task<GetWidgetConfigurationForViewDto> GetWidgetConfigurationForView(Guid id);

        Task<GetWidgetConfigurationForEditOutput> GetWidgetConfigurationForEdit(EntityDto<Guid> input);

        Task<GetWidgetConfigurationForEditOutput> GetWidgetConfigurationForEditByWidgetId(string widgetId);

        Task<GetWidgetConfigurationForEditOutput> CreateOrEdit(CreateOrEditWidgetConfigurationDto input);

        Task Delete(EntityDto<Guid> input);

    }
}