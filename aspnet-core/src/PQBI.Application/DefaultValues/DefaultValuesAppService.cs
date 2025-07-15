using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using Abp.Linq.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using PQBI.DefaultValues.Dtos;
using PQBI.Dto;
using Abp.Application.Services.Dto;
using PQBI.Authorization;
using Abp.Extensions;
using Abp.Authorization;
using Microsoft.EntityFrameworkCore;
using Abp.UI;
using PQBI.Storage;
using PQBI.Exporting;

namespace PQBI.DefaultValues
{
    [AbpAuthorize(AppPermissions.Pages_DefaultValues)]
    public class DefaultValuesAppService : PQBIAppServiceBase, IDefaultValuesAppService
    {
        private readonly IRepository<DefaultValue> _defaultValueRepository;

        public DefaultValuesAppService(IRepository<DefaultValue> defaultValueRepository)
        {
            _defaultValueRepository = defaultValueRepository;

        }

        public virtual async Task<PagedResultDto<GetDefaultValueForViewDto>> GetAll(GetAllDefaultValuesInput input)
        {

            var filteredDefaultValues = _defaultValueRepository.GetAll()
                        .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), e => false || e.Name.Contains(input.Filter) || e.Value.Contains(input.Filter));

            var pagedAndFilteredDefaultValues = filteredDefaultValues
                .OrderBy(input.Sorting ?? "id asc")
                .PageBy(input);

            var defaultValues = from o in pagedAndFilteredDefaultValues
                                select new
                                {

                                    o.Name,
                                    o.Value,
                                    Id = o.Id
                                };

            var totalCount = await filteredDefaultValues.CountAsync();

            var dbList = await defaultValues.ToListAsync();
            var results = new List<GetDefaultValueForViewDto>();

            foreach (var o in dbList)
            {
                var res = new GetDefaultValueForViewDto()
                {
                    DefaultValue = new DefaultValueDto
                    {

                        Name = o.Name,
                        Value = o.Value,
                        Id = o.Id,
                    }
                };

                results.Add(res);
            }

            return new PagedResultDto<GetDefaultValueForViewDto>(
                totalCount,
                results
            );

        }

        public virtual async Task<GetDefaultValueForViewDto> GetDefaultValueForView(int id)
        {
            var defaultValue = await _defaultValueRepository.GetAsync(id);

            var output = new GetDefaultValueForViewDto { DefaultValue = ObjectMapper.Map<DefaultValueDto>(defaultValue) };

            return output;
        }

        public async Task<GetDefaultValueForEditOutput> GetDefaultValueByName(string input)
        {
            var defaultValue = await _defaultValueRepository.FirstOrDefaultAsync(value => value.Name == input);

            var output = new GetDefaultValueForEditOutput { DefaultValue = ObjectMapper.Map<CreateOrEditDefaultValueDto>(defaultValue) };

            return output;
        }

        [AbpAuthorize(AppPermissions.Pages_DefaultValues_Edit)]
        public virtual async Task<GetDefaultValueForEditOutput> GetDefaultValueForEdit(EntityDto input)
        {
            var defaultValue = await _defaultValueRepository.FirstOrDefaultAsync(input.Id);

            var output = new GetDefaultValueForEditOutput { DefaultValue = ObjectMapper.Map<CreateOrEditDefaultValueDto>(defaultValue) };

            return output;
        }

        public virtual async Task CreateOrEdit(CreateOrEditDefaultValueDto input)
        {
            var defaultValue = await _defaultValueRepository.FirstOrDefaultAsync(value => value.Name == input.Name);

            if (defaultValue == null)
            {
                await Create(input);
            }
            else
            {
                ObjectMapper.Map(input, defaultValue);
            }
        }

        [AbpAuthorize(AppPermissions.Pages_DefaultValues_Create)]
        protected virtual async Task Create(CreateOrEditDefaultValueDto input)
        {
            var defaultValue = ObjectMapper.Map<DefaultValue>(input);

            if (AbpSession.TenantId != null)
            {
                defaultValue.TenantId = (int?)AbpSession.TenantId;
            }

            await _defaultValueRepository.InsertAsync(defaultValue);

        }

        [AbpAuthorize(AppPermissions.Pages_DefaultValues_Edit)]
        protected virtual async Task Update(CreateOrEditDefaultValueDto input)
        {
            var defaultValue = await _defaultValueRepository.FirstOrDefaultAsync((int)input.Id);
            ObjectMapper.Map(input, defaultValue);

        }

        [AbpAuthorize(AppPermissions.Pages_DefaultValues_Delete)]
        public virtual async Task Delete(EntityDto input)
        {
            await _defaultValueRepository.DeleteAsync(input.Id);
        }
    }
}