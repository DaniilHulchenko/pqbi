using PQS.Data.Measurements.Enums;
using PQS.Data.Measurements;

namespace PQBI.Sapphire.Options;


#nullable enable
public class CalcBaseWindowInterval : IEquatable<CalculationBase>, IEquatable<CalcBase>, ICloneable, IComparable<CalcBaseWindowInterval>
{
    public CalcBase CalculationBase { private set; get; }
    public WindowInterval? WindowInterval { private set; get; } = null;

    public CalcBaseWindowInterval(CalcBase calculationBase, WindowInterval? windowInterval = null)
    {
        CalculationBase = calculationBase;
        WindowInterval = windowInterval;
    }

    public CalcBaseWindowInterval(CalculationBase calculationBase)
    {
        CalculationBase = new CalcBase(calculationBase);
    }
    //public bool IsCustom => CalculationBase.CalculationBaseEnum == Enums.CalculationBase.BX || CalculationBase.CalculationBaseEnum == Enums.CalculationBase.BS;

    public static implicit operator (CalcBase CalculationBase, WindowInterval? WindowInterval)(CalcBaseWindowInterval calcBaseWindowInterval) => (calcBaseWindowInterval.CalculationBase, calcBaseWindowInterval.WindowInterval);



    public override int GetHashCode() => CalculationBase.GetHashCode();

    public override string ToString() => WindowInterval == null ? CalculationBase.ToString() : $"{CalculationBase} {WindowInterval}";

    public bool Equals(CalculationBase other)
    {
        if (WindowInterval != null)
            return false;

        return CalculationBase.Equals(new CalcBase(other));
    }

    public bool Equals(CalcBase? other)
    {
        return CalculationBase.Equals(other);
    }

    public object Clone() => new CalcBaseWindowInterval((CalcBase)CalculationBase.Clone(), (WindowInterval)WindowInterval?.Clone());

    public override bool Equals(object? obj)
    {
        if (obj is not CalcBaseWindowInterval other)
        {
            return false;
        }

        return CalculationBase.Equals(other.CalculationBase) && WindowInterval == other.WindowInterval;
    }

    public int CompareTo(CalcBaseWindowInterval? other)
    {
        int result = CalculationBase.CompareTo(other?.CalculationBase);
        if (result == 0)
        {
            if (WindowInterval == null && other?.WindowInterval == null)
            {
                return 0;
            }
            else if (WindowInterval == null)
            {
                return -1;
            }
            else if (other?.WindowInterval == null)
            {
                return 1;
            }
            else
            {
                //TODO: to return
                //return WindowInterval.CompareTo(other.WindowInterval);
            }
        }
        return result;
    }

    public static bool operator ==(CalcBaseWindowInterval? left, CalcBaseWindowInterval? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.Equals(right);
    }
    public static bool operator !=(CalcBaseWindowInterval? left, CalcBaseWindowInterval? right) => !(left == right);
}
