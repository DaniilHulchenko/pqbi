using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using Abp.Linq.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using PQBI.TrendWidgetConfigurations.Dtos;
using PQBI.Dto;
using Abp.Application.Services.Dto;
using PQBI.Authorization;
using Abp.Extensions;
using Abp.Authorization;
using Microsoft.EntityFrameworkCore;
using Abp.UI;
using PQBI.Storage;
using PQBI.Exporting;

namespace PQBI.TrendWidgetConfigurations
{
    [AbpAuthorize(AppPermissions.Pages_TrendWidgetConfigurations)]
    public class TrendWidgetConfigurationsAppService : PQBIAppServiceBase, ITrendWidgetConfigurationsAppService
    {
        private readonly IRepository<TrendWidgetConfiguration> _trendWidgetConfigurationRepository;

        public TrendWidgetConfigurationsAppService(IRepository<TrendWidgetConfiguration> trendWidgetConfigurationRepository)
        {
            _trendWidgetConfigurationRepository = trendWidgetConfigurationRepository;

        }

        public virtual async Task<PagedResultDto<GetTrendWidgetConfigurationForViewDto>> GetAll(GetAllTrendWidgetConfigurationsInput input)
        {

            var filteredTrendWidgetConfigurations = _trendWidgetConfigurationRepository.GetAll()
                        .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), e => false || e.DateRange.Contains(input.Filter) || e.Resolution.Contains(input.Filter) || e.Parameters.Contains(input.Filter));

            var pagedAndFilteredTrendWidgetConfigurations = filteredTrendWidgetConfigurations
                .OrderBy(input.Sorting ?? "id asc")
                .PageBy(input);

            var trendWidgetConfigurations = from o in pagedAndFilteredTrendWidgetConfigurations
                                            select new
                                            {

                                                o.DateRange,
                                                o.Resolution,
                                                o.Parameters,
                                                Id = o.Id
                                            };

            var totalCount = await filteredTrendWidgetConfigurations.CountAsync();

            var dbList = await trendWidgetConfigurations.ToListAsync();
            var results = new List<GetTrendWidgetConfigurationForViewDto>();

            foreach (var o in dbList)
            {
                var res = new GetTrendWidgetConfigurationForViewDto()
                {
                    TrendWidgetConfiguration = new TrendWidgetConfigurationDto
                    {

                        DateRange = o.DateRange,
                        Resolution = o.Resolution,
                        Parameters = o.Parameters,
                        Id = o.Id,
                    }
                };

                results.Add(res);
            }

            return new PagedResultDto<GetTrendWidgetConfigurationForViewDto>(
                totalCount,
                results
            );

        }

        public virtual async Task<GetTrendWidgetConfigurationForViewDto> GetTrendWidgetConfigurationForView(int id)
        {
            var trendWidgetConfiguration = await _trendWidgetConfigurationRepository.GetAsync(id);

            var output = new GetTrendWidgetConfigurationForViewDto { TrendWidgetConfiguration = ObjectMapper.Map<TrendWidgetConfigurationDto>(trendWidgetConfiguration) };

            return output;
        }

        [AbpAuthorize(AppPermissions.Pages_TrendWidgetConfigurations_Edit)]
        public virtual async Task<GetTrendWidgetConfigurationForEditOutput> GetTrendWidgetConfigurationForEdit(EntityDto input)
        {
            var trendWidgetConfiguration = await _trendWidgetConfigurationRepository.FirstOrDefaultAsync(input.Id);

            var output = new GetTrendWidgetConfigurationForEditOutput { TrendWidgetConfiguration = ObjectMapper.Map<CreateOrEditTrendWidgetConfigurationDto>(trendWidgetConfiguration) };

            return output;
        }

        [AbpAuthorize(AppPermissions.Pages_TrendWidgetConfigurations_Create)]
        public async Task<int> CreateAndGetId(CreateOrEditTrendWidgetConfigurationDto input)
        {
            var trendWidgetConfiguration = ObjectMapper.Map<TrendWidgetConfiguration>(input);

            if (AbpSession.TenantId != null)
            {
                trendWidgetConfiguration.TenantId = (int)AbpSession.TenantId;
            }

            int id = await _trendWidgetConfigurationRepository.InsertAndGetIdAsync(trendWidgetConfiguration);

            return id;
        }

        public virtual async Task CreateOrEdit(CreateOrEditTrendWidgetConfigurationDto input)
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

        [AbpAuthorize(AppPermissions.Pages_TrendWidgetConfigurations_Create)]
        protected virtual async Task Create(CreateOrEditTrendWidgetConfigurationDto input)
        {
            var trendWidgetConfiguration = ObjectMapper.Map<TrendWidgetConfiguration>(input);

            if (AbpSession.TenantId != null)
            {
                trendWidgetConfiguration.TenantId = (int?)AbpSession.TenantId;
            }

            await _trendWidgetConfigurationRepository.InsertAsync(trendWidgetConfiguration);

        }

        [AbpAuthorize(AppPermissions.Pages_TrendWidgetConfigurations_Edit)]
        protected virtual async Task Update(CreateOrEditTrendWidgetConfigurationDto input)
        {
            var trendWidgetConfiguration = await _trendWidgetConfigurationRepository.FirstOrDefaultAsync((int)input.Id);
            ObjectMapper.Map(input, trendWidgetConfiguration);

        }

        [AbpAuthorize(AppPermissions.Pages_TrendWidgetConfigurations_Delete)]
        public virtual async Task Delete(EntityDto input)
        {
            await _trendWidgetConfigurationRepository.DeleteAsync(input.Id);
        }

    }
}