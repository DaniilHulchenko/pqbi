namespace PQBI.CalculationEngine.Functions.Aggregation;

public class MinCalcFunction : SingleCalculationFunction
{
    public const string Min_Function = "min";

    public override string Alias => Min_Function;

    public override BasicValue Calc(SingleAxisInput input, double parameter)
    {
        var flag = false;
        double min = double.MaxValue;
        var dataValueStatus = PqbiDataValueStatus.Ok;

        foreach (var item in input.Data)
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
}