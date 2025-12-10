using Abp.Application.Services.Dto;
using System;

namespace PQBI.FileInfos.Dtos;

public class GetAllFileInfosInput : PagedAndSortedResultRequestDto
{
    public string Filter { get; set; }

}