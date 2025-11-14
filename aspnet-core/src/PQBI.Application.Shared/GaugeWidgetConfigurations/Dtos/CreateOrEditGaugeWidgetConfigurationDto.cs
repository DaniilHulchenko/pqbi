using System;
using Abp.Application.Services.Dto;
using System.ComponentModel.DataAnnotations;

namespace PQBI.GaugeWidgetConfigurations.Dtos;

public class CreateOrEditGaugeWidgetConfigurationDto : EntityDto<int?>
{

    [Required]
    public string DateRange { get; set; }

    [Required]
    public string Parameter { get; set; }

    [Required]
    public string Style { get; set; }

    public int RefreshRate { get; set; }

}