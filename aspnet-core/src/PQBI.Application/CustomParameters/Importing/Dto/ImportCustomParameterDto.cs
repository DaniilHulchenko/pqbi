using System;
using Abp.AutoMapper;
using PQBI.DataImporting.Excel;
using PQBI.CustomParameters.Dtos;

namespace PQBI.CustomParameters.Importing.Dto;

[AutoMapTo(typeof(CustomParameter))]
public class ImportCustomParameterDto : ImportFromExcelDto
{
    public string Name { get; set; }
    public string AggregationFunction { get; set; }
    public string Type { get; set; }
    public string InnerCustomParameters { get; set; }
    public int ResolutionInSeconds { get; set; }
    public string CustomBaseDataList { get; set; }

}