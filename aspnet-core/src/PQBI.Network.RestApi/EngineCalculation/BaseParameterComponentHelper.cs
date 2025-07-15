using PQBI.PQS.CalcEngine;
using PQS.Data.Measurements;

namespace PQBI.Network.RestApi.EngineCalculation;
public static class BaseParameterComponentHelper
{
    public static BaseParameterComponent GetFeeder(BaseParameter parameter, FeederComponentInfo feeder)
    {
        var feederId = feeder.Id;
        var msrParam = GetFeederParameter(parameter, feederId?.ToString());
        return new BaseParameterComponent(parameter, feeder, msrParam, ParameterListItemType.Logical);
    }

    public static MeasurementParameterBase GetCustomParameter(BaseParameter parameter, FeederComponentInfo feeder)
    {        
        List<string> prmSectionList = [parameter.Group, parameter.SyncInterval.ToString(), parameter.BaseResolution, parameter.ScadaQuantityName];

        return MeasurementParameterFactory.GenerateNewMesurmentParameterWithoutSplit(prmSectionList.ToArray());
    }

    public static MeasurementParameterBase GetFeederParameter(BaseParameter parameter, string feederId)
    {
        List<string> prmSectionList = null;
        if (parameter.Harmonics is not null && parameter.Harmonics.Value is not null && parameter.Harmonics.Value > 0)
        {
            var harmonicNum = parameter.Harmonics.Value.ToString();
            prmSectionList = ["STD", parameter.Group, harmonicNum, parameter.SyncInterval.ToString(), parameter.BaseResolution, parameter.ScadaQuantityName, parameter.Phase, feederId];
        }
        else
        {
            prmSectionList = ["STD", parameter.Group, parameter.SyncInterval.ToString(), parameter.BaseResolution, parameter.ScadaQuantityName, parameter.Phase, feederId];
        }

        return MeasurementParameterFactory.GenerateNewMesurmentParameterWithoutSplit(prmSectionList.ToArray());
    }

    public static BaseParameterComponent GetChannel(BaseParameter parameter, FeederComponentInfo component)
    {
        var componentId = component.ComponentId.ToString();
        var msrParam = GetChannelParameter(parameter);
        return new BaseParameterComponent(parameter, component, msrParam, ParameterListItemType.Channel);
    }

    public static MeasurementParameterBase GetChannelParameter(BaseParameter parameter)
    {
        List<string> prmSectionList = null;
        if (parameter.Harmonics is not null && parameter.Harmonics.Value is not null && parameter.Harmonics.Value > 0)
        {
            var harmonicNum = parameter.Harmonics.Value.ToString();

            prmSectionList = ["STD", parameter.Group, harmonicNum, parameter.SyncInterval.ToString(), parameter.BaseResolution, parameter.ScadaQuantityName];
        }
        else
        {
            prmSectionList = ["STD", parameter.Group, parameter.SyncInterval.ToString(), parameter.BaseResolution, parameter.ScadaQuantityName];
        }

        prmSectionList.AddRange(parameter.Phase.Split("_"));
        return MeasurementParameterFactory.GenerateNewMesurmentParameterWithoutSplit(prmSectionList.ToArray());
    }
}
