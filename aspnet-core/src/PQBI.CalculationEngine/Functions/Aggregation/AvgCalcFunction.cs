
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

        DateTime? firstStartTime = null;
        foreach (var item in data)
        {
            if (item.Value is not null)
            {
                sum += item.Value;
                count++;
            }
            if (firstStartTime is null)                 // this runs only once
                firstStartTime = item.StartTime;
            dataValueStatus = dataValueStatus.Intersect(item.DataValueStatus);
        }
        DateTime startTime = firstStartTime ?? DateTime.MinValue;

        if (count == 0)
        {
            return new BasicValue(null, startTime, dataValueStatus);
        }

        return new BasicValue(sum / count, startTime, dataValueStatus);
    }
}
