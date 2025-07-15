using System;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using PQBI.DefaultValues.Dtos;
using PQBI.Dto;

using System.Collections.Generic;

namespace PQBI.DefaultValues
{
    public interface IDefaultValuesAppService : IApplicationService
    {
        Task<PagedResultDto<GetDefaultValueForViewDto>> GetAll(GetAllDefaultValuesInput input);

        Task<GetDefaultValueForViewDto> GetDefaultValueForView(int id);

        Task<GetDefaultValueForEditOutput> GetDefaultValueByName(string input);

        Task<GetDefaultValueForEditOutput> GetDefaultValueForEdit(EntityDto input);

        Task CreateOrEdit(CreateOrEditDefaultValueDto input);

        Task Delete(EntityDto input);

    }
}