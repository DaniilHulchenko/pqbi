using PQBI.BarChartWidgetConfigurations;

using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using Abp.Linq.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using PQBI.BarChartWidgetConfigurations.Exporting;
using PQBI.BarChartWidgetConfigurations.Dtos;
using PQBI.Dto;
using Abp.Application.Services.Dto;
using PQBI.Authorization;
using Abp.Extensions;
using Abp.Authorization;
using Microsoft.EntityFrameworkCore;
using Abp.UI;
using PQBI.Storage;
using PQBI.Exporting;

namespace PQBI.BarChartWidgetConfigurations
{
    [AbpAuthorize(AppPermissions.Pages_BarChartWidgetConfigurations)]
    public class BarChartWidgetConfigurationsAppService : PQBIAppServiceBase, IBarChartWidgetConfigurationsAppService
    {
        private readonly IRepository<BarChartWidgetConfiguration> _barChartWidgetConfigurationRepository;
        private readonly IBarChartWidgetConfigurationsExcelExporter _barChartWidgetConfigurationsExcelExporter;

        public BarChartWidgetConfigurationsAppService(IRepository<BarChartWidgetConfiguration> barChartWidgetConfigurationRepository, IBarChartWidgetConfigurationsExcelExporter barChartWidgetConfigurationsExcelExporter)
        {
            _barChartWidgetConfigurationRepository = barChartWidgetConfigurationRepository;
            _barChartWidgetConfigurationsExcelExporter = barChartWidgetConfigurationsExcelExporter;

        }

        public virtual async Task<PagedResultDto<GetBarChartWidgetConfigurationForViewDto>> GetAll(GetAllBarChartWidgetConfigurationsInput input)
        {
            var typeFilter = input.TypeFilter.HasValue
                        ? (BarChartType)input.TypeFilter
                        : default;

            var filteredBarChartWidgetConfigurations = _barChartWidgetConfigurationRepository.GetAll()
                        .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), e => false || e.Components.Contains(input.Filter) || e.Events.Contains(input.Filter) || e.DateRange.Contains(input.Filter))
                        .WhereIf(input.TypeFilter.HasValue && input.TypeFilter > -1, e => e.Type == typeFilter)
                        .WhereIf(!string.IsNullOrWhiteSpace(input.EventsFilter), e => e.Events.Contains(input.EventsFilter));

            var pagedAndFilteredBarChartWidgetConfigurations = filteredBarChartWidgetConfigurations
                .OrderBy(input.Sorting ?? "id asc")
                .PageBy(input);

            var barChartWidgetConfigurations = from o in pagedAndFilteredBarChartWidgetConfigurations
                                               select new
                                               {

                                                   o.Type,
                                                   o.Components,
                                                   o.Events,
                                                   o.DateRange,
                                                   Id = o.Id
                                               };

            var totalCount = await filteredBarChartWidgetConfigurations.CountAsync();

            var dbList = await barChartWidgetConfigurations.ToListAsync();
            var results = new List<GetBarChartWidgetConfigurationForViewDto>();

            foreach (var o in dbList)
            {
                var res = new GetBarChartWidgetConfigurationForViewDto()
                {
                    BarChartWidgetConfiguration = new BarChartWidgetConfigurationDto
                    {

                        Type = o.Type,
                        Components = o.Components,
                        Events = o.Events,
                        DateRange = o.DateRange,
                        Id = o.Id,
                    }
                };

                results.Add(res);
            }

