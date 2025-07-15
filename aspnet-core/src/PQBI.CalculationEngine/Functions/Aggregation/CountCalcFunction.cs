namespace PQBI.CalculationEngine.Functions.Aggregation;

public class CountCalcFunction : SingleCalculationFunction
{
    public const string Count_Function = "count";

    public override string Alias => Count_Function;

    public override BasicValue Calc(SingleAxisInput input, double parameter)
    {
        double count = 0;
        var dataValueStatus = PqbiDataValueStatus.Ok;

        foreach (var item in input.Data)
        {
            if (item.Value is not null)
            {
                count++;
            }

            dataValueStatus = dataValueStatus.Intersect(item.DataValueStatus);

        }

        return new BasicValue(count, dataValueStatus);
    }
}