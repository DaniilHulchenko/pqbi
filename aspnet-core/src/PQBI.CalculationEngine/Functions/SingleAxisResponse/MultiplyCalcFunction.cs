namespace PQBI.CalculationEngine.Functions.SingleAxisResponse;

public class MultiplyCalcFunction : ArrayCalculationFunction
{
    public const string Multiply_Function = "mult";

    public override string Alias => Multiply_Function;

    public override IEnumerable<BasicValue> Calc(SingleAxisInput input, double parameter)
    {
        var result = new List<BasicValue>();
        var amountOfList = input.Data.Count();

        foreach (var item in input.Data)
        {
            if (item.Value is not null)
            {
                result.Add(new BasicValue(item.Value * parameter, item.DataValueStatus));
            }
            else
            {
                result.Add(new BasicValue(item.Value, item.DataValueStatus));
            }
        }

        return result;
    }
}
