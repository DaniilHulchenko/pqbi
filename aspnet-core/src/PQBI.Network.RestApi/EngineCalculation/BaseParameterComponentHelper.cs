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
        var baseResolutionParts = parameter.BaseResolution.Split('_'); // split by underscore

        var prmSectionList = new List<string>
                    {
                        "STD",
                        parameter.Group
                    };

        // safely handle harmonics
        if (parameter.Harmonics?.Value is int harmonicNum)
        {
            prmSectionList.Add(harmonicNum.ToString());
        }

        prmSectionList.Add(parameter.SyncInterval.ToString());

        // add all split parts from BaseResolution
        prmSectionList.AddRange(baseResolutionParts);

        prmSectionList.Add(parameter.ScadaQuantityName);
        prmSectionList.Add(parameter.Phase);
        prmSectionList.Add(feederId);

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
        var baseResolutionParts = parameter.BaseResolution.Split('_'); // split by underscore

        var prmSectionList = new List<string>
                    {
                        "STD",
                        parameter.Group
                    };

        // safely handle harmonics
        if (parameter.Harmonics?.Value is int harmonicNum)
        {
            prmSectionList.Add(harmonicNum.ToString());
        }

        prmSectionList.Add(parameter.SyncInterval.ToString());

        // add all split parts from BaseResolution
        prmSectionList.AddRange(baseResolutionParts);

        prmSectionList.Add(parameter.ScadaQuantityName);
        prmSectionList.AddRange(parameter.Phase.Split("_"));
              
        return MeasurementParameterFactory.GenerateNewMesurmentParameterWithoutSplit(prmSectionList.ToArray());
    }
}
