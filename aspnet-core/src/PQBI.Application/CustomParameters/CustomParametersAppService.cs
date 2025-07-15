using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using Abp.Linq.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using PQBI.CustomParameters.Exporting;
using PQBI.CustomParameters.Dtos;
using PQBI.Dto;
using Abp.Application.Services.Dto;
using PQBI.Authorization;
using Abp.Extensions;
using Abp.Authorization;
using Microsoft.EntityFrameworkCore;
using Abp.UI;
using PQBI.Storage;
using PQBI.Exporting;

namespace PQBI.CustomParameters;

[AbpAuthorize(AppPermissions.Pages_CustomParameters)]
public class CustomParametersAppService : PQBIAppServiceBase, ICustomParametersAppService
{
    private readonly IRepository<CustomParameter> _customParameterRepository;
    private readonly ICustomParametersExcelExporter _customParametersExcelExporter;

    public CustomParametersAppService(IRepository<CustomParameter> customParameterRepository, ICustomParametersExcelExporter customParametersExcelExporter)
    {
        _customParameterRepository = customParameterRepository;
        _customParametersExcelExporter = customParametersExcelExporter;

    }

    public virtual async Task<PagedResultDto<GetCustomParameterForViewDto>> GetAll(GetAllCustomParametersInput input)
    {

        var filteredCustomParameters = _customParameterRepository.GetAll()
                    .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), e => false || e.Name.Contains(input.Filter) || e.AggregationFunction.Contains(input.Filter) || e.Type.Contains(input.Filter) || e.InnerCustomParameters.Contains(input.Filter) || e.CustomBaseDataList.Contains(input.Filter))
                        .WhereIf(!string.IsNullOrWhiteSpace(input.NameFilter), e => e.Name.Contains(input.NameFilter))
                        .WhereIf(!string.IsNullOrWhiteSpace(input.AggregationFunctionFilter), e => e.AggregationFunction.Contains(input.AggregationFunctionFilter))
                        .WhereIf(!string.IsNullOrWhiteSpace(input.TypeFilter), e => e.Type.Contains(input.TypeFilter))
                        .WhereIf(input.MinResolutionInSecondsFilter != null, e => e.ResolutionInSeconds >= input.MinResolutionInSecondsFilter)
                        .WhereIf(input.MaxResolutionInSecondsFilter != null, e => e.ResolutionInSeconds <= input.MaxResolutionInSecondsFilter)
                        .WhereIf(!string.IsNullOrWhiteSpace(input.CustomBaseDataListFilter), e => e.CustomBaseDataList.Contains(input.CustomBaseDataListFilter));

        var pagedAndFilteredCustomParameters = filteredCustomParameters
            .OrderBy(input.Sorting ?? "id asc")
            .PageBy(input);

        var customParameters = from o in pagedAndFilteredCustomParameters
                               select new
                               {

                                   o.Name,
                                   o.AggregationFunction,
                                   o.Type,
                                   Id = o.Id,
                                   ResolutionInSeconds = o.ResolutionInSeconds,
                               };

        var totalCount = await filteredCustomParameters.CountAsync();

        var dbList = await customParameters.ToListAsync();
        var results = new List<GetCustomParameterForViewDto>();

        foreach (var o in dbList)
        {
            var res = new GetCustomParameterForViewDto()
            {
                CustomParameter = new CustomParameterDto
                {
                    ResolutionInSeconds = o.ResolutionInSeconds,
                    Name = o.Name,
                    AggregationFunction = o.AggregationFunction,
                    Type = o.Type,
                    Id = o.Id,
                }
            };

            results.Add(res);
        }

        return new PagedResultDto<GetCustomParameterForViewDto>(
            totalCount,
            results
        );

    }

    public virtual async Task<GetCustomParameterForViewDto> GetCustomParameterForView(int id)
    {
        var customParameter = await _customParameterRepository.GetAsync(id);

        var output = new GetCustomParameterForViewDto { CustomParameter = ObjectMapper.Map<CustomParameterDto>(customParameter) };

        return output;
    }

    [AbpAuthorize(AppPermissions.Pages_CustomParameters_Edit)]
    public virtual async Task<GetCustomParameterForEditOutput> GetCustomParameterForEdit(EntityDto input)
    {
        var customParameter = await _customParameterRepository.FirstOrDefaultAsync(input.Id);

        var output = new GetCustomParameterForEditOutput { CustomParameter = ObjectMapper.Map<CreateOrEditCustomParameterDto>(customParameter) };

        return output;
    }

    public virtual async Task<CreateOrEditCustomParameterDto> CreateOrEdit(CreateOrEditCustomParameterDto input)
    {
        var response = default(CreateOrEditCustomParameterDto);
        if (input.Id == null)
        {
            response = await Create(input);
        }
        else
        {
            response = await Update(input);
        }
        return response;
    }

    [AbpAuthorize(AppPermissions.Pages_CustomParameters_Create)]

    protected virtual async Task<CreateOrEditCustomParameterDto> Create(CreateOrEditCustomParameterDto input)
    {
        var customParameter = ObjectMapper.Map<CustomParameter>(input);

        if (AbpSession.TenantId != null)
        {
            customParameter.TenantId = (int)AbpSession.TenantId;
        }

        var id = await _customParameterRepository.InsertAndGetIdAsync(customParameter);

        var response = ObjectMapper.Map<CreateOrEditCustomParameterDto>(customParameter);
        response.Id = id;

        return response;
    }

    [AbpAuthorize(AppPermissions.Pages_CustomParameters_Edit)]
    protected virtual async Task<CreateOrEditCustomParameterDto> Update(CreateOrEditCustomParameterDto input)
    {
        var customParameter = await _customParameterRepository.FirstOrDefaultAsync((int)input.Id);
        ObjectMapper.Map(input, customParameter);

        var response = ObjectMapper.Map<CreateOrEditCustomParameterDto>(customParameter);
        return response;
    }

    [AbpAuthorize(AppPermissions.Pages_CustomParameters_Delete)]
    public virtual async Task Delete(EntityDto input)
    {
        await _customParameterRepository.DeleteAsync(input.Id);
    }

    public virtual async Task<FileDto> GetCustomParametersToExcel(GetAllCustomParametersForExcelInput input)
    {

        var filteredCustomParameters = _customParameterRepository.GetAll()
                    .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), e => false || e.Name.Contains(input.Filter) || e.AggregationFunction.Contains(input.Filter) || e.Type.Contains(input.Filter) || e.InnerCustomParameters.Contains(input.Filter) || e.CustomBaseDataList.Contains(input.Filter))
                    .WhereIf(!string.IsNullOrWhiteSpace(input.NameFilter), e => e.Name.Contains(input.NameFilter))
                    .WhereIf(!string.IsNullOrWhiteSpace(input.AggregationFunctionFilter), e => e.AggregationFunction.Contains(input.AggregationFunctionFilter))
                    .WhereIf(!string.IsNullOrWhiteSpace(input.TypeFilter), e => e.Type.Contains(input.TypeFilter))
                    .WhereIf(input.MinResolutionInSecondsFilter != null, e => e.ResolutionInSeconds >= input.MinResolutionInSecondsFilter)
                    .WhereIf(input.MaxResolutionInSecondsFilter != null, e => e.ResolutionInSeconds <= input.MaxResolutionInSecondsFilter)
                    .WhereIf(!string.IsNullOrWhiteSpace(input.CustomBaseDataListFilter), e => e.CustomBaseDataList.Contains(input.CustomBaseDataListFilter));

        var query = from o in filteredCustomParameters
                    select new GetCustomParameterForViewDto()
                    {
                        CustomParameter = new CustomParameterDto
                        {
                            Name = o.Name,
                            AggregationFunction = o.AggregationFunction,
                            Type = o.Type,
                            Id = o.Id,
                            ResolutionInSeconds = o.ResolutionInSeconds,
                        }
                    };

        var customParameterListDtos = await query.ToListAsync();

        return _customParametersExcelExporter.ExportToFile(customParameterListDtos);
    }

}