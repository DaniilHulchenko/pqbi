using PQBI.CalculationEngine.Functions;
using PQS.Data.Common.Values;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace PQBI.CalculationEngine.Matrix;


public class AggregationMatrix
{
    public const string Arithmetic_Name = "Arithmetics";

    protected static Dictionary<string, Func<IEnumerable<BasicValue>, double, BasicValue>> AggregationMapper = new Dictionary<string, Func<IEnumerable<BasicValue>, double, BasicValue>>();

    static AggregationMatrix()
    {
        AggregationMapper.Add("avg", AvgCalc);
        AggregationMapper.Add("count", CountCalc);
        AggregationMapper.Add("max", MaxCalc);
        AggregationMapper.Add("min", MinCalc);
        AggregationMapper.Add("rms", RmsCalc);
        AggregationMapper.Add("sum", SumCalc);
        AggregationMapper.Add("percentile", PercentileCalc);
    }

    public IEnumerable<string> Keys => AggregationMapper.Keys;

    public enum TokenArithmeticType
    {
        Variable,
        Operator,
        Number
    }

    public record ArithmeticToken(string Value, TokenArithmeticType Type);

    public List<ArithmeticToken> TokenizeArithmeticExpression(string expression)
    {
        var tokens = new List<ArithmeticToken>();
        StringBuilder sb = new StringBuilder();
        bool insideBraces = false;

        for (int i = 0; i < expression.Length; i++)
        {
            char ch = expression[i];

            if (ch == '{')
            {
                if (sb.Length > 0)
                {
                    var value = sb.ToString().Trim();
                    if (double.TryParse(value, out _))
                        tokens.Add(new ArithmeticToken(value, TokenArithmeticType.Number));
                    sb.Clear();
                }

                insideBraces = true;
                continue;
            }

            if (ch == '}')
            {
                insideBraces = false;
                var variable = sb.ToString().Trim();
                tokens.Add(new ArithmeticToken(variable, TokenArithmeticType.Variable));
                sb.Clear();
                continue;
            }

            if (insideBraces)
            {
                sb.Append(ch);
            }
            else if (char.IsWhiteSpace(ch))
            {
                continue;
            }
            else if ("+-*/()".Contains(ch))
            {
                if (sb.Length > 0)
                {
                    var value = sb.ToString().Trim();
                    if (double.TryParse(value, out _))
                        tokens.Add(new ArithmeticToken(value, TokenArithmeticType.Number));
                    sb.Clear();
                }
                tokens.Add(new ArithmeticToken(ch.ToString(), TokenArithmeticType.Operator));
            }
            else
            {
                sb.Append(ch);
            }
        }

        if (sb.Length > 0)
        {
            var value = sb.ToString().Trim();
            if (double.TryParse(value, out _))
                tokens.Add(new ArithmeticToken(value, TokenArithmeticType.Number));
        }

        return tokens;
    }

    public static bool TryGetArithmetics(string aggregationFunctionId, out string expression)
    {
        expression = string.Empty;

        string pattern = $@"(?i:{AggregationMatrix.Arithmetic_Name})\(((?>[^()]+|(?<open>\()|(?<-open>\)))+(?(open)(?!)))\)";
        Match match = Regex.Match(aggregationFunctionId, pattern, RegexOptions.IgnoreCase);
        var result = false;

        if (match.Success)
        {
            expression = match.Groups[1].Value;
            result = true;
        }

        return result;
    }

