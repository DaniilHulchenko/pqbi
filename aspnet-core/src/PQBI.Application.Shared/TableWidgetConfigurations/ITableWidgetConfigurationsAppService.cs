using System;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using PQBI.TableWidgetConfigurations.Dtos;
using PQBI.Dto;

using System.Collections.Generic;

namespace PQBI.TableWidgetConfigurations
{
    public interface ITableWidgetConfigurationsAppService : IApplicationService
    {
        Task<PagedResultDto<GetTableWidgetConfigurationForViewDto>> GetAll(GetAllTableWidgetConfigurationsInput input);

        Task<GetTableWidgetConfigurationForViewDto> GetTableWidgetConfigurationForView(int id);

        Task<GetTableWidgetConfigurationForEditOutput> GetTableWidgetConfigurationForEdit(EntityDto input);

        Task CreateOrEdit(CreateOrEditTableWidgetConfigurationDto input);

        Task<int> CreateAndGetId(CreateOrEditTableWidgetConfigurationDto input);

        Task Delete(EntityDto input);

        Task<FileDto> GetTableWidgetConfigurationsToExcel(GetAllTableWidgetConfigurationsForExcelInput input);

    }
}