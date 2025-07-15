using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;
using Abp.Domain.Entities;

namespace PQBI.TableWidgetConfigurations
{
    [Table("TableWidgetConfigurations")]
    public class TableWidgetConfiguration : Entity, IMustHaveTenant
    {
        public int TenantId { get; set; }

        [Required]
        public virtual string Configuration { get; set; }

        [Required]
        public virtual string Components { get; set; }

        public virtual string DateRange { get; set; }

        public virtual string? DesignOptions { get; set; }

    }
}