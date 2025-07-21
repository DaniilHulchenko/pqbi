using PQS.Data.Events.Enums;
using PQS.Translator;
using PQS.Translator.Utils;
using System.Reflection;

namespace PQBI.Infrastructure.Sapphire;

public static class EventFactory
{
    private static IEnumerable<int> BlackList = Enumerable.Range(30000, 30).ToHashSet();

    public static IEnumerable<EventClassDescription> GetAllEventInfos()
    {
        var list = new List<EventClassDescription>();

        var enumType = typeof(EventClass);

        foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var enumValue = (EventClass)field.GetValue(null);
            var index = (int)enumValue;
            if (BlackList.Contains(index) == false)
            {
                continue;
            }

            var attribute = field.GetCustomAttribute(typeof(LocalizedDescriptionAttribute)) as LocalizedDescriptionAttribute;

            if (attribute != null)
            {
                list.Add(new EventClassDescription
                {
                    EventClass = enumValue,
                    Alias = enumValue.ToString(),
                    Description = enumValue.Description() // Description from the attribute
                });
            }
            else
            {
                list.Add(new EventClassDescription
                {
                    EventClass = enumValue,
                    Alias = enumValue.ToString(),
                    Description = enumValue.ToString() // Description from the attribute
                });
            }
        }

        return list;
    }

    public static IEnumerable<EventClass> GetAllEvents()
    {
        var list = new List<EventClass>();

        var enumType = typeof(EventClass);

        foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            list.Add((EventClass)field.GetValue(null));
        }

        return list;
    }
}

