using System;
using Abp.Application.Services.Dto;

namespace PQBI.FileInfos.Dtos;

public class FileInfoDto : EntityDto
{
    public string Name { get; set; }

    public string Content { get; set; }

}