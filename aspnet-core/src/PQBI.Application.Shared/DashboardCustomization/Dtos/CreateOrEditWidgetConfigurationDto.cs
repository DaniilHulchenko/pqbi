using System;
using Abp.Application.Services.Dto;
using System.ComponentModel.DataAnnotations;

namespace PQBI.DashboardCustomization.Dtos
{
    public class CreateOrEditWidgetConfigurationDto : EntityDto<Guid?>
    {

        [Required]
        public string WidgetGuid { get; set; }

        public string Name { get; set; }

        public string Configuration { get; set; }

        public DateTime LastModifiedOn { get; set; }

    }
}