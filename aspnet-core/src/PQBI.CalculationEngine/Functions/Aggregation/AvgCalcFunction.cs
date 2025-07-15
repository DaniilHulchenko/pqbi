
namespace PQBI.CalculationEngine.Functions.Aggregation;


public class AvgCalcFunction : SingleCalculationFunction
{
    public const string Avg_Function = "avg";

    public override string Alias => Avg_Function;

    public override BasicValue Calc(SingleAxisInput input, double parameter)
    {
        double? sum = 0;
        var count = 0;
        var data = input.Data;
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
}