            return new PagedResultDto<GetBarChartWidgetConfigurationForViewDto>(
                totalCount,
                results
            );

        }

        public virtual async Task<GetBarChartWidgetConfigurationForViewDto> GetBarChartWidgetConfigurationForView(int id)
        {
            var barChartWidgetConfiguration = await _barChartWidgetConfigurationRepository.GetAsync(id);

            var output = new GetBarChartWidgetConfigurationForViewDto { BarChartWidgetConfiguration = ObjectMapper.Map<BarChartWidgetConfigurationDto>(barChartWidgetConfiguration) };

            return output;
        }

        [AbpAuthorize(AppPermissions.Pages_BarChartWidgetConfigurations_Edit)]
        public virtual async Task<GetBarChartWidgetConfigurationForEditOutput> GetBarChartWidgetConfigurationForEdit(EntityDto input)
        {
            var barChartWidgetConfiguration = await _barChartWidgetConfigurationRepository.FirstOrDefaultAsync(input.Id);

            var output = new GetBarChartWidgetConfigurationForEditOutput { BarChartWidgetConfiguration = ObjectMapper.Map<CreateOrEditBarChartWidgetConfigurationDto>(barChartWidgetConfiguration) };

            return output;
        }

        public virtual async Task CreateOrEdit(CreateOrEditBarChartWidgetConfigurationDto input)
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

        [AbpAuthorize(AppPermissions.Pages_BarChartWidgetConfigurations_Create)]
        public virtual async Task<int> CreateAndGetId(CreateOrEditBarChartWidgetConfigurationDto input)
        {
            var barChartWidgetConfiguration = ObjectMapper.Map<BarChartWidgetConfiguration>(input);

            if (AbpSession.TenantId != null)
            {
                barChartWidgetConfiguration.TenantId = (int?)AbpSession.TenantId;
            }

            int id = await _barChartWidgetConfigurationRepository.InsertAndGetIdAsync(barChartWidgetConfiguration);

            return id;

        }

        [AbpAuthorize(AppPermissions.Pages_BarChartWidgetConfigurations_Create)]
        protected virtual async Task Create(CreateOrEditBarChartWidgetConfigurationDto input)
        {
            var barChartWidgetConfiguration = ObjectMapper.Map<BarChartWidgetConfiguration>(input);

            if (AbpSession.TenantId != null)
            {
                barChartWidgetConfiguration.TenantId = (int?)AbpSession.TenantId;
            }

            await _barChartWidgetConfigurationRepository.InsertAsync(barChartWidgetConfiguration);

        }

        [AbpAuthorize(AppPermissions.Pages_BarChartWidgetConfigurations_Edit)]
        protected virtual async Task Update(CreateOrEditBarChartWidgetConfigurationDto input)
        {
            var barChartWidgetConfiguration = await _barChartWidgetConfigurationRepository.FirstOrDefaultAsync((int)input.Id);
            ObjectMapper.Map(input, barChartWidgetConfiguration);

        }

        [AbpAuthorize(AppPermissions.Pages_BarChartWidgetConfigurations_Delete)]
        public virtual async Task Delete(EntityDto input)
        {
            await _barChartWidgetConfigurationRepository.DeleteAsync(input.Id);
        }

        public virtual async Task<FileDto> GetBarChartWidgetConfigurationsToExcel(GetAllBarChartWidgetConfigurationsForExcelInput input)
        {
            var typeFilter = input.TypeFilter.HasValue
                        ? (BarChartType)input.TypeFilter
                        : default;

            var filteredBarChartWidgetConfigurations = _barChartWidgetConfigurationRepository.GetAll()
                        .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), e => false || e.Components.Contains(input.Filter) || e.Events.Contains(input.Filter) || e.DateRange.Contains(input.Filter))
                        .WhereIf(input.TypeFilter.HasValue && input.TypeFilter > -1, e => e.Type == typeFilter)
                        .WhereIf(!string.IsNullOrWhiteSpace(input.EventsFilter), e => e.Events.Contains(input.EventsFilter));

            var query = from o in filteredBarChartWidgetConfigurations
                        select new GetBarChartWidgetConfigurationForViewDto()
                        {
                            BarChartWidgetConfiguration = new BarChartWidgetConfigurationDto
                            {
                                Type = o.Type,
                                Components = o.Components,
                                Events = o.Events,
                                DateRange = o.DateRange,
                                Id = o.Id
                            }
                        };

            var barChartWidgetConfigurationListDtos = await query.ToListAsync();

            return _barChartWidgetConfigurationsExcelExporter.ExportToFile(barChartWidgetConfigurationListDtos);
        }

    }
}