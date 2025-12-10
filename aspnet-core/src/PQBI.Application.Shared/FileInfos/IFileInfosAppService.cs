using System;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using PQBI.FileInfos.Dtos;
using PQBI.Dto;

using System.Collections.Generic;

namespace PQBI.FileInfos;

public interface IFileInfosAppService : IApplicationService
{
    Task<PagedResultDto<GetFileInfoForViewDto>> GetAll(GetAllFileInfosInput input);

    Task<GetFileInfoForViewDto> GetFileInfoForView(EntityDto<int> input);

    Task<GetFileInfoForEditOutput> GetFileInfoForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditFileInfoDto input);

    Task Delete(EntityDto input);

}