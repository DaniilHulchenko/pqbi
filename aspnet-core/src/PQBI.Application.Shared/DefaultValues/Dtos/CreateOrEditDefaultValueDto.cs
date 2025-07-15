using System;
using Abp.Application.Services.Dto;
using System.ComponentModel.DataAnnotations;

namespace PQBI.DefaultValues.Dtos
{
    public class CreateOrEditDefaultValueDto : EntityDto<int?>
    {

        [Required]
        public string Name { get; set; }

        [Required]
        public string Value { get; set; }

    }
}