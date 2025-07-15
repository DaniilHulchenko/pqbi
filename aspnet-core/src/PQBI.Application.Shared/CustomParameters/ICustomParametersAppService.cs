using System;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using PQBI.CustomParameters.Dtos;
using PQBI.Dto;

using System.Collections.Generic;

namespace PQBI.CustomParameters;

public interface ICustomParametersAppService : IApplicationService
{
    public const string CustomParameter = "CustomParameters";
    Task<PagedResultDto<GetCustomParameterForViewDto>> GetAll(GetAllCustomParametersInput input);

    Task<GetCustomParameterForViewDto> GetCustomParameterForView(int id);

    Task<GetCustomParameterForEditOutput> GetCustomParameterForEdit(EntityDto input);

    Task<CreateOrEditCustomParameterDto> CreateOrEdit(CreateOrEditCustomParameterDto input);

    Task Delete(EntityDto input);

    Task<FileDto> GetCustomParametersToExcel(GetAllCustomParametersForExcelInput input);

}