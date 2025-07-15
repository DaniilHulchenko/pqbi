using PQBI.CalculationEngine.Functions;
using PQBI.Infrastructure.Extensions;
using PQS.Data.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PQBI.CalculationEngine.Matrix;

public interface IMatrixParameterKey
{
    public string BaseParameterName { get; }
    public string ParameterId { get; }
}


public interface IMatrixBaseParameterKey : IMatrixParameterKey
{
    public Guid ComponentID { get; }
    public string ScadaParameterName { get; }

}

public record DataUnitType(int Id, string TokenCode);
public record EmptyDataUnitType() : DataUnitType(-1, "");
public readonly record struct BasicValueWorkItem(BasicValue basicValue, string parameterName);

public class ParameterMatrix : MatrixBase
{

    public record BaseParameterInfo(IEnumerable<BasicValue> BaseParameters, PQZStatus Status = PQZStatus.OK);

    private readonly Dictionary<string, BaseParameterInfo> _validParameters = new Dictionary<string, BaseParameterInfo>();
    private readonly Dictionary<string, BaseParameterInfo> _invalidParameters = new Dictionary<string, BaseParameterInfo>();

    public Dictionary<string, BaseParameterInfo> ValidParameters => _validParameters;
    public Dictionary<string, BaseParameterInfo> InvalidParameters => _invalidParameters;

