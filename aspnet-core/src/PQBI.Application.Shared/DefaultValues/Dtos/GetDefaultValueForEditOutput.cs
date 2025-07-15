using System;
using Abp.Application.Services.Dto;
using System.ComponentModel.DataAnnotations;

namespace PQBI.DefaultValues.Dtos
{
    public class GetDefaultValueForEditOutput
    {
        public CreateOrEditDefaultValueDto DefaultValue { get; set; }

    }
}