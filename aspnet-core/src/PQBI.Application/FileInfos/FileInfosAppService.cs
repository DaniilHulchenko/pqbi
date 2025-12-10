using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using Abp.Linq.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using PQBI.FileInfos.Dtos;
using PQBI.Dto;
using Abp.Application.Services.Dto;
using PQBI.Authorization;
using Abp.Extensions;
using Abp.Authorization;
using Microsoft.EntityFrameworkCore;
using Abp.UI;
using PQBI.Storage;
using System.Globalization;

namespace PQBI.FileInfos;

[AbpAuthorize(AppPermissions.Pages_FileInfos)]
public class FileInfosAppService : PQBIAppServiceBase, IFileInfosAppService
{
    private readonly IRepository<FileInfo> _fileInfoRepository;

    public FileInfosAppService(IRepository<FileInfo> fileInfoRepository)
    {
        _fileInfoRepository = fileInfoRepository;

    }

    public virtual async Task<PagedResultDto<GetFileInfoForViewDto>> GetAll(GetAllFileInfosInput input)
    {

        var filteredFileInfos = _fileInfoRepository.GetAll()
                    .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), e => false || e.Name.Contains(input.Filter) || e.Content.Contains(input.Filter));

        var pagedAndFilteredFileInfos = filteredFileInfos
            .OrderBy(input.Sorting ?? "id asc")
            .PageBy(input);

        var fileInfos = from o in pagedAndFilteredFileInfos
                        select new
                        {

                            o.Name,
                            o.Content,
                            Id = o.Id
                        };

        var totalCount = await filteredFileInfos.CountAsync();

        var dbList = await fileInfos.ToListAsync();
        var results = new List<GetFileInfoForViewDto>();

        foreach (var o in dbList)
        {
            var res = new GetFileInfoForViewDto()
            {
                FileInfo = new FileInfoDto
                {

                    Name = o.Name,
                    Content = o.Content,
                    Id = o.Id,
                }
            };

            results.Add(res);
        }

        return new PagedResultDto<GetFileInfoForViewDto>(
            totalCount,
            results
        );

    }

    public virtual async Task<GetFileInfoForViewDto> GetFileInfoForView(EntityDto<int> input)
    {
        var fileInfo = await _fileInfoRepository.GetAsync(input.Id);

        var output = new GetFileInfoForViewDto { FileInfo = ObjectMapper.Map<FileInfoDto>(fileInfo) };

        return output;
    }

    [AbpAuthorize(AppPermissions.Pages_FileInfos_Edit)]
    public virtual async Task<GetFileInfoForEditOutput> GetFileInfoForEdit(EntityDto input)
    {
        var fileInfo = await _fileInfoRepository.FirstOrDefaultAsync(input.Id);

        var output = new GetFileInfoForEditOutput { FileInfo = ObjectMapper.Map<CreateOrEditFileInfoDto>(fileInfo) };

        return output;
    }

    public virtual async Task CreateOrEdit(CreateOrEditFileInfoDto input)
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

    [AbpAuthorize(AppPermissions.Pages_FileInfos_Create)]
    protected virtual async Task Create(CreateOrEditFileInfoDto input)
    {
        var fileInfo = ObjectMapper.Map<FileInfo>(input);

        if (AbpSession.TenantId != null)
        {
            fileInfo.TenantId = (int?)AbpSession.TenantId;
        }

        await _fileInfoRepository.InsertAsync(fileInfo);

    }

    [AbpAuthorize(AppPermissions.Pages_FileInfos_Edit)]
    protected virtual async Task Update(CreateOrEditFileInfoDto input)
    {
        var fileInfo = await _fileInfoRepository.FirstOrDefaultAsync((int)input.Id);
        ObjectMapper.Map(input, fileInfo);

    }

    [AbpAuthorize(AppPermissions.Pages_FileInfos_Delete)]
    public virtual async Task Delete(EntityDto input)
    {
        await _fileInfoRepository.DeleteAsync(input.Id);
    }

}