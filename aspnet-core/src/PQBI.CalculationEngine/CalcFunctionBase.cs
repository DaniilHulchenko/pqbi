using PQBI.CalculationEngine.Functions;

namespace PQBI.CalculationEngine;

public abstract class CalcFunctionBase
{
    public abstract string Alias { get; }

}

public abstract class SingleCalculationFunction : CalcFunctionBase
{
    public abstract BasicValue Calc(SingleAxisInput input, double parameter);
}
