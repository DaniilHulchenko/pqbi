using System;
using Abp.Application.Services.Dto;
using System.ComponentModel.DataAnnotations;

namespace PQBI.CustomParameters.Dtos;

public partial class CreateOrEditCustomParameterDto : EntityDto<int?>
{

    [Required]
    public string Name { get; set; }

    [Required]
    public string AggregationFunction { get; set; }

    [Required]
    public string Type { get; set; }

    public string InnerCustomParameters { get; set; }

    public int ResolutionInSeconds { get; set; }

    public string CustomBaseDataList { get; set; }

}