    private BasicValue[] ArithmeticCalculation(string arithmeticStatement, Dictionary<string, BasicValue[]> variables, bool isThrowException, double defaultValue)
    {
        var arrayLength = variables.Values.First().Length;
        DataTable dt = new DataTable();

        var result = new BasicValue[arrayLength];
        for (var parameterIndex = 0; parameterIndex < arrayLength; parameterIndex++)
        {
            StringBuilder expressionBuilder = new StringBuilder();
            IEnumerable<ArithmeticToken> tokens = TokenizeArithmeticExpression(arithmeticStatement);

            foreach (var token in tokens)
            {
                string val = string.Empty;
                switch (token.Type)
                {
                    case TokenArithmeticType.Variable:

                        BasicValue[] ptr = variables[token.Value];
                        BasicValue element = ptr[parameterIndex];

                        val = (element.Value ?? defaultValue).ToString();
                        break;

                    case TokenArithmeticType.Operator:
                        val = token.Value;
                        break;

                    case TokenArithmeticType.Number:
                        val = token.Value;
                        break;

                    default:
                        break;
                }

                expressionBuilder.Append(val);
            }

            var expression = expressionBuilder.ToString();
            var res = dt.Compute(expression, "");
            var numeric = double.Parse(res.ToString());

            if (numeric == double.PositiveInfinity || numeric == double.NegativeInfinity || numeric == double.NaN)
            {
                if (isThrowException)
                {
                    throw new Exception("Devision by zero");
                }
                else
                {
                    numeric = defaultValue;
                }
            }

            result[parameterIndex] = new BasicValue(numeric, PqbiDataValueStatus.Ok);// TODO

        }
        return result;
    }

    public IEnumerable<BasicValue> RunArithmetic(BasicValueWorkItem[,] matrix, string statement)
    {
        var @params = GetArguments(statement);
        int rowCount = matrix.GetLength(0);
        int columnCount = matrix.GetLength(1);

        var varibles = new Dictionary<string, BasicValue[]>();

        foreach (var @param in @params)
        {
            for (int row = 0; row < rowCount; row++)
            {
                BasicValueWorkItem item = matrix[row, 0];
                if (item.parameterName == param)
                {
                    BasicValue[] columnData = new BasicValue[columnCount];
                    for (int col = 0; col < columnCount; col++)
                    {
                        columnData[col] = matrix[row, col].basicValue;
                    }

                    if (varibles.TryGetValue(param, out _) == false)
                    {
                        varibles.Add(param, columnData);
                    }
                    break;

                }
            }
        }

        var ptr = ArithmeticCalculation(statement, varibles, false, 58);

        return ptr;
    }

    public IEnumerable<string> GetArguments(string expression)
    {
        var results = new List<string>();

        // Extract contents from curly braces
        var braceMatches = Regex.Matches(expression, @"\{([^}]+)\}");
        foreach (Match match in braceMatches)
        {
            results.Add(match.Groups[1].Value.Trim());
        }

        // Remove brace blocks so they don’t interfere with number detection
        var cleanedExpression = Regex.Replace(expression, @"\{[^}]+\}", " ");

        // Match plain numbers only (without + or - prefix)
        var numberMatches = Regex.Matches(cleanedExpression, @"\b\d+(\.\d+)?\b");
        foreach (Match match in numberMatches)
        {
            results.Add(match.Value);
        }

        return results;
    }

    public static BasicValue Run(IEnumerable<BasicValue> points, double parameter, string funcId)
    {
        funcId = funcId.ToLower();
        var callback = AggregationMapper[funcId];
        var result = callback(points, parameter);
        return result;
    }


    public BasicValue Run(IEnumerable<BasicValueWorkItem> points, double parameter, string funcId)
    {
        funcId = funcId.ToLower();

        return Run(points.Select(x => x.basicValue), parameter, funcId);
    }



    public static BasicValue AvgCalc(IEnumerable<BasicValue> data, double parameter)
    {
        double? sum = 0;
        var count = 0;
        var dataValueStatus = PqbiDataValueStatus.Ok;

        foreach (var item in data)
        {
            if (item.Value is not null)
            {
                sum += item.Value;
                count++;
            }

            dataValueStatus = dataValueStatus.Intersect(item.DataValueStatus);
        }

        if (count == 0)
        {
            return new BasicValue(null, dataValueStatus);
        }

        return new BasicValue(sum / count, dataValueStatus);
    }

