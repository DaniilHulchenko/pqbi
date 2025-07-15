namespace PQBI.CalculationEngine.Functions.Aggregation;

public class RmsCalcFunction : SingleCalculationFunction
{
    public const string Rms_Function = "rms";

    public override string Alias => Rms_Function;

    public override BasicValue Calc(SingleAxisInput input, double parameter)
    {
        double sum = 0;
        var data = input.Data;
        var count = 0;
        var dataValueStatus = PqbiDataValueStatus.Ok;

        foreach (var item in data)
        {
            if (item.Value is not null)
            {
                sum += (item.Value.Value * item.Value.Value);
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
}
