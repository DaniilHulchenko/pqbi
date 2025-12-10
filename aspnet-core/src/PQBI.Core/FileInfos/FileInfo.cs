using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;
using Abp.Domain.Entities;

namespace PQBI.FileInfos;

[Table("FileInfos")]
public class FileInfo : Entity, IMayHaveTenant
{
    public int? TenantId { get; set; }

    [Required]
    public virtual string Name { get; set; }

    [Required]
    public virtual string Content { get; set; }

}