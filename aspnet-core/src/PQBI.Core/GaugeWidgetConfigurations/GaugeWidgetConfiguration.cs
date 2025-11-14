using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;
using Abp.Domain.Entities;

namespace PQBI.GaugeWidgetConfigurations;

[Table("GaugeWidgetConfigurations")]
public class GaugeWidgetConfiguration : Entity, IMayHaveTenant
{
    public int? TenantId { get; set; }

    [Required]
    public virtual string DateRange { get; set; }

    [Required]
    public virtual string Parameter { get; set; }

    [Required]
    public virtual string Style { get; set; }

    public virtual int RefreshRate { get; set; }

}