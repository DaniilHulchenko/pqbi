using System;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using PQBI.Groups.Dtos;
using PQBI.Dto;

using System.Collections.Generic;

namespace PQBI.Groups;

public interface IGroupsAppService : IApplicationService
{
    Task<PagedResultDto<GetGroupForViewDto>> GetAll(GetAllGroupsInput input);

    Task<GetGroupForViewDto> GetGroupForView(EntityDto<Guid> input);

    Task<GetGroupForEditOutput> GetGroupForEdit(EntityDto<Guid> input);

    Task CreateOrEdit(CreateOrEditGroupDto input);

    Task Delete(EntityDto<Guid> input);

    Task<FileDto> GetGroupsToExcel(GetAllGroupsForExcelInput input);

}