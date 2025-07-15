using System;
using Abp.Application.Services.Dto;
using System.ComponentModel.DataAnnotations;

namespace PQBI.CustomParameters.Dtos;

public class GetCustomParameterForEditOutput
{
    public CreateOrEditCustomParameterDto CustomParameter { get; set; }

}