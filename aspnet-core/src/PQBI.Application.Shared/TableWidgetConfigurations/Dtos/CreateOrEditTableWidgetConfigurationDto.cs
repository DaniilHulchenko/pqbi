using System;
using Abp.Application.Services.Dto;
using System.ComponentModel.DataAnnotations;

namespace PQBI.TableWidgetConfigurations.Dtos
{
    public class CreateOrEditTableWidgetConfigurationDto : EntityDto<int?>
    {

        [Required]
        public string Configuration { get; set; }
        
        [Required]
        public string Components { get; set; }

        public string DateRange { get; set; }

        public string? DesignOptions { get; set; }

    }
}