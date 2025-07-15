namespace PQBI.CalculationEngine.Functions.SingleAxisResponse;

public abstract class ArrayCalculationFunction : CalcFunctionBase

{
    public abstract IEnumerable<BasicValue> Calc(SingleAxisInput input, double parameters);
}

