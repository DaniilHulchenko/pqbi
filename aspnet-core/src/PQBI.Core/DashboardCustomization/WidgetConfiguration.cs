using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;
using Abp.Domain.Entities;

namespace PQBI.DashboardCustomization
{
    [Table("WidgetConfigurations")]
    public class WidgetConfiguration : Entity<Guid>
    {

        [Required]
        public virtual string WidgetGuid { get; set; }

        public virtual string Configuration { get; set; }

        [Required]
        public virtual string Name { get; set; }

        public virtual DateTime CreatedOn { get; set; }

        public virtual DateTime LastModifiedOn { get; set; }

    }
}