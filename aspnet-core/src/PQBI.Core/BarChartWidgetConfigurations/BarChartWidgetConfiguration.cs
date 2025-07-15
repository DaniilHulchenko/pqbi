using PQBI.BarChartWidgetConfigurations;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;
using Abp.Domain.Entities;

namespace PQBI.BarChartWidgetConfigurations
{
    [Table("BarChartWidgetConfigurations")]
    public class BarChartWidgetConfiguration : Entity, IMayHaveTenant
    {
        public int? TenantId { get; set; }

        public virtual BarChartType Type { get; set; }

        [Required]
        public virtual string Components { get; set; }

        [Required]
        public virtual string Events { get; set; }

        public virtual string DateRange { get; set; }

    }
}