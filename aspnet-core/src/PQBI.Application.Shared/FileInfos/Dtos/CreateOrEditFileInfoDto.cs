using System;
using Abp.Application.Services.Dto;
using System.ComponentModel.DataAnnotations;

namespace PQBI.FileInfos.Dtos;

public class CreateOrEditFileInfoDto : EntityDto<int?>
{

    [Required]
    public string Name { get; set; }

    [Required]
    public string Content { get; set; }

}