using System;
using Abp.Application.Services.Dto;

namespace PQBI.DefaultValues.Dtos
{
    public class DefaultValueDto : EntityDto
    {
        public string Name { get; set; }

        public string Value { get; set; }

    }
}