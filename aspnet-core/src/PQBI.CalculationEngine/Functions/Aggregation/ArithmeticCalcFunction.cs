using System.Data;
using System.Text;

namespace PQBI.CalculationEngine.Functions.Aggregation;

public class ArithmeticCalcFunction : CalcFunctionBase
{
    public ArithmeticCalcFunction()
    {
    }


    public const string Arithmetic_Function = "arithmetic";

    public override string Alias => Arithmetic_Function;

    public static string getArguments(string expression)
    {
        var firstParenIndex = expression.IndexOf('(');
        var lastParenIndex = expression.LastIndexOf(')');

        var args = expression.Substring(firstParenIndex + 1, lastParenIndex - firstParenIndex - 1);
        return args;

    }


    public async Task<double[]> Calculation(string arithmeticStatement, Dictionary<string, double[]> variable, bool isThrowException, double defaultValue)
    {
        var arrayLength = variable.Values.First().Length;
        var result = new double[arrayLength];

        for (var parameterIndex = 0; parameterIndex < arrayLength; parameterIndex++)
        {
            var expressionBuilder = new StringBuilder();
            for (int index = 0; index < arithmeticStatement.Length;)
            {

                if (arithmeticStatement[index] == '{')
                {
                    var k = index;
                    do
                    {
                    }
                    while (arithmeticStatement[k++] != '}');
                    var parameterName = arithmeticStatement.Substring(index, k - index);
                    var ptr = variable[parameterName];
                    expressionBuilder.Append($"{ptr[parameterIndex]}");

                    index += k - index;

                    continue;
                }

                expressionBuilder.Append(arithmeticStatement[index++]);
            }

            var expression = expressionBuilder.ToString();
            DataTable dt = new DataTable();
            var res = dt.Compute(expression, "");
            var numeric = double.Parse(res.ToString());

            if (numeric == double.PositiveInfinity || numeric == double.NegativeInfinity  || numeric == double.NaN)
            {
                if (isThrowException )
                {
                    throw new Exception("Devision by zero");
                }
                else
                {
                    numeric = defaultValue;
                }
            }

            result[parameterIndex] = numeric;

        }
        return result;
    }
}

