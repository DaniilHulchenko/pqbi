using PQS.Translator;
using System.Reflection;
using PQS.Data.Measurements.Enums;
using PQS.Data.Common.Attributes;

namespace PQBI.Infrastructure.Sapphire;

public class GroupDataInfo
{
    public Group GroupId { get; init; }
    public string GroupName => GroupId.ToString();

    public string Description => GroupId.Description();

    public bool IsHarmonic
    {
        get
        {
            var isHarmonic = false;

            var fieldInfo = GroupId.GetType().GetField(GroupId.ToString());
            var harmonicAttribute = fieldInfo.GetCustomAttribute<IsHarmonicAttribute>();

            if (harmonicAttribute is not null)
            {
                isHarmonic = harmonicAttribute.IsHarmonic;
            }

            return isHarmonic;
        }
    }


    public static IEnumerable<GroupDataInfo> GetAllGroupDataInfos()
    {
        var list = new List<GroupDataInfo>();

        var enumType = typeof(Group);

        foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var group = (Group)field.GetValue(null);
            list.Add(new GroupDataInfo { GroupId = group });
        }

        return list;
    }
}
