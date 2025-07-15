using PQBI.CalculationEngine.Functions;
using PQBI.CalculationEngine.Matrix;
using PQBI.PQS.CalcEngine;
using PQS.Data.Common;
using PQS.Data.Common.Values;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PQBI.PQS;

public class AxisValue
{
    public long TimeStempInSeconds { get; init; }
    public DateTime DateTime => DateTimeOffset.FromUnixTimeSeconds(TimeStempInSeconds).DateTime;
    public double? Value { get; init; }
    public DataValueStatus DataValueStatus { get; set; }
}


public record MissingBaseParameterInfo(string PropertyName, PQZStatus Status, string Message);
public record GraphParametersComponentDtoV3(string CustomParameterName, IEnumerable<FeederComponentInfo> Feeders, string RequestType, DataUnitType DataUnitType, IEnumerable<string> ParameterNames, IEnumerable<AxisValue> Data, IEnumerable<MissingBaseParameterInfo> MissingInformation);


public static class GraphParametersComponentDtoV3Extensions
{
    public static bool TryGetMissingParameterInfo(this GraphParametersComponentDtoV3 graph, out MissingBaseParameterInfo missingBaseParameterInfo)
    {
        missingBaseParameterInfo = null;

        if (graph is not null)
        {
            missingBaseParameterInfo = graph.MissingInformation?.FirstOrDefault();
        }

        return missingBaseParameterInfo != null;
    }

    public static bool TryGetFirstAxisValue(this GraphParametersComponentDtoV3 graph, out AxisValue axisValue)
    {
        axisValue = FirstAxis(graph);

        return axisValue != null;
    }

    public static BasicValue ToBasicValue(this AxisValue axis)
    {
        return new BasicValue(axis.Value, axis.DataValueStatus.ToPqbiDataValueStatus());
    }


    public static AxisValue FirstAxis(this GraphParametersComponentDtoV3 graph)
    {

        if (graph is not null)
        {
            return graph.Data?.FirstOrDefault();
        }

        return null;
    }

    public static BasicValue FirstValue(this GraphParametersComponentDtoV3 graph)
    {
        var axis = FirstAxis(graph);
        if (axis is not null)
        {
            return axis.ToBasicValue();
        }

        return new BasicValue(null, PqbiDataValueStatus.Missing);
    }
}