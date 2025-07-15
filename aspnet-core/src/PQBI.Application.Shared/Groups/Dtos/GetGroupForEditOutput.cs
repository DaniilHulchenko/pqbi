using System;
using Abp.Application.Services.Dto;
using System.ComponentModel.DataAnnotations;

namespace PQBI.Groups.Dtos;

public class GetGroupForEditOutput
{
    public CreateOrEditGroupDto Group { get; set; }

}