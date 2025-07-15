using PQS.Translator;
using System.Reflection;

using PQS.Data.Measurements.Enums;

namespace PQBI.Infrastructure.Sapphire;

public class QuantityDataInfo
{
    public QuantityEnum Quantity { get; init; }
    public string PhaseName => Quantity.ToString();

    public string Description => Quantity.Description();

    public static IEnumerable<QuantityDataInfo> GetAllQuantityDataInfos()
    {
        var list = new List<QuantityDataInfo>();

        var enumType = typeof(QuantityEnum);

        foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var quantity = (QuantityEnum)field.GetValue(null);
            list.Add(new QuantityDataInfo { Quantity = quantity });
        }

        return list;
    }
}