    public static BasicValue CountCalc(IEnumerable<BasicValue> data, double parameter)
    {
        double count = 0;
        var dataValueStatus = PqbiDataValueStatus.Ok;

        foreach (var item in data)
        {
            if (item.Value is not null)
            {
                count++;
            }

            dataValueStatus = dataValueStatus.Intersect(item.DataValueStatus);

        }

        return new BasicValue(count, dataValueStatus);
    }

    public static BasicValue MaxCalc(IEnumerable<BasicValue> data, double parameter)
    {
        var flag = false;
        double max = double.MinValue;
        var dataValueStatus = PqbiDataValueStatus.Ok;


        foreach (var item in data)
        {
            if (item.Value is not null)
            {
                flag = true;
                max = Math.Max(max, item.Value.Value);
            }

            dataValueStatus = dataValueStatus.Intersect(item.DataValueStatus);
        }

        return new BasicValue(flag ? max : null, dataValueStatus);
    }

    public static BasicValue MinCalc(IEnumerable<BasicValue> data, double parameter)
    {
        var flag = false;
        double min = double.MaxValue;
        var dataValueStatus = PqbiDataValueStatus.Ok;

        foreach (var item in data)
        {
            if (item.Value is not null)
            {
                flag = true;
                min = Math.Min(min, item.Value.Value);
            }

            dataValueStatus = dataValueStatus.Intersect(item.DataValueStatus);

        }

        return new BasicValue(flag ? min : null, dataValueStatus);
    }

    public static BasicValue RmsCalc(IEnumerable<BasicValue> data, double parameter)
    {
        double sum = 0;
        var count = 0;
        var dataValueStatus = PqbiDataValueStatus.Ok;

        foreach (var item in data)
        {
            if (item.Value is not null)
            {
                sum += item.Value.Value * item.Value.Value;
                count++;
            }
            dataValueStatus = dataValueStatus.Intersect(item.DataValueStatus);
        }

        if (count == 0)
        {
            return new BasicValue(null, dataValueStatus);
        }

        sum = sum / count;
        var result = Math.Sqrt(sum);
        return new BasicValue(result, dataValueStatus); ;
    }

    public static BasicValue SumCalc(IEnumerable<BasicValue> data, double parameter)
    {
        double? sum = 0;
        var dataValueStatus = PqbiDataValueStatus.Ok;

        foreach (var item in data)
        {
            if (item.Value is not null)
            {
                sum += item.Value.Value;
            }

            dataValueStatus = dataValueStatus.Intersect(item.DataValueStatus);
        }

        return new BasicValue(data.Count() > 0 ? sum : null, dataValueStatus);
    }

    public static BasicValue PercentileCalc(IEnumerable<BasicValue> data, double parameter)
    {
        var combinedStatus = PqbiDataValueStatus.Ok;

        var lts = new List<BasicValue>();
        foreach (var item in data)
        {
            combinedStatus = combinedStatus.Intersect(item.DataValueStatus);
        }

        var sortedSequence = data.Where(x => x.Value is not null).OrderBy(x => x.Value).ToArray();

        var values = data
             .Where(bv => bv.Value.HasValue)
             .Select(bv => bv.Value!.Value)
             .OrderBy(v => v)
             .ToList();

        // No numeric data → return “no value” with the aggregated status
        if (values.Count == 0)
            return new BasicValue(null, combinedStatus);

        // Only one point → that point *is* every percentile
        if (values.Count == 1)
            return new BasicValue(values[0], combinedStatus);

        // Position on the sorted axis (0-based index)
        double pos = (values.Count - 1) * parameter;
        int lower = (int)Math.Floor(pos);
        int upper = (int)Math.Ceiling(pos);

        // Exact match (no interpolation needed)
        if (lower == upper)
            return new BasicValue(values[lower], combinedStatus);

        // Linear interpolation between the two nearest ranks
        double fraction = pos - lower;
        double interp = values[lower] + (values[upper] - values[lower]) * fraction;

        return new BasicValue(interp, combinedStatus);
    }


}