using System;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using PQBI.BarChartWidgetConfigurations.Dtos;
using PQBI.Dto;

using System.Collections.Generic;

namespace PQBI.BarChartWidgetConfigurations
{
    public interface IBarChartWidgetConfigurationsAppService : IApplicationService
    {
        Task<PagedResultDto<GetBarChartWidgetConfigurationForViewDto>> GetAll(GetAllBarChartWidgetConfigurationsInput input);

        Task<GetBarChartWidgetConfigurationForViewDto> GetBarChartWidgetConfigurationForView(int id);

        Task<GetBarChartWidgetConfigurationForEditOutput> GetBarChartWidgetConfigurationForEdit(EntityDto input);

        Task CreateOrEdit(CreateOrEditBarChartWidgetConfigurationDto input);

        Task<int> CreateAndGetId(CreateOrEditBarChartWidgetConfigurationDto input);

        Task Delete(EntityDto input);

        Task<FileDto> GetBarChartWidgetConfigurationsToExcel(GetAllBarChartWidgetConfigurationsForExcelInput input);

    }
}