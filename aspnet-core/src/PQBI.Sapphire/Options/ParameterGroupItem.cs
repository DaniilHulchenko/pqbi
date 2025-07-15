using PQBI.Sapphire.Options;
using PQS.Data.Common.Attributes;
using System.Reflection;
using MeasurementGroup = PQS.Data.Measurements.Enums.Group;

namespace PQBI.IntegrationTests.Scenarios.PopulatingParameters;

public class ParameterGroupItem
{
    public MeasurementGroup Group { get; set; }
    public string Description { get; set; }

    public bool IsHarmonic
    {
        get
        {
            var isHarmonic = false;

            var fieldInfo = Group.GetType().GetField(Group.ToString());
            var harmonicAttribute = fieldInfo.GetCustomAttribute<IsHarmonicAttribute>();

            if (harmonicAttribute is not null)
            {
                isHarmonic = harmonicAttribute.IsHarmonic;
            }

            return isHarmonic;
        }
    }
}
