using System;
using Abp.Application.Services.Dto;
using System.ComponentModel.DataAnnotations;

namespace PQBI.Groups.Dtos;

public class CreateOrEditGroupDto : EntityDto<Guid?>
{

    [Required]
    [StringLength(GroupConsts.MaxNameLength, MinimumLength = GroupConsts.MinNameLength)]
    public string Name { get; set; }

    [Required]
    public string Subgroups { get; set; }

}