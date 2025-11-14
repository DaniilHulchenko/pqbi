namespace PQBI.CalculationEngine.Functions.Aggregation;

public class MaxCalcFunction : SingleCalculationFunction
{
    public const string Max_Function = "max";

    public override string Alias => Max_Function;

    public override BasicValue Calc(SingleAxisInput input, double parameter)
    {
        var flag = false;
        double max = double.MinValue;
        var dataValueStatus = PqbiDataValueStatus.Ok;
        DateTime? firstStartTime = null;

        foreach (var item in input.Data)
        {
            if (item.Value is not null)
            {
                flag = true;
                max = Math.Max(max, item.Value.Value);
            }
            if (firstStartTime is null)                 // this runs only once
                firstStartTime = item.StartTime;
            dataValueStatus = dataValueStatus.Intersect(item.DataValueStatus);
        }
        DateTime startTime = firstStartTime ?? DateTime.MinValue;
        return new BasicValue(flag ? max : null, startTime, dataValueStatus);
    }
}
