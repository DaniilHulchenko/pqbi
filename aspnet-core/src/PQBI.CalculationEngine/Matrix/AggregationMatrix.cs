using PQBI.CalculationEngine.Functions;

namespace PQBI.CalculationEngine.Matrix;


public class AggregationMatrix
{
    protected Dictionary<string, Func<IEnumerable<BasicValue>, double, BasicValue>> AggregationMapper = new Dictionary<string, Func<IEnumerable<BasicValue>, double, BasicValue>>();

    public AggregationMatrix()
    {
        Initialize();
    }

    public IEnumerable<string> Keys => AggregationMapper.Keys;


    private void Initialize()
    {
        AggregationMapper.Add("avg", AvgCalc);
        AggregationMapper.Add("count", CountCalc);
        AggregationMapper.Add("max", MaxCalc);
        AggregationMapper.Add("min", MinCalc);
        AggregationMapper.Add("rms", RmsCalc);
        AggregationMapper.Add("sum", SumCalc);
        AggregationMapper.Add("percentile", PercentileCalc);
    }

    public BasicValue Run(IEnumerable<BasicValue> points, double parameter, string funcId)
    {
        funcId = funcId.ToLower();
        var callback = AggregationMapper[funcId];
        var result = callback(points,parameter);
        return result;
    }


    private BasicValue AvgCalc(IEnumerable<BasicValue> data, double parameter)
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

    private BasicValue CountCalc(IEnumerable<BasicValue> data, double parameter)
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

    private BasicValue MaxCalc(IEnumerable<BasicValue> data, double parameter)
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

    private BasicValue MinCalc(IEnumerable<BasicValue> data, double parameter)
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

    private BasicValue RmsCalc(IEnumerable<BasicValue> data, double parameter)
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

    private BasicValue SumCalc(IEnumerable<BasicValue> data, double parameter)
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

    private BasicValue PercentileCalc(IEnumerable<BasicValue> data, double parameter)
    {
        var sortedSequence = data.Where(x => x.Value is not null).OrderBy(x => x).ToArray();

        double rank = parameter / 100.0 * (sortedSequence.Length - 1) + 1;
        int integerRank = (int)rank;
        double fractionalRank = rank - integerRank;

        if (integerRank >= sortedSequence.Length)
        {
            //Todo refactored
            return new BasicValue(sortedSequence.Last().Value, PqbiDataValueStatus.Ok);
            //return sortedSequence.Last();
        }

        if (integerRank == 0)
        {
            //Todo refactored
            return new BasicValue(sortedSequence.First().Value, PqbiDataValueStatus.Ok);
            //return sortedSequence.First();
        }

        BasicValue lowerValue = sortedSequence[integerRank - 1];
        BasicValue upperValue = sortedSequence[integerRank];

        var result = lowerValue.Value + (upperValue.Value - lowerValue.Value) * fractionalRank;
        return new BasicValue(result, PqbiDataValueStatus.Ok);
        //return result;
    }

}

