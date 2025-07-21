using PQBI.CalculationEngine.Functions;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PQBI.CalculationEngine.Matrix;

public class MatrixBase 
{
    public const int Single_Resolution = -1;

    protected readonly AggregationMatrix AggregationMatrix = new AggregationMatrix();
    protected readonly OperatorMatrix OperatorMatrix = new OperatorMatrix();

    public BasicValue[] AggregationCalculation { get; protected set; }


    //protected string CleanFunctionId(string functionId)
    //{
    //    functionId = Regex.Replace(functionId, @"\(\s*\)", "");
    //    return functionId.ToLower();
    //}

    public static (string Name, double? Parameter) CleanFunctionId(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input cannot be null or whitespace.", nameof(input));

        input = input.Trim();
        int open = input.IndexOf('(');
        int close = input.LastIndexOf(')');

        if (open > 0 && close == input.Length - 1 && close > open)
        {
            string name = input.Substring(0, open).Trim().ToLower();
            string inside = input.Substring(open + 1, close - open - 1).Trim();

            if (inside.Length == 0)
            {
                return (name, null);
            }

            if (double.TryParse(inside, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
            {
                return (name, d);
            }

            throw new FormatException($"Unable to parse parameter '{inside}' as a double.");
        }

        return (input, null);
    }


public BasicValue CalcAggregationFunction(string funcId, SingleAxisInput input)
    {
        var result = default(BasicValue);

        if (funcId.Contains('('))
        {
            if (TryExtracSingleParameter(funcId, out var singleParameter))
            {

            }
        }
        else
        {


        }

        return result;
    }

    public bool TryExtracSingleParameter(string funWithparameter, out double parameter)
    {
        var result = true;
        parameter = 0;

        var pattern = @"\((\d+(\.\d+)?)\)";
        var match = Regex.Match(funWithparameter, pattern);
        var numberStr = match.Groups[1].Value;

        if (string.IsNullOrEmpty(numberStr))
        {
            return false;
        }

        parameter = double.Parse(numberStr);
        return true;

    }

    public List<BasicValue[]> DevideByGroups(IEnumerable<BasicValue> points, int resolutionInSeconds , int syncInSeconds)
    {
        if (resolutionInSeconds == Single_Resolution)
        {
            return [points.ToArray()];
        }

  
        var result = new List<BasicValue[]>();
        int gVec = resolutionInSeconds / syncInSeconds;

        var data = points as BasicValue[] ?? points.ToArray();
        int dataCount = data.Length;

        var buffer = new BasicValue[gVec];
        int bufferIndex = 0;

        for (int index = 0; index < dataCount; index++)
        {
            buffer[bufferIndex++] = data[index];

            if (bufferIndex == gVec)
            {
                result.Add(buffer);
                buffer = new BasicValue[gVec];
                bufferIndex = 0;
            }
        }

        if (bufferIndex > 0)
        {
            result.Add(buffer.Take(bufferIndex).ToArray());
        }

        return result;
    }

}

