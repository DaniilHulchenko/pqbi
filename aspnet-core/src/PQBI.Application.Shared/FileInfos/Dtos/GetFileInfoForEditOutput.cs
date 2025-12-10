using System;
using Abp.Application.Services.Dto;
using System.ComponentModel.DataAnnotations;

namespace PQBI.FileInfos.Dtos;

public class GetFileInfoForEditOutput
{
    public CreateOrEditFileInfoDto FileInfo { get; set; }

}