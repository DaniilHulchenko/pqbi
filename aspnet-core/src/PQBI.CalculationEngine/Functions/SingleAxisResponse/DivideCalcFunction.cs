namespace PQBI.CalculationEngine.Functions.SingleAxisResponse;

public class DivideCalcFunction : ArrayCalculationFunction
{
    public const string Devide_Function = "divide";

    public override string Alias => Devide_Function;

    public override IEnumerable<BasicValue> Calc(SingleAxisInput input , double parameter)
    {
        var result = new List<BasicValue>();
        var amountOfList = input.Data.Count();

        foreach (var item in input.Data)
        {

            if (item.Value is not null)
            {
                result.Add(new BasicValue((double)(item.Value / parameter), item.DataValueStatus));
            }
            else
            {
                result.Add(new BasicValue(item.Value, item.DataValueStatus));
            }
        }

        return  result ;
    }
}