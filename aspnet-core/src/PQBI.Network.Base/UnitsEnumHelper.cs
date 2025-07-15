using PQS.Data.Common.Units;
using PQS.Translator.Utils;
using System.Reflection;

namespace PQBI;

public static class UnitsEnumHelper
{
    public static List<(string DescriptionKey, UnitsEnum Value)> GetDescriptionValuePairs()
    {
        return Enum.GetValues(typeof(UnitsEnum))
                   .Cast<UnitsEnum>()
                   .Select(e => (GetLocalizedDescriptionKey(e), e))
                   .ToList();
    }

    public static string GetLocalizedDescriptionKey(UnitsEnum value)
    {
        var field = value.GetType().GetField(value.ToString());
        if (field != null)
        {
            var attr = field.GetCustomAttribute<LocalizedDescriptionAttribute>();
            return attr.DescriptionCode ?? value.ToString();
        }

        return value.ToString();
    }
}
