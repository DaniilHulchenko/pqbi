using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using Abp.Linq.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using PQBI.Groups.Exporting;
using PQBI.Groups.Dtos;
using PQBI.Dto;
using Abp.Application.Services.Dto;
using PQBI.Authorization;
using Abp.Extensions;
using Abp.Authorization;
using Microsoft.EntityFrameworkCore;
using Abp.UI;
using PQBI.Storage;
using System.Globalization;

namespace PQBI.Groups;

[AbpAuthorize(AppPermissions.Pages_Groups)]
public class GroupsAppService : PQBIAppServiceBase, IGroupsAppService
{
    private readonly IRepository<Group, Guid> _groupRepository;
    private readonly IGroupsExcelExporter _groupsExcelExporter;

    public GroupsAppService(IRepository<Group, Guid> groupRepository, IGroupsExcelExporter groupsExcelExporter)
    {
        _groupRepository = groupRepository;
        _groupsExcelExporter = groupsExcelExporter;

    }

    public virtual async Task<PagedResultDto<GetGroupForViewDto>> GetAll(GetAllGroupsInput input)
    {

        var filteredGroups = _groupRepository.GetAll()
                    .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), e => false || e.Name.Contains(input.Filter) || e.Subgroups.Contains(input.Filter))
                        .WhereIf(!string.IsNullOrWhiteSpace(input.NameFilter), e => e.Name.Contains(input.NameFilter))
                        .WhereIf(!string.IsNullOrWhiteSpace(input.SubgroupsFilter), e => e.Subgroups.Contains(input.SubgroupsFilter));

        var pagedAndFilteredGroups = filteredGroups
            .OrderBy(input.Sorting ?? "id asc")
            .PageBy(input);

        var groups = from o in pagedAndFilteredGroups
                     select new
                     {

                         o.Name,
                         o.Subgroups,
                         Id = o.Id
                     };

        var totalCount = await filteredGroups.CountAsync();

        var dbList = await groups.ToListAsync();
        var results = new List<GetGroupForViewDto>();

        foreach (var o in dbList)
        {
            var res = new GetGroupForViewDto()
            {
                Group = new GroupDto
                {

                    Name = o.Name,
                    Subgroups = o.Subgroups,
                    Id = o.Id,
                }
            };

            results.Add(res);
        }

        return new PagedResultDto<GetGroupForViewDto>(
            totalCount,
            results
        );

    }

    public virtual async Task<GetGroupForViewDto> GetGroupForView(EntityDto<Guid> input)
    {
        var group = await _groupRepository.GetAsync(input.Id);

        var output = new GetGroupForViewDto { Group = ObjectMapper.Map<GroupDto>(group) };

        return output;
    }

    [AbpAuthorize(AppPermissions.Pages_Groups_Edit)]
    public virtual async Task<GetGroupForEditOutput> GetGroupForEdit(EntityDto<Guid> input)
    {
        var group = await _groupRepository.FirstOrDefaultAsync(input.Id);

        var output = new GetGroupForEditOutput { Group = ObjectMapper.Map<CreateOrEditGroupDto>(group) };

        return output;
    }

    public virtual async Task CreateOrEdit(CreateOrEditGroupDto input)
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

    [AbpAuthorize(AppPermissions.Pages_Groups_Create)]
    protected virtual async Task Create(CreateOrEditGroupDto input)
    {
        var group = ObjectMapper.Map<Group>(input);

        await _groupRepository.InsertAsync(group);

    }

    [AbpAuthorize(AppPermissions.Pages_Groups_Edit)]
    protected virtual async Task Update(CreateOrEditGroupDto input)
    {
        var group = await _groupRepository.FirstOrDefaultAsync((Guid)input.Id);
        ObjectMapper.Map(input, group);

    }

    [AbpAuthorize(AppPermissions.Pages_Groups_Delete)]
    public virtual async Task Delete(EntityDto<Guid> input)
    {
        await _groupRepository.DeleteAsync(input.Id);
    }

    public virtual async Task<FileDto> GetGroupsToExcel(GetAllGroupsForExcelInput input)
    {

        var filteredGroups = _groupRepository.GetAll()
                    .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), e => false || e.Name.Contains(input.Filter) || e.Subgroups.Contains(input.Filter))
                    .WhereIf(!string.IsNullOrWhiteSpace(input.NameFilter), e => e.Name.Contains(input.NameFilter))
                    .WhereIf(!string.IsNullOrWhiteSpace(input.SubgroupsFilter), e => e.Subgroups.Contains(input.SubgroupsFilter));

        var query = from o in filteredGroups
                    select new GetGroupForViewDto()
                    {
                        Group = new GroupDto
                        {
                            Name = o.Name,
                            Subgroups = o.Subgroups,
                            Id = o.Id
                        }
                    };

        var groupListDtos = await query.ToListAsync();

        return _groupsExcelExporter.ExportToFile(groupListDtos);
    }

}