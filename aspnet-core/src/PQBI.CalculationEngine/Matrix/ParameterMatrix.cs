using PQBI.CalculationEngine.Functions;
using PQBI.CalculationEngine.Functions.Aggregation;
using PQBI.Infrastructure.Extensions;
using PQS.Data.Common;
using System.Collections.Generic;

namespace PQBI.CalculationEngine.Matrix;

public interface IMatrixParameterKey
{
    public Guid ComponentID { get; }
    public string BaseParameterName { get; }

    public string ScadaParameterName { get; }
    public string ParameterId { get; }

}

public record DataUnitType(int Id, string TokenCode);
public record EmptyDataUnitType() : DataUnitType(-1, "");

public class ParameterMatrix : MatrixBase
{
    public record BaseParameterInfo(IEnumerable<BasicValue> BaseParameters, PQZStatus Status = PQZStatus.OK);

    private readonly Dictionary<IMatrixParameterKey, BaseParameterInfo> _validParameters = new Dictionary<IMatrixParameterKey, BaseParameterInfo>();
    private readonly Dictionary<IMatrixParameterKey, BaseParameterInfo> _invalidParameters = new Dictionary<IMatrixParameterKey, BaseParameterInfo>();

    public Dictionary<IMatrixParameterKey, BaseParameterInfo> ValidParameters => _validParameters;
    public Dictionary<IMatrixParameterKey, BaseParameterInfo> InvalidParameters => _invalidParameters;



    public DataUnitType DataUnitType { get; set; }

    public void AddSeries(IMatrixParameterKey parameternId, IEnumerable<BasicValue> nums, PQZStatus status)
    {
        if (status == PQZStatus.OK)
        {
            _validParameters[parameternId] = new BaseParameterInfo(nums, status);
        }
        else
        {
            _invalidParameters[parameternId] = new BaseParameterInfo(nums, status);
        }
    }

    public bool IfMatrixEmpty()
    {
        return _validParameters.Count > 0;
    }

    public IEnumerable<BasicValue> CalculateOperator(IEnumerable<BasicValue> points, string operatorFunction)
    {
        if (operatorFunction.IsStringEmpty())
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

    public void CalculateAndSetOutterAggregation2222(string aggregationFunctionId)
    {
        var (functionName, parameter) = CleanFunctionId(aggregationFunctionId);


        if (_validParameters.Count == 0)
        {
            AggregationCalculation = Array.Empty<BasicValue>();
            return;
        }

        var matrix = ConvertListDimentionalArray(_validParameters.Count, _validParameters.First().Value.BaseParameters.Count());
        int rowLength = matrix.GetLength(0);
        int columnLength = matrix.GetLength(1);

        var result = new BasicValue[columnLength];
        var buffer = new BasicValue[rowLength];

        for (int column = 0; column < columnLength; column++)
        {
            for (int row = 0; row < rowLength; row++)
            {
                buffer[row] = matrix[row, column];
            }

            result[column] = AggregationMatrix.Run(buffer, -1, functionName);
            //result[column] = callback(buffer);
        }

        AggregationCalculation = result;
    }

    public IEnumerable<BasicValue> CalculateAndSetOutterAggregation(Func<BasicValue[], BasicValue> callback)
    {
        if (_validParameters.Count == 0)
        {
            AggregationCalculation = Array.Empty<BasicValue>();
            return AggregationCalculation;
        }

        var matrix = ConvertListDimentionalArray(_validParameters.Count, _validParameters.First().Value.BaseParameters.Count());
        int rowLength = matrix.GetLength(0);
        int columnLength = matrix.GetLength(1);

        var result = new BasicValue[columnLength];
        var buffer = new BasicValue[rowLength];

        for (int column = 0; column < columnLength; column++)
        {
            for (int row = 0; row < rowLength; row++)
            {
                buffer[row] = matrix[row, column];
            }

            result[column] = callback(buffer);
        }

        AggregationCalculation = result;
        return result;
    }

    public IEnumerable<BasicValue> CalculateAggregation(IEnumerable<BasicValue> points, string quantityAggregationFunction, int resolutionResolutionInSeconds, int sycResolutionInSeconds)
    {
        if (sycResolutionInSeconds == resolutionResolutionInSeconds)
        {
            return points;
        }

        var groupByResponse = DevideByGroups(points, resolutionResolutionInSeconds, sycResolutionInSeconds);
        var calculated = new List<BasicValue>();

        foreach (var list in groupByResponse)
        {
            var tmp = AggregationMatrix.Run(list, -1, quantityAggregationFunction);
            calculated.Add(tmp);
        }

        return calculated;
    }
    private BasicValue[,] ConvertListDimentionalArray(int rows, int columns)
    {
        var result = new BasicValue[rows, columns];

        int row = 0;
        foreach (var keyAndValue in _validParameters)
        {
            int column = 0;
            foreach (var item in keyAndValue.Value.BaseParameters)
            {
                result[row, column++] = item;
            }

            row++;
        }

        return result;
    }
}

