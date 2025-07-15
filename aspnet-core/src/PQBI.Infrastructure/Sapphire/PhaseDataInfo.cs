using PQS.Translator;
using System.Reflection;

using PQS.Data.Measurements.Enums;

namespace PQBI.Infrastructure.Sapphire;

public class PhaseDataInfo
{
    public PhaseMeasurementEnum Phase { get; init; }
    public string PhaseName => Phase.ToString();

    public string Description => Phase.Description();

    public static IEnumerable<PhaseDataInfo> GetAllPhaseDataInfos()
    {
        var list = new List<PhaseDataInfo>();

        var enumType = typeof(PhaseMeasurementEnum);

        foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var phase = (PhaseMeasurementEnum)field.GetValue(null);
            list.Add(new PhaseDataInfo { Phase = phase });
        }

        return list;
    }
}
