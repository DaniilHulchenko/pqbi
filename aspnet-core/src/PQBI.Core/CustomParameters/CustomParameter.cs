using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;
using Abp.Domain.Entities;

namespace PQBI.CustomParameters;

[Table("CustomParameters")]
public class CustomParameter : Entity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [Required]
    public virtual string Name { get; set; }

    [Required]
    public virtual string AggregationFunction { get; set; }

    [Required]
    public virtual string Type { get; set; }

    public virtual string InnerCustomParameters { get; set; }

    public virtual int ResolutionInSeconds { get; set; }

    public virtual string CustomBaseDataList { get; set; }

}