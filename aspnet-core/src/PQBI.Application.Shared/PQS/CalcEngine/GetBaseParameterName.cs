using PQS.Data.Networks;
using System;

namespace PQBI.PQS.CalcEngine;

public class BaseParameterNameSlim 
{
    public Guid ComponentId { get; set; } //ComponentId
    public string? FeederId { get; set; } // in Case feeder  it is an ID and if no feeder provided than channel

    public bool IsLogical => !string.IsNullOrEmpty(FeederId);

    public string AggregationFunction { get; set; }
    public string Operator { get; set; }

    public string Quantity { get; set; }
    public int? Resolution { get; set; } = null;
    public string Group { get; set; }
    public HarmonicsDto Harmonics { get; set; }

    public string Phase { get; set; }
    public string Base { get; set; }



}


public static class BaseParameterNameSlimExtensions
{
    public static BaseParameter ToBaseParameter(this BaseParameterNameSlim parameter)
    {
        //return new BaseParameter { AggregationFunction = parameter.AggregationFunction, Operator = parameter.Operator, Quantity = parameter.Quantity, Resolution = parameter.Resolution, fe }

        var result =  new BaseParameter
        {
            AggregationFunction = parameter.AggregationFunction,
            Operator = parameter.Operator,

            Quantity = parameter.Quantity,
            Group = parameter.Group,
            Harmonics = parameter.Harmonics,

            Phase = parameter.Phase,
            BaseResolution = parameter.Base
        };


        if(parameter.Resolution is not null)
        {
            result.Resolution = parameter.Resolution.Value;
        }

        return result;
    }
}

