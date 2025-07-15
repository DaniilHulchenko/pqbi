using PQBI.CalculationEngine.Functions.SingleAxisResponse;

namespace PQBI.CalculationEngine.Functions.CalcSingleAxis;



public class AbsoluteCalcFunction : ArrayCalculationFunction
{
    public const string Absolute_Function = "absolute";

    public override string Alias => Absolute_Function;

    public override IEnumerable<BasicValue> Calc(SingleAxisInput input, double parameters)
    {
        var result = new List<BasicValue>();
        var amountOfList = input.Data.Count();


        foreach (var item in input.Data)
        {

            if (item.Value is not null)
            {
                result.Add( new BasicValue(Math.Abs(item.Value.Value), item.DataValueStatus));
            }
            else
            {
                result.Add(new BasicValue(item.Value,item.DataValueStatus));
            }
        }

        return result;
    }
}