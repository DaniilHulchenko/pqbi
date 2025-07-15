using PQBI.CalculationEngine.Functions;

namespace PQBI.CalculationEngine.Matrix;

//static IEnumerable<SingleCalculationFunction> GetAllAggregationFunctions() => [new AvgCalcFunction(), new MinCalcFunction(), new MaxCalcFunction(), new CountCalcFunction(), new SumCalcFunction(), new PercentileCalcFunction(), new RmsCalcFunction()];


public class OperatorMatrix
{
    protected Dictionary<string, Func<BasicValue, double, BasicValue>> OperatorMapper = new Dictionary<string, Func<BasicValue, double, BasicValue>>();

    public OperatorMatrix()
    {
        Initialize();
    }

    public IEnumerable<string> Keys => OperatorMapper.Keys;

    private void Initialize()
    {
        OperatorMapper.Add("divide", DivideCalc);
        OperatorMapper.Add("mult", MultCalc);
        OperatorMapper.Add("absolute", MultCalc);
    }

    public IEnumerable<BasicValue> Run(IEnumerable<BasicValue> points, double parameter, string funcId)
    {
        funcId = funcId.ToLower();
        var result = new List<BasicValue>();
        var callback = OperatorMapper[funcId];

        foreach (var point in points)
        {
            result.Add(callback(point, parameter));
        }

        return result;
    }

    private BasicValue DivideCalc(BasicValue basicValue, double parameter)
    {
        if (basicValue.Value is not null)
        {
            return new BasicValue((double)(basicValue.Value / parameter), basicValue.DataValueStatus);
        }

        return new BasicValue(basicValue.Value, basicValue.DataValueStatus);
    }
    private BasicValue MultCalc(BasicValue basicValue, double parameter)
    {
        if (basicValue.Value is not null)
        {
            return new BasicValue(basicValue.Value * parameter, basicValue.DataValueStatus);
        }
        return new BasicValue(basicValue.Value, basicValue.DataValueStatus);
    }

    public BasicValue AbsoluteCalc(BasicValue basicValue, double parameter)
    {
        if (basicValue.Value is not null)
        {
            return new BasicValue(Math.Abs(basicValue.Value.Value), basicValue.DataValueStatus);
        }

        return basicValue;
    }
}

