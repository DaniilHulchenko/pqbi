using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;
using Abp.Domain.Entities;

namespace PQBI.Groups;

[Table("Groups")]
public class Group : Entity<Guid>
{

    [Required]
    [StringLength(GroupConsts.MaxNameLength, MinimumLength = GroupConsts.MinNameLength)]
    public virtual string Name { get; set; }

    [Required]
    public virtual string Subgroups { get; set; }

}