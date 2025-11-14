using System.Collections.Generic;
using System;
using Abp.Runtime.Validation;
using PQBI.Infrastructure.Sapphire;
using PQS.Data.Events.Enums;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Linq;
using PQBI.CalculationEngine.Functions;
using System.Reflection.Metadata.Ecma335;

namespace PQBI.PQS.CalcEngine;

public class HarmonicsDto
{
    //public List<int> HarmonicNums { get; set; } = new List<int>();
    //public string Range { get; set; }
    //public string RangeOn { get; set; }
    public int? Value { get; set; }

    //public int? Index { get; set; }

}


//public partial class FromComponent
//{
//    public Guid ComponentId { get; set; }
//    public int? FeederId { get; set; } // in Case feeder  it is an ID
//}


public record InnerCustomParameter(int CustomParameterId, string Quantity, int Resolution, string Operator, string InnerAggregationFunction);


public class FeederComponentInfo
{
    public int? Id { get; set; }      // FeederId
    public string? Name { get; set; } //FeederName
    public Guid ComponentId { get; set; } //ComponentID
    public string CompName { get; set; }

    public override int GetHashCode()
    {
        if (Id is null)
        {
            return ComponentId.GetHashCode();
        }

        return ComponentId.GetHashCode() ^ Id.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        var result = false;
        if (obj is FeederComponentInfo feeder)
        {
            if (feeder.Id is not null)
            {
                result = ComponentId == feeder.ComponentId && Id == feeder.Id;
            }
            else
            {
                result = ComponentId == feeder.ComponentId;
            }
        }

        return result;
    }

    public override string ToString()
    {
        var key = ComponentId.ToString();

        if (Id is not null)
        {
            key = $"{key}__{Id}";
        }

        return key;
    }
}

public record PQSCalculationResponse(TrendResponse Calculated);


