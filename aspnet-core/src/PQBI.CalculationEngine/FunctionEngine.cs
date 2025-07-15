using System.Text.RegularExpressions;
using PQBI.CalculationEngine.Functions;
using PQBI.CalculationEngine.Functions.Aggregation;
using PQBI.CalculationEngine.Functions.CalcSingleAxis;
using PQBI.CalculationEngine.Functions.SingleAxisResponse;
using PQBI.Infrastructure;
using Microsoft.Extensions.Options;

namespace PQBI.CalculationEngine;

public class FunctionEngineConfig : PQSConfig<FunctionEngineConfig>
{
    public bool IsDuringDevisionThrowException { get; set; }
    public double DefaultValueDuringDivision { get; set; } = 0;
}

public interface IFunctionEngine
{
    static IEnumerable<SingleCalculationFunction> GetAllAggregationFunctions() => [new AvgCalcFunction(), new MinCalcFunction(), new MaxCalcFunction(), new CountCalcFunction(), new SumCalcFunction(), new PercentileCalcFunction(), new RmsCalcFunction()];
    static IEnumerable<ArrayCalculationFunction> GetAllMathematicalFunction() => [new MultiplyCalcFunction(), new DivideCalcFunction(), new AbsoluteCalcFunction()];

    BasicValue AggregationCalculationAsync(string type, IEnumerable<BasicValue> input);
    //IEnumerable<double?> AggregationCalculation(MatrixCalculation_Original matrix, string type);
    IEnumerable<BasicValue> AggregationCalculation(IEnumerable<IEnumerable<BasicValue>> matrix, string type);


    IEnumerable<IEnumerable<BasicValue>> CalcGroupByAsync(GroupByFunctionInput input);

    IEnumerable<BasicValue> SingleParameterCalculationAxis(string type, string funWithparameter, IEnumerable<BasicValue> input);
}

public class FunctionEngine : IFunctionEngine
{
    private readonly Dictionary<string, CalcFunctionBase> _functionTypes;
    private readonly Dictionary<string, SingleCalculationFunction> _aggrigationFunctionTypes;
    private readonly Dictionary<string, ArrayCalculationFunction> _calcFunctionWithParameter;
    private readonly FunctionEngineConfig _config;

    public FunctionEngine(IOptions<FunctionEngineConfig> config)
    {
        _functionTypes = new Dictionary<string, CalcFunctionBase>();
        _aggrigationFunctionTypes = new Dictionary<string, SingleCalculationFunction>();
        _calcFunctionWithParameter = new Dictionary<string, ArrayCalculationFunction>();

        _config = config.Value;

        Initialize();
    }


    private void Initialize()
    {
        _functionTypes.Add(GroupByCalcFunction.GroupBy_Function, new GroupByCalcFunction());

        //InitCalcSingleAxisOutputAxisfunctionFunctions([new AbsoluteCalcFunction()]);
        InitAggregationFunctions(IFunctionEngine.GetAllAggregationFunctions());
        InitCalcFunctionWithParameter(IFunctionEngine.GetAllMathematicalFunction());
    }

    private void InitAggregationFunctions(IEnumerable<SingleCalculationFunction> funcs)
    {
        foreach (var func in funcs)
        {
            _aggrigationFunctionTypes.Add(func.Alias, func);
        }
    }

    private void InitCalcFunctionWithParameter(IEnumerable<ArrayCalculationFunction> funcs)
    {
        foreach (var func in funcs)
        {
            _calcFunctionWithParameter.Add(func.Alias, func);
        }
    }

    public IEnumerable<BasicValue> SingleParameterCalculationAxis(string type, string funWithparameter, IEnumerable<BasicValue> input)
    {
        var flag = false;

        type = type.ToLower();

        IEnumerable<BasicValue> result = null;

        var pattern = @"\((\d+(\.\d+)?)\)";
        var match = Regex.Match(funWithparameter, pattern);
        var numberStr = match.Groups[1].Value;


        double parameter = 0;
        double.TryParse(numberStr, out parameter);

        foreach (var func in _calcFunctionWithParameter)
        {
            if (type.StartsWith(func.Key))
            {
                flag = true;
                var funcInput = new SingleAxisInput
                {
                    Data = input.ToArray(),
                };
                result = func.Value.Calc(funcInput, parameter);
                break;
            }

        }

        if(flag == false)
        {
            throw new Exception($"Such Alias {type} is not supported.");
        }

        return result;
    }