    private static readonly Regex _rx = new(
        @"^\s*(?<op>\w+)(?:\s*\(\s*(?<arg>[0-9]*\.?[0-9]+)\s*\))?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);


    public void AddSeries(string parameterName, IEnumerable<BasicValue> nums, PQZStatus status)
    {
        if (status == PQZStatus.OK)
        {
            _validParameters[parameterName] = new BaseParameterInfo(nums, status);
        }
        else
        {
            _invalidParameters[parameterName] = new BaseParameterInfo(nums, status);
        }
    }

    public IEnumerable<BasicValue> CalculateOperator(IEnumerable<BasicValue> points, string operatorFunction)
    {
        if (operatorFunction.IsStringEmpty() || points.IsCollectionEmpty())
        {
            return points;
        }

        var (functionName, parameter) = CleanFunctionId(operatorFunction);
        if (parameter is not null)
        {
            return OperatorMatrix.Run(points, parameter.Value, functionName);
        }

        return OperatorMatrix.Run(points, -1, functionName);
    }

    public void CalculateAndSetOutterAggregation(string aggregationFunctionId)
    {
        if (_validParameters.Count == 0)
        {
            AggregationCalculation = Array.Empty<BasicValue>();
            return;
        }

        var (functionName, parameter) = CleanFunctionId(aggregationFunctionId);
        double extraPrm = parameter ?? -1;

        BasicValueWorkItem[,] matrix = ConvertListDimentionalArray();
        int columnLength = matrix.GetLength(1);
        var result = new BasicValue[columnLength];

        if (AggregationMatrix.TryGetArithmetics(aggregationFunctionId, out string expression))
        {
            var ptr = AggregationMatrixObj.RunArithmetic(matrix, expression);
            var index = 0;

            foreach (var item in ptr)
            {
                result[index++] = item;
            }
        }
        else
        {
            result = CalculateAggregationFromMatrix(matrix, extraPrm, functionName);
        }

        AggregationCalculation = result;
    }

    public void CalculateAndSetOutterAggregationOnSingleCollection(string aggregationFunctionId)
    {
        if (_validParameters.Count == 0)
        {
            AggregationCalculation = Array.Empty<BasicValue>();
            return;
        }

        var (functionName, parameter) = CleanFunctionId(aggregationFunctionId);
        double extraPrm = parameter ?? -1;

        List<BasicValue> valList = new List<BasicValue>();
        foreach (var keyAndValue in _validParameters)
        {
            valList.AddRange(keyAndValue.Value.BaseParameters);
        }

        BasicValue basVal = AggregationMatrix.Run(valList, extraPrm, functionName);
        AggregationCalculation = [basVal];
    }


    public void RunOperatonOnAggregationCalculation(string operatorFunction)
    {
        if (operatorFunction.IsStringEmpty())
        {
            return;
        }

        IEnumerable<BasicValue> aggregationCalculation = CalculateOperator(AggregationCalculation, operatorFunction);
        AggregationCalculation = aggregationCalculation.ToArray();
    }

    public IEnumerable<BasicValue> CalculateAggregation(IEnumerable<BasicValue> points, string quantityAggregationFunction, int resolutionResolutionInSeconds, int sycResolutionInSeconds, bool isExcludeFlagged, double nominalVal = -1, bool isIgnoreAligningFunction = false)
    {
        if (sycResolutionInSeconds == resolutionResolutionInSeconds || points.IsCollectionEmpty())
        {
            return points;
        }

        var groupByResponse = DevideByGroups(points, resolutionResolutionInSeconds, sycResolutionInSeconds);
        var calculated = new List<BasicValue>();

        double additionalParameter = -1;
        if (IsPercentile(quantityAggregationFunction))
        {
            TryParse(quantityAggregationFunction, out quantityAggregationFunction, out additionalParameter);
        }

        List<BasicValue[]> pointsGroupList = FilterByFlaggedEvents(isExcludeFlagged, groupByResponse);
        if (!isIgnoreAligningFunction)
        {
            foreach (var list in pointsGroupList)
            {
                var tmp = AggregationMatrix.Run(list, additionalParameter, quantityAggregationFunction);
                calculated.Add(tmp);
            }
        }
        else
        {
            calculated =
                pointsGroupList.SelectMany(arr => arr)        // flatten
                    .ToList();                     // List<BasicValue>                       // List<BasicValue[]>

            //.Where(arr => arr != null)     // optional: ignore null entries
        }
        calculated = Normalize(nominalVal, calculated);

        return calculated;
    }

    //public List<BasicValue[]> FilterByFlaggedEvents(bool isExcludeFlagged, double nominalVal, List<BasicValue[]> groupByResponse)
    //{
    //    List<BasicValue[]> pointsGroupList = new List<BasicValue[]>();
    //    if (nominalVal == -1 && !isExcludeFlagged)
    //    {
    //        pointsGroupList = groupByResponse;
    //    }
    //    else if (nominalVal != -1 && isExcludeFlagged)
    //    {
    //        foreach (var pointsGroup in groupByResponse)
    //        {
    //            List<BasicValue> newPointsGroup = new List<BasicValue>();
    //            for (int i = 0; i < pointsGroup.Count(); i++)
    //            {
    //                double? pointVal = (pointsGroup[i].Value / nominalVal) * 100;
    //                BasicValue newBasicValue = new BasicValue(pointVal, pointsGroup[i].DataValueStatus);

    //                if (pointsGroup[i].DataValueStatus != PqbiDataValueStatus.Flag)
    //                {
    //                    newPointsGroup.Add(newBasicValue);
    //                }
    //            }
    //            pointsGroupList.Add(newPointsGroup.ToArray());
    //        }
    //    }
    //    else if (nominalVal != -1)
    //    {
    //        foreach (var pointsGroup in groupByResponse)
    //        {
    //            List<BasicValue> newPointsGroup = new List<BasicValue>();
    //            for (int i = 0; i < pointsGroup.Count(); i++)
    //            {
    //                double? pointVal = (pointsGroup[i].Value / nominalVal) * 100;
    //                BasicValue newBasicValue = new BasicValue(pointVal, pointsGroup[i].DataValueStatus);
    //                newPointsGroup.Add(newBasicValue);
    //            }
    //            pointsGroupList.Add(newPointsGroup.ToArray());
    //        }
    //    }
    //    else if (isExcludeFlagged)
    //    {
    //        foreach (var pointsGroup in groupByResponse)
    //        {
    //            List<BasicValue> newPointsGroup = new List<BasicValue>();
    //            for (int i = 0; i < pointsGroup.Count(); i++)
    //            {
    //                if (pointsGroup[i].DataValueStatus != PqbiDataValueStatus.Flag)
    //                {
    //                    newPointsGroup.Add(pointsGroup[i]);
    //                }
    //            }
    //            pointsGroupList.Add(newPointsGroup.ToArray());
    //        }
    //    }

    //    return pointsGroupList;
    //}

    public static List<BasicValue[]> FilterByFlaggedEvents(bool isExcludeFlagged, List<BasicValue[]> groupByResponse)
    {
        List<BasicValue[]> pointsGroupList = new List<BasicValue[]>();
        if (!isExcludeFlagged)
        {
            pointsGroupList = groupByResponse;
        }
        else
        {
            foreach (var pointsGroup in groupByResponse)
            {
                List<BasicValue> newPointsGroup = new List<BasicValue>();
                for (int i = 0; i < pointsGroup.Count(); i++)
                {
                    if (pointsGroup[i].DataValueStatus != PqbiDataValueStatus.Flag)
                    {
                        newPointsGroup.Add(pointsGroup[i]);
                    }
                }
                pointsGroupList.Add(newPointsGroup.ToArray());
            }
        }

        return pointsGroupList;
    }

    public List<BasicValue> Normalize(double nominalVal, List<BasicValue> baseValList)
    {
        List<BasicValue>? pointsList = null;
        if (nominalVal == -1)
        {
            pointsList = baseValList;
        }
        else if (nominalVal != -1)
        {
            pointsList = new List<BasicValue>();
            for (int i = 0; i < baseValList.Count; i++)
            {
                double? pointVal = Normalize(nominalVal, baseValList[i].Value);
                BasicValue newBasicValue = new BasicValue(pointVal, baseValList[i].DataValueStatus);
                pointsList.Add(newBasicValue);
            }
        }

        return pointsList;
    }

    public static double? Normalize(double nominalVal, double? baseVal)
    {
        return (baseVal / nominalVal) * 100;
    }

    //(IMatrixParameterKey, 
    //private BasicValueWorkItem[,] ConvertListDimentionalArray(List<BasicValue[]> aggregationCalculationList, List<IMatrixBaseParameterKey> matrixPrmKeyList)
    private BasicValueWorkItem[,] ConvertListDimentionalArray()
    {
        var result = new BasicValueWorkItem[_validParameters.Count, _validParameters.First().Value.BaseParameters.Count()];

        //var result = new BasicValueWorkItem[_validParameters.Count + aggregationCalculationList.Count, _validParameters.First().Value.BaseParameters.Count()];
        int row = 0;

        foreach (var keyAndValue in _validParameters)
        {
            int column = 0;
            foreach (var item in keyAndValue.Value.BaseParameters)
            {
                result[row, column++] = new BasicValueWorkItem(item, keyAndValue.Key);
            }

            row++;
        }

        //for (int childNum = 0; childNum < childrenAggregationCalculationList.Count; childNum++)
        //{
        //    int column = 0;            
        //    BasicValue[] valList = childrenAggregationCalculationList[childNum];
        //    foreach (var item in valList)
        //    {
        //        result[row, column++] = new BasicValueWorkItem(item, matrixPrmKeyList[childNum]);
        //    }
        //    row++;
        //}

        return result;
    }

    public static bool TryParse(string input, out string op, out double arg)
    {
        op = string.Empty;
        arg = -1;

        var m = _rx.Match(input);
        if (!m.Success) return false;

        op = m.Groups["op"].Value.ToLowerInvariant();

        if (m.Groups["arg"].Success)
        {
            // Parse using invariant culture so the dot is always the decimal separator
            arg = double.Parse(
                m.Groups["arg"].Value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture);
        }

        return true;
    }

    bool IsPercentile(string op) =>
        op.StartsWith("percentile(", StringComparison.OrdinalIgnoreCase);
}


