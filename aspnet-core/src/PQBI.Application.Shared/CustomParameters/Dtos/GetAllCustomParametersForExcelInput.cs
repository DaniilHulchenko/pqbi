using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;

namespace PQBI.CustomParameters.Dtos;

public class GetAllCustomParametersForExcelInput
{
    public string Filter { get; set; }

    public string NameFilter { get; set; }

    public string AggregationFunctionFilter { get; set; }

    public string TypeFilter { get; set; }

    public int? MaxResolutionInSecondsFilter { get; set; }
    public int? MinResolutionInSecondsFilter { get; set; }

    public string CustomBaseDataListFilter { get; set; }

}