using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using Abp.Linq.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using PQBI.TableWidgetConfigurations.Exporting;
using PQBI.TableWidgetConfigurations.Dtos;
using PQBI.Dto;
using Abp.Application.Services.Dto;
using PQBI.Authorization;
using Abp.Extensions;
using Abp.Authorization;
using Microsoft.EntityFrameworkCore;
using Abp.UI;
using PQBI.Storage;
using PQBI.Exporting;

namespace PQBI.TableWidgetConfigurations
{
    [AbpAuthorize(AppPermissions.Pages_TableWidgetConfigurations)]
    public class TableWidgetConfigurationsAppService : PQBIAppServiceBase, ITableWidgetConfigurationsAppService
    {
        private readonly IRepository<TableWidgetConfiguration> _tableWidgetConfigurationRepository;
        private readonly ITableWidgetConfigurationsExcelExporter _tableWidgetConfigurationsExcelExporter;

        public TableWidgetConfigurationsAppService(IRepository<TableWidgetConfiguration> tableWidgetConfigurationRepository, ITableWidgetConfigurationsExcelExporter tableWidgetConfigurationsExcelExporter)
        {
            _tableWidgetConfigurationRepository = tableWidgetConfigurationRepository;
            _tableWidgetConfigurationsExcelExporter = tableWidgetConfigurationsExcelExporter;

        }

        public virtual async Task<PagedResultDto<GetTableWidgetConfigurationForViewDto>> GetAll(GetAllTableWidgetConfigurationsInput input)
        {

            var filteredTableWidgetConfigurations = _tableWidgetConfigurationRepository.GetAll()
                        .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), e => false || e.Configuration.Contains(input.Filter) || e.Components.Contains(input.Filter) || e.DateRange.Contains(input.Filter))
                        .WhereIf(!string.IsNullOrWhiteSpace(input.ConfigurationFilter), e => e.Configuration.Contains(input.ConfigurationFilter));

            var pagedAndFilteredTableWidgetConfigurations = filteredTableWidgetConfigurations
                .OrderBy(input.Sorting ?? "id asc")
                .PageBy(input);

            var tableWidgetConfigurations = from o in pagedAndFilteredTableWidgetConfigurations
                                            select new
                                            {

                                                o.Configuration,
                                                o.Components,
                                                o.DateRange,
                                                Id = o.Id
                                            };

            var totalCount = await filteredTableWidgetConfigurations.CountAsync();

            var dbList = await tableWidgetConfigurations.ToListAsync();
            var results = new List<GetTableWidgetConfigurationForViewDto>();

            foreach (var o in dbList)
            {
                var res = new GetTableWidgetConfigurationForViewDto()
                {
                    TableWidgetConfiguration = new TableWidgetConfigurationDto
                    {
                        Configuration = o.Configuration,
                        Components = o.Components,
                        DateRange = o.DateRange,
                        Id = o.Id,
                    }
                };

                results.Add(res);
            }

            return new PagedResultDto<GetTableWidgetConfigurationForViewDto>(
                totalCount,
                results
            );

        }

        public virtual async Task<GetTableWidgetConfigurationForViewDto> GetTableWidgetConfigurationForView(int id)
        {
            var tableWidgetConfiguration = await _tableWidgetConfigurationRepository.GetAsync(id);

            var output = new GetTableWidgetConfigurationForViewDto { TableWidgetConfiguration = ObjectMapper.Map<TableWidgetConfigurationDto>(tableWidgetConfiguration) };

            return output;
        }

        [AbpAuthorize(AppPermissions.Pages_TableWidgetConfigurations_Edit)]
        public virtual async Task<GetTableWidgetConfigurationForEditOutput> GetTableWidgetConfigurationForEdit(EntityDto input)
        {
            var tableWidgetConfiguration = await _tableWidgetConfigurationRepository.FirstOrDefaultAsync(input.Id);

            var output = new GetTableWidgetConfigurationForEditOutput { TableWidgetConfiguration = ObjectMapper.Map<CreateOrEditTableWidgetConfigurationDto>(tableWidgetConfiguration) };

            return output;
        }

        public virtual async Task CreateOrEdit(CreateOrEditTableWidgetConfigurationDto input)
        {
            if (input.Id == null)
            {
                await Create(input);
            }
            else
            {
                await Update(input);
            }
        }

        [AbpAuthorize(AppPermissions.Pages_TableWidgetConfigurations_Create)]
        public virtual async Task<int> CreateAndGetId(CreateOrEditTableWidgetConfigurationDto input)
        {
            var tableWidgetConfiguration = ObjectMapper.Map<TableWidgetConfiguration>(input);

            if (AbpSession.TenantId != null)
            {
                tableWidgetConfiguration.TenantId = (int)AbpSession.TenantId;
            }

            int id = await _tableWidgetConfigurationRepository.InsertAndGetIdAsync(tableWidgetConfiguration);

            return id;
        }

        [AbpAuthorize(AppPermissions.Pages_TableWidgetConfigurations_Create)]
        protected virtual async Task Create(CreateOrEditTableWidgetConfigurationDto input)
        {
            var tableWidgetConfiguration = ObjectMapper.Map<TableWidgetConfiguration>(input);

            if (AbpSession.TenantId != null)
            {
                tableWidgetConfiguration.TenantId = (int)AbpSession.TenantId;
            }

            await _tableWidgetConfigurationRepository.InsertAsync(tableWidgetConfiguration);

        }

        [AbpAuthorize(AppPermissions.Pages_TableWidgetConfigurations_Edit)]
        protected virtual async Task Update(CreateOrEditTableWidgetConfigurationDto input)
        {
            var tableWidgetConfiguration = await _tableWidgetConfigurationRepository.FirstOrDefaultAsync((int)input.Id);
            ObjectMapper.Map(input, tableWidgetConfiguration);

        }

        [AbpAuthorize(AppPermissions.Pages_TableWidgetConfigurations_Delete)]
        public virtual async Task Delete(EntityDto input)
        {
            await _tableWidgetConfigurationRepository.DeleteAsync(input.Id);
        }

        public virtual async Task<FileDto> GetTableWidgetConfigurationsToExcel(GetAllTableWidgetConfigurationsForExcelInput input)
        {

            var filteredTableWidgetConfigurations = _tableWidgetConfigurationRepository.GetAll()
                        .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), e => false || e.Configuration.Contains(input.Filter) || e.Components.Contains(input.Filter) || e.DateRange.Contains(input.Filter))
                        .WhereIf(!string.IsNullOrWhiteSpace(input.ConfigurationFilter), e => e.Configuration.Contains(input.ConfigurationFilter));

            var query = from o in filteredTableWidgetConfigurations
                        select new GetTableWidgetConfigurationForViewDto()
                        {
                            TableWidgetConfiguration = new TableWidgetConfigurationDto
                            {
                                Configuration = o.Configuration,
                                Components = o.Components,
                                DateRange = o.DateRange,
                                Id = o.Id
                            }
                        };

            var tableWidgetConfigurationListDtos = await query.ToListAsync();

            return _tableWidgetConfigurationsExcelExporter.ExportToFile(tableWidgetConfigurationListDtos);
        }

    }
}