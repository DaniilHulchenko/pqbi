using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;
using Abp.Domain.Entities;

namespace PQBI.DefaultValues
{
    [Table("DefaultValues")]
    public class DefaultValue : Entity, IMayHaveTenant
    {
        public int? TenantId { get; set; }

        [Required]
        public virtual string Name { get; set; }

        [Required]
        public virtual string Value { get; set; }

        public virtual DateTime CreatedAt { get; set; }

        public virtual DateTime LastUpdatedAt { get; set; }

    }
}