    public BasicValue AggregationCalculationAsync(string type, IEnumerable<BasicValue> input)
    {
        type = type.ToLower();

        foreach (var aggregationFunction in _aggrigationFunctionTypes.Values)
        {
            if (type.StartsWith(aggregationFunction.Alias))
            {
                BasicValue result  = new BasicValue();

                if (aggregationFunction is PercentileCalcFunction percentileCalcFunction)
                {
                    //With Parameter
                    percentileCalcFunction.TryExtracParameter(type, out var @param);
                    result = percentileCalcFunction.Calc(new SingleAxisInput { Data = input }, @param);

                }
                else
                {
                    result = aggregationFunction.Calc(new SingleAxisInput { Data = input }, 0);
                }

                //Paramerless!!!
                return result;
            }
        }

        throw new Exception($"{nameof(AggregationCalculation)} - type doesnt exists.");
    }

    public async Task<IEnumerable<double>> ArithmeticCalculation(Dictionary<string, double[]> matrix, string type)
    {
        IEnumerable<double> result = null;

        var args = ArithmeticCalcFunction.getArguments(type);
        var arithmetics = new ArithmeticCalcFunction();
        result = await arithmetics.Calculation(args, matrix, _config.IsDuringDevisionThrowException, _config.DefaultValueDuringDivision);

        return result;
    }

    //public IEnumerable<double?> AggregationCalculation(MatrixCalculation_Original matrix, string type)
    //{
    //    var data = matrix.ConvertListDimentionalArray();

    //    type = type.ToLower();

    //    IEnumerable<double?> result =  IntrinsicAggregationCalculationAsync(data, type);
    //    return result;
    //}

    public IEnumerable<BasicValue> AggregationCalculation(IEnumerable<IEnumerable<BasicValue>> matrix, string type)
    {

        var data = ConvertTo2DArray(matrix);
        type = type.ToLower();

        IEnumerable<BasicValue> result =  IntrinsicAggregationCalculationAsync(data, type);


        return result;
    }

    public static BasicValue[,] ConvertTo2DArray(IEnumerable<IEnumerable<BasicValue>> enumerableMatrix)
    {
        int rows = enumerableMatrix.Count();
        int columns = enumerableMatrix.First().Count();

        BasicValue[,] result = new BasicValue[columns, rows];

        for (int i = 0; i < rows; i++)
        {
            try
            {
                for (int col = 0; col < columns; col++)
                {
                    result[col, i] = (enumerableMatrix.ElementAt(i)).ElementAt(col);
                }
            }
            catch (Exception ex1)
            {
            }
        }

        return result;
    }




    public IEnumerable<IEnumerable<BasicValue>> CalcGroupByAsync(GroupByFunctionInput input)
    {
        var result = default(IEnumerable<IEnumerable<BasicValue>>);
        var function = _functionTypes[GroupByCalcFunction.GroupBy_Function];

        if (function is GroupByCalcFunction groupByCalcFunction)
        {
            result = groupByCalcFunction.Calc(input);
        }

        return result;
    }

    private IEnumerable<BasicValue> IntrinsicAggregationCalculationAsync(BasicValue[,] data, string type)
    {
        var rows = data.GetLength(0);
        var columns = data.GetLength(1);
        var calculated = new List<BasicValue>();

        var avgs = new List<BasicValue>();
        for (int i = 0; i < columns; i++)
        {
            var buffer = new List<BasicValue>();
            for (int j = 0; j < rows; j++)
            {
                buffer.Add(data[j, i]);
            }

            var tmp =  AggregationCalculationAsync(type, buffer.ToArray());
            calculated.Add(tmp);
        }

        return calculated;
    }

}
