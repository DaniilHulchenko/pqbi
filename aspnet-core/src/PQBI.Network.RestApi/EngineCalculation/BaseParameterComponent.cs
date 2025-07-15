using PQBI.CalculationEngine.Matrix;
using PQBI.Infrastructure.Extensions;
using PQBI.PQS.CalcEngine;
using PQBI.Tenants.Dashboard.Dto;
using PQS.Data.Common;
using PQS.Data.Measurements;
using PQS.Data.Networks;
using System.Collections.Generic;

namespace PQBI.Network.RestApi.EngineCalculation;

public static class BaseParameterComponentExtensions
{
    public static IEnumerable<BaseParameterComponent> CreateBaseParameterComponents(this IEnumerable<BaseParameter> baseParameters, IEnumerable<FeederComponentInfo> feeders)
    {
        var parameterComponents = new List<BaseParameterComponent>();
        foreach (var baseParameter in baseParameters)
        {
            var buffer = baseParameter.CreateBaseParameterComponents(feeders);
            parameterComponents.AddRange(buffer);
        }

        return parameterComponents;
    }


    public static IEnumerable<BaseParameterComponent> CreateExceptionBaseParameterComponents(this IEnumerable<BaseParameter> baseParameters)
    {
        var parameterComponents = CreateBaseParameterComponents(baseParameters, []);
        return parameterComponents;
    }

    public static IEnumerable<BaseParameterComponent> CreateBaseParameterComponents(this BaseParameter parameter, IEnumerable<FeederComponentInfo> feeders)
    {
        var result = new List<BaseParameterComponent>();
        var parameterListType = CalculationStaticTypes.GetParameterListType(parameter.Type);

        if (parameter.IsExceptionParameter)
        {
            var exception = parameter.CreateBaseParameterComponentForException();
            return [exception];
        }

        if (feeders.IsCollectionEmpty())
        {
            return [];
        }

        switch (parameterListType)
        {
            case ParameterListItemType.Logical:
                var feederBaseParameters = parameter.GetBaseParameterFeeders(feeders);
                result.AddRange(feederBaseParameters);
                break;

            case ParameterListItemType.Channel:
                var channels = parameter.GetBaseParameterChannels(feeders);
                result.AddRange(channels);
                break;

            case ParameterListItemType.Additional:
                var custom = parameter.GetAdditionalarameter(feeders);
                result.AddRange(custom);
                break;

            default:
                break;
        }

        return result;
    }

    public static IEnumerable<BaseParameterComponent> GetBaseParameterFeeders(this BaseParameter baseParameter, IEnumerable<FeederComponentInfo> feederComponentInfos)
    {
        var result = new List<BaseParameterComponent>();

        foreach (var feederComponentInfo in feederComponentInfos)
        {
            var baseParameterComponent = BaseParameterComponentHelper.GetFeeder(baseParameter, feederComponentInfo);
            //var baseParameterComponent = BaseParameterComponentHelper.GetFeeder(baseParameter, feederComponentInfo.ComponentId.ToString(), feederComponentInfo.Id.ToString());
            result.Add(baseParameterComponent);
        }

        return result;
    }

    public static IEnumerable<BaseParameterComponent> GetBaseParameterChannels(this BaseParameter baseParameter, IEnumerable<FeederComponentInfo> channels)
    {
        var result = new List<BaseParameterComponent>();

        foreach (var channel in channels)
        {
            var baseParameterComponent = BaseParameterComponentHelper.GetChannel(baseParameter, channel);
            //var baseParameterComponent = BaseParameterComponentHelper.GetChannel(baseParameter, channel.ComponentId.ToString());
            result.Add(baseParameterComponent);
        }

        return result;
    }

    public static IEnumerable<BaseParameterComponent> GetAdditionalarameter(this BaseParameter baseParameter, IEnumerable<FeederComponentInfo> feeders)
    {
        List<BaseParameterComponent> result = new List<BaseParameterComponent>();
        foreach (var feeder in feeders)
        {
            var item = GetAdditionalParameter(baseParameter, feeder);
            result.Add(item);
        }

        return result;
    }

    public static BaseParameterComponent GetAdditionalParameter(BaseParameter baseParameter, FeederComponentInfo feeder)
    {
        var msrParam = BaseParameterComponentHelper.GetCustomParameter(baseParameter, feeder);

        return new BaseParameterComponent(baseParameter, feeder, msrParam, ParameterListItemType.Additional);

    }

    //public static MeasurementParameterBase GetAdditionParameter(BaseParameter parameter)
    //{
    //    List<string> prmSectionList = null;
    //    if (parameter.Harmonics is not null && parameter.Harmonics.Value is not null && parameter.Harmonics.Value > 0)
    //    {
    //        var harmonicNum = parameter.Harmonics.Value.ToString();

