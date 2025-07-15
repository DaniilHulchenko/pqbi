using PQS.Translator;
using System.Reflection;

using PQS.Data.Measurements.Enums;

namespace PQBI.Infrastructure.Sapphire;

public class BaseDataInfo
{
    public CalculationBase Base { get; init; }
    public string PhaseName => Base.ToString();

    public string Description => Base.Description();

    public static IEnumerable<BaseDataInfo> GetAllBaseDataInfos()
    {
        var list = new List<BaseDataInfo>();

        var enumType = typeof(CalculationBase);

        foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var @base = (CalculationBase)field.GetValue(null);
            list.Add(new BaseDataInfo { Base = @base });
        }

        return list;
    }
}
