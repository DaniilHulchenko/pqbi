using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using Abp.Linq.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using PQBI.GaugeWidgetConfigurations.Dtos;
using PQBI.Dto;
using Abp.Application.Services.Dto;
using PQBI.Authorization;
using Abp.Extensions;
using Abp.Authorization;
using Microsoft.EntityFrameworkCore;
using Abp.UI;
using PQBI.Storage;
using System.Globalization;

namespace PQBI.GaugeWidgetConfigurations;

[AbpAuthorize(AppPermissions.Pages_GaugeWidgetConfigurations)]
public class GaugeWidgetConfigurationsAppService : PQBIAppServiceBase, IGaugeWidgetConfigurationsAppService
{
    private readonly IRepository<GaugeWidgetConfiguration> _gaugeWidgetConfigurationRepository;

    public GaugeWidgetConfigurationsAppService(IRepository<GaugeWidgetConfiguration> gaugeWidgetConfigurationRepository)
    {
        _gaugeWidgetConfigurationRepository = gaugeWidgetConfigurationRepository;

    }

    public virtual async Task<PagedResultDto<GetGaugeWidgetConfigurationForViewDto>> GetAll(GetAllGaugeWidgetConfigurationsInput input)
    {

        var filteredGaugeWidgetConfigurations = _gaugeWidgetConfigurationRepository.GetAll()
                    .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), e => false || e.DateRange.Contains(input.Filter) || e.Parameter.Contains(input.Filter) || e.Style.Contains(input.Filter));

        var pagedAndFilteredGaugeWidgetConfigurations = filteredGaugeWidgetConfigurations
            .OrderBy(input.Sorting ?? "id asc")
            .PageBy(input);

        var gaugeWidgetConfigurations = from o in pagedAndFilteredGaugeWidgetConfigurations
                                        select new
                                        {

                                            o.DateRange,
                                            o.Parameter,
                                            o.Style,
                                            o.RefreshRate,
                                            Id = o.Id
                                        };

        var totalCount = await filteredGaugeWidgetConfigurations.CountAsync();

        var dbList = await gaugeWidgetConfigurations.ToListAsync();
        var results = new List<GetGaugeWidgetConfigurationForViewDto>();

        foreach (var o in dbList)
        {
            var res = new GetGaugeWidgetConfigurationForViewDto()
            {
                GaugeWidgetConfiguration = new GaugeWidgetConfigurationDto
                {

                    DateRange = o.DateRange,
                    Parameter = o.Parameter,
                    Style = o.Style,
                    RefreshRate = o.RefreshRate,
                    Id = o.Id,
                }
            };

            results.Add(res);
        }

        return new PagedResultDto<GetGaugeWidgetConfigurationForViewDto>(
            totalCount,
            results
        );

    }

    public virtual async Task<GetGaugeWidgetConfigurationForViewDto> GetGaugeWidgetConfigurationForView(EntityDto<int> input)
    {
        var gaugeWidgetConfiguration = await _gaugeWidgetConfigurationRepository.GetAsync(input.Id);

        var output = new GetGaugeWidgetConfigurationForViewDto { GaugeWidgetConfiguration = ObjectMapper.Map<GaugeWidgetConfigurationDto>(gaugeWidgetConfiguration) };

        return output;
    }

    [AbpAuthorize(AppPermissions.Pages_GaugeWidgetConfigurations_Edit)]
    public virtual async Task<GetGaugeWidgetConfigurationForEditOutput> GetGaugeWidgetConfigurationForEdit(EntityDto input)
    {
        var gaugeWidgetConfiguration = await _gaugeWidgetConfigurationRepository.FirstOrDefaultAsync(input.Id);

        var output = new GetGaugeWidgetConfigurationForEditOutput { GaugeWidgetConfiguration = ObjectMapper.Map<CreateOrEditGaugeWidgetConfigurationDto>(gaugeWidgetConfiguration) };

        return output;
    }

    public virtual async Task CreateOrEdit(CreateOrEditGaugeWidgetConfigurationDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GaugeWidgetConfigurations_Create)]
    protected virtual async Task Create(CreateOrEditGaugeWidgetConfigurationDto input)
    {
        var gaugeWidgetConfiguration = ObjectMapper.Map<GaugeWidgetConfiguration>(input);

        if (AbpSession.TenantId != null)
        {
            gaugeWidgetConfiguration.TenantId = (int?)AbpSession.TenantId;
        }

        await _gaugeWidgetConfigurationRepository.InsertAsync(gaugeWidgetConfiguration);

    }

    [AbpAuthorize(AppPermissions.Pages_GaugeWidgetConfigurations_Edit)]
    protected virtual async Task Update(CreateOrEditGaugeWidgetConfigurationDto input)
    {
        var gaugeWidgetConfiguration = await _gaugeWidgetConfigurationRepository.FirstOrDefaultAsync((int)input.Id);
        ObjectMapper.Map(input, gaugeWidgetConfiguration);

    }

    [AbpAuthorize(AppPermissions.Pages_GaugeWidgetConfigurations_Create)]
    public async Task<int> CreateAndGetId(CreateOrEditGaugeWidgetConfigurationDto input)
    {
        var gaugeWidgetConfiguration = ObjectMapper.Map<GaugeWidgetConfiguration>(input);

        if (AbpSession.TenantId != null)
        {
            gaugeWidgetConfiguration.TenantId = (int?)AbpSession.TenantId;
        }

        var result = await _gaugeWidgetConfigurationRepository.InsertAndGetIdAsync(gaugeWidgetConfiguration);

        return result;
    }

    [AbpAuthorize(AppPermissions.Pages_GaugeWidgetConfigurations_Delete)]
    public virtual async Task Delete(EntityDto input)
    {
        await _gaugeWidgetConfigurationRepository.DeleteAsync(input.Id);
    }
}