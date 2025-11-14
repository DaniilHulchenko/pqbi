namespace PQBI.CalculationEngine.Functions.Aggregation;

public class CountCalcFunction : SingleCalculationFunction
{
    public const string Count_Function = "count";

    public override string Alias => Count_Function;

    public override BasicValue Calc(SingleAxisInput input, double parameter)
    {
        double count = 0;
        var dataValueStatus = PqbiDataValueStatus.Ok;
        DateTime? firstStartTime = null;

        foreach (var item in input.Data)
        {
            if (item.Value is not null)
            {
                count++;
            }
            if (firstStartTime is null)                 // this runs only once
                firstStartTime = item.StartTime;
            dataValueStatus = dataValueStatus.Intersect(item.DataValueStatus);
        }
        DateTime startTime = firstStartTime ?? DateTime.MinValue;

        return new BasicValue(count, startTime, dataValueStatus);
    }
}