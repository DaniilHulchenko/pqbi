using PQBI.PQS.CalcEngine;

using System;
using Abp.Application.Services.Dto;
using System.ComponentModel.DataAnnotations;

namespace PQBI.CardWidgetConfigurations.Dtos;

public class CreateOrEditCardWidgetConfigurationDto : EntityDto<int?>
{

    [Required]
    public string DateRange { get; set; }

    [Required]
    public string Parameters { get; set; }

    public CardWidgetStyleType StyleType { get; set; }

    public int RefreshRate { get; set; }

}