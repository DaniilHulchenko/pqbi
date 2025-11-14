using PQBI.PQS.CalcEngine;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;
using Abp.Domain.Entities;

namespace PQBI.CardWidgetConfigurations;

[Table("CardWidgetConfigurations")]
public class CardWidgetConfiguration : Entity, IMayHaveTenant
{
    public int? TenantId { get; set; }

    [Required]
    public virtual string DateRange { get; set; }

    [Required]
    public virtual string Parameters { get; set; }

    public virtual CardWidgetStyleType StyleType { get; set; }

    public virtual int RefreshRate { get; set; }

}