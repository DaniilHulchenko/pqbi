using PQS.Data.Common.Values;

namespace PQBI.CalculationEngine.Functions.Aggregation;

public class SumCalcFunction : SingleCalculationFunction
{
    public const string Sum_Function = "sum";

    public override string Alias => Sum_Function;

    public override BasicValue Calc(SingleAxisInput input, double parameter)
    {
        double? sum = 0;
        var data = input.Data;
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
}
