using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;

namespace PQBI.Groups.Dtos;

public class GetAllGroupsForExcelInput
{
    public string Filter { get; set; }

    public string NameFilter { get; set; }

    public string SubgroupsFilter { get; set; }

}