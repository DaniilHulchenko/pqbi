using PQBI.PQS.CalcEngine;

using System;
using Abp.Application.Services.Dto;

namespace PQBI.CardWidgetConfigurations.Dtos;

public class CardWidgetConfigurationDto : EntityDto
{
    public string DateRange { get; set; }

    public string Parameters { get; set; }

    public CardWidgetStyleType StyleType { get; set; }

    public int RefreshRate { get; set; }

}