using Abp.Application.Services.Dto;
using System;

namespace PQBI.Groups.Dtos;

public class GetAllGroupsInput : PagedAndSortedResultRequestDto
{
    public string Filter { get; set; }

    public string NameFilter { get; set; }

    public string SubgroupsFilter { get; set; }

}