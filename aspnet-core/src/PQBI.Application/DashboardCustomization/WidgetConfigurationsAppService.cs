using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using Abp.Linq.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using PQBI.DashboardCustomization.Dtos;
using PQBI.Dto;
using Abp.Application.Services.Dto;
using PQBI.Authorization;
using Abp.Extensions;
using Abp.Authorization;
using Microsoft.EntityFrameworkCore;
using Abp.UI;
using PQBI.Storage;
using PQBI.Exporting;

namespace PQBI.DashboardCustomization
{
    [AbpAuthorize]
    public class WidgetConfigurationsAppService : PQBIAppServiceBase, IWidgetConfigurationsAppService
    {
        private readonly IRepository<WidgetConfiguration, Guid> _widgetConfigurationRepository;

        public WidgetConfigurationsAppService(IRepository<WidgetConfiguration, Guid> widgetConfigurationRepository)
        {
            _widgetConfigurationRepository = widgetConfigurationRepository;

        }

        public virtual async Task<PagedResultDto<GetWidgetConfigurationForViewDto>> GetAll(GetAllWidgetConfigurationsInput input)
        {

            var filteredWidgetConfigurations = _widgetConfigurationRepository.GetAll()
                        .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), e => false || e.WidgetGuid.Contains(input.Filter) || e.Configuration.Contains(input.Filter))
                        .WhereIf(!string.IsNullOrWhiteSpace(input.WidgetGuidFilter), e => e.WidgetGuid.Contains(input.WidgetGuidFilter));

            var pagedAndFilteredWidgetConfigurations = filteredWidgetConfigurations
                .OrderBy(input.Sorting ?? "id asc")
                .PageBy(input);

            var widgetConfigurations = from o in pagedAndFilteredWidgetConfigurations
                                       select new
                                       {

                                           o.WidgetGuid,
                                           o.Configuration,
                                           Id = o.Id
                                       };

            var totalCount = await filteredWidgetConfigurations.CountAsync();

            var dbList = await widgetConfigurations.ToListAsync();
            var results = new List<GetWidgetConfigurationForViewDto>();

            foreach (var o in dbList)
            {
                var res = new GetWidgetConfigurationForViewDto()
                {
                    WidgetConfiguration = new WidgetConfigurationDto
                    {

                        WidgetGuid = o.WidgetGuid,
                        Configuration = o.Configuration,
                        Id = o.Id,
                    }
                };

                results.Add(res);
            }

            return new PagedResultDto<GetWidgetConfigurationForViewDto>(
                totalCount,
                results
            );

        }

        public virtual async Task<GetWidgetConfigurationForViewDto> GetWidgetConfigurationForView(Guid id)
        {
            var widgetConfiguration = await _widgetConfigurationRepository.GetAsync(id);

            var output = new GetWidgetConfigurationForViewDto { WidgetConfiguration = ObjectMapper.Map<WidgetConfigurationDto>(widgetConfiguration) };

            return output;
        }

        [AbpAuthorize(AppPermissions.WidgetConfigurations_Edit)]
        public virtual async Task<GetWidgetConfigurationForEditOutput> GetWidgetConfigurationForEdit(EntityDto<Guid> input)
        {
            var widgetConfiguration = await _widgetConfigurationRepository.FirstOrDefaultAsync(input.Id);

            var output = new GetWidgetConfigurationForEditOutput { WidgetConfiguration = ObjectMapper.Map<CreateOrEditWidgetConfigurationDto>(widgetConfiguration) };

            return output;
        }

        [AbpAuthorize(AppPermissions.WidgetConfigurations_Edit)]
        public virtual async Task<GetWidgetConfigurationForEditOutput> GetWidgetConfigurationForEditByWidgetId(string widgetId)
        {
            var widgetConfiguration = await _widgetConfigurationRepository.FirstOrDefaultAsync(w => w.WidgetGuid == widgetId);

            var output = new GetWidgetConfigurationForEditOutput { WidgetConfiguration = ObjectMapper.Map<CreateOrEditWidgetConfigurationDto>(widgetConfiguration) };

            return output;
        }
        
        public virtual async Task<GetWidgetConfigurationForEditOutput> CreateOrEdit(CreateOrEditWidgetConfigurationDto input)
        {
            WidgetConfiguration configuration = null!;
            if (input.Id == null)
            {
                configuration = await Create(input);
            }
            else
            {
                configuration = await Update(input);
            }

            var output = new GetWidgetConfigurationForEditOutput { WidgetConfiguration = ObjectMapper.Map<CreateOrEditWidgetConfigurationDto>(configuration) };

            return output;
        }

        [AbpAuthorize(AppPermissions.WidgetConfigurations_Create)]
        protected virtual async Task<WidgetConfiguration> Create(CreateOrEditWidgetConfigurationDto input)
        {
            var widgetConfiguration = ObjectMapper.Map<WidgetConfiguration>(input);
            widgetConfiguration.CreatedOn = DateTime.UtcNow;
            var result = await _widgetConfigurationRepository.InsertAsync(widgetConfiguration);
            return result;
        }

        [AbpAuthorize(AppPermissions.WidgetConfigurations_Edit)]
        protected virtual async Task<WidgetConfiguration> Update(CreateOrEditWidgetConfigurationDto input)
        {
            var widgetConfiguration = await _widgetConfigurationRepository.FirstOrDefaultAsync((Guid)input.Id);
            var result = ObjectMapper.Map(input, widgetConfiguration);
            return result;
        }

        [AbpAuthorize(AppPermissions.WidgetConfigurations_Delete)]
        public virtual async Task Delete(EntityDto<Guid> input)
        {
            await _widgetConfigurationRepository.DeleteAsync(input.Id);
        }

    }
}