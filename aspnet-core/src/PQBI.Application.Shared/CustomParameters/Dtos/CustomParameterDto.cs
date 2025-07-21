using System;
using Abp.Application.Services.Dto;

namespace PQBI.CustomParameters.Dtos;

public class CustomParameterDto : EntityDto
{
    public string Name { get; set; }

    public string AggregationFunction { get; set; }

    public string STDPQSParametersList { get; set; }

    public string Type { get; set; }

    public int ResolutionInSeconds { get; set; }
}