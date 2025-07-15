using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;
using Abp.Domain.Entities;

namespace PQBI.TrendWidgetConfigurations
{
    [Table("TrendWidgetConfigurations")]
    public class TrendWidgetConfiguration : Entity, IMayHaveTenant
    {
        public int? TenantId { get; set; }

        [Required]
        public virtual string DateRange { get; set; }

        [Required]
        public virtual string Resolution { get; set; }

        public virtual string Parameters { get; set; }

    }
}