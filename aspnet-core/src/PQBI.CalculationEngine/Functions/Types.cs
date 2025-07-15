using PQS.Data.Common.Values;
using PQS.Translator.Utils;

namespace PQBI.CalculationEngine.Functions;


public enum PqbiDataValueStatus : int
{
    Ok = 0,
    Hole,
    Missing,
    Flag,
    FlagMissing
}

public static class PqbiDataValueStatusExtensions
{
    public static Dictionary<DataValueStatus, PqbiDataValueStatus> Mapper = new Dictionary<DataValueStatus, PqbiDataValueStatus> {
        { DataValueStatus.OK, PqbiDataValueStatus.Ok },
        { DataValueStatus.HOLE , PqbiDataValueStatus.Hole },
        { DataValueStatus.FLAG_MISSED_DATA , PqbiDataValueStatus.Missing },
        { DataValueStatus.FLAG_EVENT , PqbiDataValueStatus.Flag },
        { DataValueStatus.FLAG_USER_DEFINE , PqbiDataValueStatus.Missing },
        { DataValueStatus.AVAILABILITY_SKIP , PqbiDataValueStatus.Missing },
        { DataValueStatus.NOAP , PqbiDataValueStatus.Missing },
    };


    public static PqbiDataValueStatus ToPqbiDataValueStatus(this DataValueStatus source)
    {
        return Mapper[source];
    }


    public static PqbiDataValueStatus Intersect(this PqbiDataValueStatus previous, PqbiDataValueStatus current)
    {
        if (previous == PqbiDataValueStatus.FlagMissing || current == PqbiDataValueStatus.FlagMissing)
        {
            return PqbiDataValueStatus.FlagMissing;
        }

        if (previous == PqbiDataValueStatus.Ok && current == PqbiDataValueStatus.Ok)
        {
            return PqbiDataValueStatus.Ok;
        }

        if ((previous == PqbiDataValueStatus.Ok && current == PqbiDataValueStatus.Hole) || (previous == PqbiDataValueStatus.Hole && current == PqbiDataValueStatus.Ok))
        {
            return PqbiDataValueStatus.Missing;
        }

        if ((previous == PqbiDataValueStatus.Ok && current == PqbiDataValueStatus.Missing) || (previous == PqbiDataValueStatus.Missing && current == PqbiDataValueStatus.Ok))
        {
            return PqbiDataValueStatus.Missing;
        }

        if ((previous == PqbiDataValueStatus.Ok && current == PqbiDataValueStatus.Flag) || (previous == PqbiDataValueStatus.Flag && current == PqbiDataValueStatus.Ok))
        {
            return PqbiDataValueStatus.Flag;
        }

        //---------------------------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------------------------

        if (previous == PqbiDataValueStatus.Hole && current == PqbiDataValueStatus.Hole)
        {
            return PqbiDataValueStatus.Hole;
        }

        if ((previous == PqbiDataValueStatus.Hole && current == PqbiDataValueStatus.Missing) || (previous == PqbiDataValueStatus.Missing && current == PqbiDataValueStatus.Hole))
        {
            return PqbiDataValueStatus.Missing;
        }

        if ((previous == PqbiDataValueStatus.Hole && current == PqbiDataValueStatus.Flag) || (previous == PqbiDataValueStatus.Flag && current == PqbiDataValueStatus.Hole))
        {
            return PqbiDataValueStatus.FlagMissing;
        }

        //---------------------------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------------------------

        if ((previous == PqbiDataValueStatus.Missing && current == PqbiDataValueStatus.Flag) || (previous == PqbiDataValueStatus.Flag && current == PqbiDataValueStatus.Missing))
        {
            return PqbiDataValueStatus.FlagMissing;
        }

        //---------------------------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------------------------

        if ((previous == PqbiDataValueStatus.Flag) && (current == PqbiDataValueStatus.Flag))
        {
            return PqbiDataValueStatus.Flag;
        }

        //Throw not ImplementException
        return PqbiDataValueStatus.FlagMissing;
    }
}



public readonly record struct BasicValue(double? Value, PqbiDataValueStatus DataValueStatus);

public class SingleAxisInput
{
    public IEnumerable<BasicValue> Data { get; set; }
}
