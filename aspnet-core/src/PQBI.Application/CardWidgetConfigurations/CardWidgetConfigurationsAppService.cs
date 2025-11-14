using PQBI.PQS.CalcEngine;

using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using Abp.Linq.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using PQBI.CardWidgetConfigurations.Dtos;
using PQBI.Dto;
using Abp.Application.Services.Dto;
using PQBI.Authorization;
using Abp.Extensions;
using Abp.Authorization;
using Microsoft.EntityFrameworkCore;
using Abp.UI;
using PQBI.Storage;
using System.Globalization;
using PQBI.TableWidgetConfigurations.Dtos;
using PQBI.TableWidgetConfigurations;

namespace PQBI.CardWidgetConfigurations;

//[AbpAuthorize(AppPermissions.Pages_CardWidgetConfigurations)]
public class CardWidgetConfigurationsAppService : PQBIAppServiceBase, ICardWidgetConfigurationsAppService
{
    private readonly IRepository<CardWidgetConfiguration> _cardWidgetConfigurationRepository;

    public CardWidgetConfigurationsAppService(IRepository<CardWidgetConfiguration> cardWidgetConfigurationRepository)
    {
        _cardWidgetConfigurationRepository = cardWidgetConfigurationRepository;

    }

    public virtual async Task<PagedResultDto<GetCardWidgetConfigurationForViewDto>> GetAll(GetAllCardWidgetConfigurationsInput input)
    {

        var filteredCardWidgetConfigurations = _cardWidgetConfigurationRepository.GetAll()
                    .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), e => false || e.DateRange.Contains(input.Filter) || e.Parameters.Contains(input.Filter));

        var pagedAndFilteredCardWidgetConfigurations = filteredCardWidgetConfigurations
            .OrderBy(input.Sorting ?? "id asc")
            .PageBy(input);

        var cardWidgetConfigurations = from o in pagedAndFilteredCardWidgetConfigurations
                                       select new
                                       {

                                           o.DateRange,
                                           o.Parameters,
                                           o.StyleType,
                                           o.RefreshRate,
                                           Id = o.Id
                                       };

        var totalCount = await filteredCardWidgetConfigurations.CountAsync();

        var dbList = await cardWidgetConfigurations.ToListAsync();
        var results = new List<GetCardWidgetConfigurationForViewDto>();

        foreach (var o in dbList)
        {
            var res = new GetCardWidgetConfigurationForViewDto()
            {
                CardWidgetConfiguration = new CardWidgetConfigurationDto
                {

                    DateRange = o.DateRange,
                    Parameters = o.Parameters,
                    StyleType = o.StyleType,
                    RefreshRate = o.RefreshRate,
                    Id = o.Id,
                }
            };

            results.Add(res);
        }

        return new PagedResultDto<GetCardWidgetConfigurationForViewDto>(
            totalCount,
            results
        );

    }

    public virtual async Task<GetCardWidgetConfigurationForViewDto> GetCardWidgetConfigurationForView(EntityDto<int> input)
    {
        var cardWidgetConfiguration = await _cardWidgetConfigurationRepository.GetAsync(input.Id);

        var output = new GetCardWidgetConfigurationForViewDto { CardWidgetConfiguration = ObjectMapper.Map<CardWidgetConfigurationDto>(cardWidgetConfiguration) };

        return output;
    }

    [AbpAuthorize(AppPermissions.Pages_CardWidgetConfigurations_Edit)]
    public virtual async Task<GetCardWidgetConfigurationForEditOutput> GetCardWidgetConfigurationForEdit(EntityDto input)
    {
        var cardWidgetConfiguration = await _cardWidgetConfigurationRepository.FirstOrDefaultAsync(input.Id);

        var output = new GetCardWidgetConfigurationForEditOutput { CardWidgetConfiguration = ObjectMapper.Map<CreateOrEditCardWidgetConfigurationDto>(cardWidgetConfiguration) };

        return output;
    }

    public virtual async Task CreateOrEdit(CreateOrEditCardWidgetConfigurationDto input)
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

    [AbpAuthorize(AppPermissions.Pages_CardWidgetConfigurations_Create)]
    protected virtual async Task Create(CreateOrEditCardWidgetConfigurationDto input)
    {
        var cardWidgetConfiguration = ObjectMapper.Map<CardWidgetConfiguration>(input);

        if (AbpSession.TenantId != null)
        {
            cardWidgetConfiguration.TenantId = (int?)AbpSession.TenantId;
        }

        await _cardWidgetConfigurationRepository.InsertAsync(cardWidgetConfiguration);

    }

    [AbpAuthorize(AppPermissions.Pages_CardWidgetConfigurations_Create)]
    public async Task<int> CreateAndGetId(CreateOrEditCardWidgetConfigurationDto input)
    {
        var cardWidgetConfiguration = ObjectMapper.Map<CardWidgetConfiguration>(input);

        if (AbpSession.TenantId != null)
        {
            cardWidgetConfiguration.TenantId = (int?)AbpSession.TenantId;
        }

        int id = await _cardWidgetConfigurationRepository.InsertAndGetIdAsync(cardWidgetConfiguration);

        return id;
    }

    [AbpAuthorize(AppPermissions.Pages_CardWidgetConfigurations_Edit)]
    protected virtual async Task Update(CreateOrEditCardWidgetConfigurationDto input)
    {
        var cardWidgetConfiguration = await _cardWidgetConfigurationRepository.FirstOrDefaultAsync((int)input.Id);
        ObjectMapper.Map(input, cardWidgetConfiguration);

    }

    [AbpAuthorize(AppPermissions.Pages_CardWidgetConfigurations_Delete)]
    public virtual async Task Delete(EntityDto input)
    {
        await _cardWidgetConfigurationRepository.DeleteAsync(input.Id);
    }

}