    //        prmSectionList = ["STD", parameter.Group, harmonicNum, parameter.SyncInterval.ToString(), parameter.BaseResolution, parameter.ScadaQuantityName];
    //    }
    //    else
    //    {
    //        prmSectionList = ["STD", parameter.Group, parameter.SyncInterval.ToString(), parameter.BaseResolution, parameter.ScadaQuantityName];
    //    }

    //    prmSectionList.AddRange(parameter.Phase.Split("_"));
    //    return MeasurementParameterFactory.GenerateNewMesurmentParameterWithoutSplit(prmSectionList.ToArray());
    //}

    public static BaseParameterComponent CreateBaseParameterComponentForException(this BaseParameter parameter)
    {
        var result = new List<BaseParameterComponent>();

        var parameterListType = CalculationStaticTypes.GetParameterListType(parameter.Type);
        switch (parameterListType)
        {
            case ParameterListItemType.Logical:
                var added = BaseParameterComponentHelper.GetFeeder(parameter, parameter.FromComponents);
                //var added = BaseParameterComponentHelper.GetFeeder(parameter, componentId.ToString(), parameter.FromComponents.Id?.ToString());
                return added;

            case ParameterListItemType.Channel:
                var chanellID = parameter.FromComponents.ComponentId;
                var addedChannel = BaseParameterComponentHelper.GetChannel(parameter, parameter.FromComponents);
                //var addedChannel = BaseParameterComponentHelper.GetChannel(parameter, chanellID.ToString());
                return addedChannel;

            default:
                throw new NotImplementedException();
        }
    }

    public static IEnumerable<BaseParameterComponent> CreateDataForException(this BaseParameter parameter)//, IEnumerable<FeederDataDto> feeders)
    {
        var result = new List<BaseParameterComponent>();

        var parameterListType = CalculationStaticTypes.GetParameterListType(parameter.Type);

        switch (parameterListType)
        {
            case ParameterListItemType.Logical:
                var added = BaseParameterComponentHelper.GetFeeder(parameter, parameter.FromComponents);
                //var added = BaseParameterComponentHelper.GetFeeder(parameter, componentId.ToString(), parameter.FromComponents.Id?.ToString());
                result.Add(added);

                break;

            case ParameterListItemType.Channel:
                var chanellID = parameter.FromComponents.ComponentId;
                var addedChannel = BaseParameterComponentHelper.GetChannel(parameter, parameter.FromComponents);
                //var addedChannel = BaseParameterComponentHelper.GetChannel(parameter, chanellID.ToString());
                result.Add(addedChannel);
                break;

            default:
                break;
        }


        return result;
    }

}



public class BaseParameterComponent : IMatrixBaseParameterKey
{
    public BaseParameterComponent(BaseParameter parameter, FeederComponentInfo feeder,
        MeasurementParameterBase measurementParameter, ParameterListItemType parameterListItemType)
    {
        Parameter = parameter;
        Feeder = feeder;
        MeasurementParameter = measurementParameter;
        ParameterListItemType = parameterListItemType;
    }

    public bool IsHarmonic => MeasurementParameter.IsHarmonicParameter;

    public string ScadaParameterName => MeasurementParameter.BuidCodeFromParameter();

    public BaseParameter Parameter { get; }
    public FeederComponentInfo Feeder { get; }
    public MeasurementParameterBase MeasurementParameter { get; }
    public string AggregationFunction => Parameter.AggregationFunction;
    public string Operator => Parameter.Operator;
    public int BaseParameterResolutionInSeconds => Parameter.Resolution ?? 0;

    public ParameterListItemType ParameterListItemType { get; }
    public string BaseParameterName => Parameter.Name;
    public Guid ComponentID => Feeder.ComponentId;
    public int? FeederId => Feeder?.Id;
    public DataUnitType DataUnitType => Axis?.DataUnitType;

    public string ParameterId
    {
        get
        {
            if (FeederId is not null)
            {
                var id = $"{ScadaParameterName}_{ComponentID}_{FeederId}";
                return id;
            }

            var tmp = $"{ScadaParameterName}_{ComponentID}";
            return tmp;
        }
    }

    public PQZStatus PQZStatus
    {
        get
        {
            if (Axis is null)
            {
                return PQZStatus.MISSING_OR_WRONG_CONFIGURATIONS;
            }
            return Axis.PQZStatus;
        }
    }

    public PQBIAxisData Axis { get; set; }

    public void SetRawData(PQBIAxisData axisses, bool isToCalculate, double? nominalValue)
    {
        Axis = axisses;

        //if (isToCalculate)
        //{
        //    var nominal = nominalValue ?? Axis.Nominal ?? 1;

        //    foreach (var item in Axis.DataTimeStamps)
        //    {
        //        item.Point /= nominal;
        //    }
        //}
    }
}
