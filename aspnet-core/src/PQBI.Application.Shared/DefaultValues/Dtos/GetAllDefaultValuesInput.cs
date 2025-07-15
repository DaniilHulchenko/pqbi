using Abp.Application.Services.Dto;
using System;

namespace PQBI.DefaultValues.Dtos
{
    public class GetAllDefaultValuesInput : PagedAndSortedResultRequestDto
    {
        public string Filter { get; set; }

    }
}