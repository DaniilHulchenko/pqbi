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


        foreach (var item in input.Data)
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
}
