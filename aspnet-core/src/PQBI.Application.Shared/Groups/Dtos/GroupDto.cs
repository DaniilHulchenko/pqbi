using System;
using Abp.Application.Services.Dto;

namespace PQBI.Groups.Dtos;

public class GroupDto : EntityDto<Guid>
{
    public string Name { get; set; }

    public string Subgroups { get; set; }

}