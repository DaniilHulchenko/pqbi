using Abp.UI;
using Microsoft.Extensions.Logging;
using PQBI.CalculationEngine;
using PQBI.CalculationEngine.Functions;
using PQBI.CalculationEngine.Functions.CalcSingleAxis;
using PQBI.CalculationEngine.Matrix;
using PQBI.Infrastructure.Extensions;
using PQBI.PQS;
using PQBI.PQS.CalcEngine;
using PQBI.Tenants.Dashboard.Dto;
using PQS.CommonReport;
using PQS.Data.Common;
using PQS.Data.Common.Units;
using System.Data;

namespace PQBI.Network.RestApi.EngineCalculation;

public interface IEngineCalculationService
{
    BasicValue AggregationFunctionsAsync(string aggregationFunc, IEnumerable<BasicValue> numbers);
    IEnumerable<GraphParametersComponentDtoV3> RootCalculation(CustomParameterNodeCalculator node);
    IEnumerable<BasicValue> CalculatedInnerAlignment(CustomParameterNodeCalculator node, BaseParameterComponent item);
    void CalculateFinalMatrixChildless(CustomParameterNodeCalculator node);
    void AddFinalMaxtrixCalculationWithChildren(CustomParameterNodeCalculator node);
}

public class EngineCalculationService : IEngineCalculationService
{

    private readonly ILogger<EngineCalculationService> _logger;
    private readonly IFunctionEngine _engineCalculator;

    public EngineCalculationService(ILogger<EngineCalculationService> logger, IFunctionEngine engineCalculator)
    {
        _logger = logger;
        _engineCalculator = engineCalculator;
    }

    public BasicValue AggregationFunctionsAsync(string aggregationFunc, IEnumerable<BasicValue> numbers)
    {
        var result = _engineCalculator.AggregationCalculationAsync(aggregationFunc, numbers);
        return result;
    }

    private IEnumerable<GraphParametersComponentDtoV3> GetAxisForBaseParameter(CustomParameterNodeCalculator node)
    {
        List<GraphParametersComponentDtoV3> response = new List<GraphParametersComponentDtoV3>();
        NormalizeEnum normalizeEnum = NormalizeEnum.NO;
        double nominalVal = 1;
        var settings = node.AdvancedSettingsForTable;
        if (settings is { NormalizeType: NormalizeEnum.VALUE, NominalVal: double nv })
        {
            normalizeEnum = NormalizeEnum.VALUE;
            nominalVal = nv;
        }
        else if (settings is { NormalizeType: NormalizeEnum.NOMINAL })
            normalizeEnum = NormalizeEnum.NOMINAL;

        var missings = new List<MissingBaseParameterInfo>();
        foreach (var baseParameter in node.BaseParameterComponents)
        {
            var baseParameterAxis = new List<AxisValue>();

            if (baseParameter.Axis is null)
            {
                //todo investigate should not happen
                baseParameter.SetRawData(new PQBIAxisDataEmpty(baseParameter.ComponentID, baseParameter.FeederId, baseParameter.BaseParameterName, PQZStatus.GENERAL_ERROR, baseParameter.DataUnitType), false, null);
            }

            var points = baseParameter.Axis.DataTimeStamps.Select(x => new BasicValue(x.Point, x.DataValueStatus.ToPqbiDataValueStatus())).ToArray();          

            IEnumerable<AxisValue> axisValCollection = null;
            if (baseParameter.Axis.PQZStatus == PQZStatus.OK)
            {
                if (node.AdvancedSettingsForTable?.IsExcludeFlaggedData == true)
                {
                    //ParameterMatrix parameterMatrix = new ParameterMatrix();

                    if (normalizeEnum != NormalizeEnum.NO)
                    {
                        var unitState = UnitsEnum.STD_PERCENT;
                        var token = UnitsEnumHelper.GetLocalizedDescriptionKey(unitState);
                        baseParameter.Axis.DataUnitType = new DataUnitType((int)unitState, token);                      
                        if (normalizeEnum == NormalizeEnum.NOMINAL)
                            nominalVal = baseParameter.Axis.Nominal ?? 100;
                    }

                    GroupByFunctionInput groupByOperation;
                    int resolutionInSeconds = node.WidgetResolutionAutoOrInSeconds;

                    if (node.IsWidgetResolutionAuto)
                    {
                        //axisValCollection = SetAutoModeAsync(node, points);

                        var auto = new AutoCalcFunction();
                        var numInGroup = auto.CalcNumInGroup(node.AutoWishListNumber, points);
                        groupByOperation = new GroupByFunctionInput(points, numInGroup);                         
                       
                        TimeSpan interval = TimeSpan.FromTicks(node.Duration.Ticks / node.AutoWishListNumber);
                        resolutionInSeconds = (int)interval.TotalSeconds;
                    }
                    else
                    {
                        groupByOperation = new GroupByFunctionInput(points, resolutionInSeconds, node.CustomParameterResolutionRecalculatedInSeconds);
                        //{
                        //    Data = points,
                        //    NumInGroup = resolutionInSeconds / node.CustomParameterResolutionRecalculatedInSeconds
                        //    //ResolutionInSeconds = resolutionInSeconds,
                        //    //SyncInSeconds = node.CustomParameterResolutionRecalculatedInSeconds
                        //};                        
                    }
                    var data = _engineCalculator.CalcGroupByAsync(groupByOperation);

                    List<BasicValue[]> dataGroupList = data.Select(group => group.ToArray()).ToList();
                    dataGroupList = ParameterMatrix.FilterByFlaggedEvents(true, dataGroupList);

                    if (normalizeEnum == NormalizeEnum.NO)
                        axisValCollection = CalculateQuantityFunction(node.WidgetAggragationFunction, node.StartDate.ToDateTimeOffsetInSeconds(), resolutionInSeconds, dataGroupList);
                    else
                        axisValCollection = CalculateQuantityAndNormalizeFunction(node.WidgetAggragationFunction, node.StartDate.ToDateTimeOffsetInSeconds(), resolutionInSeconds, dataGroupList, nominalVal);
                }
                else
                {
                    if (normalizeEnum == NormalizeEnum.NO)
                    {
                        foreach (var dataTimeStamp in baseParameter.Axis.DataTimeStamps)
                        {
                            baseParameterAxis.Add(new AxisValue { Value = dataTimeStamp.Point, TimeStempInSeconds = dataTimeStamp.DateTime.ToDateTimeOffsetInSeconds() });
                        }                      
                    }
                    else
                    {
                        if (normalizeEnum != NormalizeEnum.NO)
                        {
                            var unitState = UnitsEnum.STD_PERCENT;
                            var token = UnitsEnumHelper.GetLocalizedDescriptionKey(unitState);
                            baseParameter.Axis.DataUnitType = new DataUnitType((int)unitState, token);
                            if (normalizeEnum == NormalizeEnum.NOMINAL)
                                nominalVal = baseParameter.Axis.Nominal ?? 100;
                        }

                        foreach (var dataTimeStamp in baseParameter.Axis.DataTimeStamps)
                        {
                            double? pointVal = ParameterMatrix.Normalize(nominalVal, dataTimeStamp.Point);
                            baseParameterAxis.Add(new AxisValue { Value = pointVal, TimeStempInSeconds = dataTimeStamp.DateTime.ToDateTimeOffsetInSeconds() });
                        }                        
                    }                    
                }                   
            }
            else
            {
                missings.Add(new MissingBaseParameterInfo(baseParameter.BaseParameterName, baseParameter.Axis.PQZStatus, baseParameter.Axis.PQZStatus.ToString()));
            }

            string prmNameWithFeeder;
            string prmName = MsrPrmTranslator.GetMsrPrmNameForGraphLegend(baseParameter.MeasurementParameter, true);
            if (!string.IsNullOrEmpty(baseParameter.Feeder.CompName))
                prmNameWithFeeder = $"{baseParameter.Feeder.CompName}, {baseParameter.Feeder.Name} - {prmName}";
            else
                prmNameWithFeeder = $"{baseParameter.Feeder.Name} - {prmName}";
            var tmp = BaseParameterPrepareBaseParameterResponse(baseParameter.ScadaParameterName, prmNameWithFeeder, baseParameter.ComponentID, baseParameter.FeederId, baseParameter.DataUnitType, node.CustomParameterType, baseParameterAxis, missings);
            response.Add(tmp);
        }

        return response;
    }

    public GraphParametersComponentDtoV3 BaseParameterPrepareBaseParameterResponse(string parameterName, string prmNameToDisplay, Guid componentId, int? feaederId, DataUnitType dataUnitType, CustomParameterType customParameterType, IEnumerable<AxisValue> axises, IEnumerable<MissingBaseParameterInfo> missingBaseParameterInfos)
    {

        var tmp = new GraphParametersComponentDtoV3(prmNameToDisplay, [new FeederComponentInfo { ComponentId = componentId, Id = feaederId }], customParameterType.ToString(), dataUnitType, [parameterName], axises, missingBaseParameterInfos);
        return tmp;
    }

    public IEnumerable<GraphParametersComponentDtoV3> RootCalculation(CustomParameterNodeCalculator node)
    {
        List<GraphParametersComponentDtoV3> responsess = new List<GraphParametersComponentDtoV3>();

        switch (node.CustomParameterType)
        {
            case CustomParameterType.SPMC:

                var axises = GetAxisForSingleParameter(node);
                var response = PrepareResponseForSPMCAndMPSC(node, axises);
                responsess.Add(response);

                break;

            case CustomParameterType.MPSC:

                var multiParameterAxis = GetAxisesForMultiParameter(node);
                responsess.AddRange(multiParameterAxis);
                break;

            case CustomParameterType.BPCP:

                //if (node.BaseParameterComponents.Count() > 0)
                //{
                //    node.BaseParameterComponents.First().Axis != null
                //}

                var baseParameterResponse = GetAxisForBaseParameter(node);
                responsess.AddRange(baseParameterResponse);
                break;

            case CustomParameterType.Exception:

                var exceptionParameters = GetAxisesForMultiParameter(node);
                responsess.AddRange(exceptionParameters);
                break;

            default:
                throw new UserFriendlyException("Tree calculation case doesnt not exist");
        }

        return responsess;
    }


    private IEnumerable<AxisValue> GetAxisForSingleParameter(CustomParameterNodeCalculator node)
    {

        var finalMatrix = node.FinalAggregationMatrixes.FirstOrDefault();
        if (finalMatrix is null)
        {
            return [];
        }

        IEnumerable<AxisValue> response = null;
        using (var mainLogger = PqbiStopwatch.Anchor($"{nameof(GetAxisForSingleParameter)}"))
        {
            GroupByFunctionInput groupByOperation;
            var resolutionInSeconds = node.WidgetResolutionAutoOrInSeconds;
            if (node.IsWidgetResolutionAuto)
            {
                var auto = new AutoCalcFunction();
                var numInGroup = auto.CalcNumInGroup(node.AutoWishListNumber, finalMatrix.AggregationCalculation);
                groupByOperation = new GroupByFunctionInput(finalMatrix.AggregationCalculation, numInGroup);
             
                TimeSpan interval = TimeSpan.FromTicks(node.Duration.Ticks / node.AutoWishListNumber);
                resolutionInSeconds = (int)interval.TotalSeconds;
            }
            else
            {
                groupByOperation = new GroupByFunctionInput(finalMatrix.AggregationCalculation, node.WidgetResolutionAutoOrInSeconds, node.CustomParameterResolutionRecalculatedInSeconds);
              
            }
            var data = _engineCalculator.CalcGroupByAsync(groupByOperation);

            
            //var resolutionInSeconds = GroupByCalcFunction.ParseAndConvertToSecond(node.WidgetResolution);
            response = CalculateQuantityFunction(node.WidgetAggragationFunction, node.StartDate.ToDateTimeOffsetInSeconds(), resolutionInSeconds, data);
        }

        return response;
    }

    private IEnumerable<GraphParametersComponentDtoV3> GetAxisesForMultiParameter(CustomParameterNodeCalculator node)
    {
        List<GraphParametersComponentDtoV3> responses = new List<GraphParametersComponentDtoV3>();

        var index = 0;
        foreach (var finalMatrix in node.FinalAggregationMatrixes)
        {
            var res = new List<GraphParametersComponentDtoV3>();
            IEnumerable<AxisValue> axis = null;
            var resolutionInSeconds = node.WidgetResolutionAutoOrInSeconds;
            GroupByFunctionInput groupByOperation;
            if (node.IsWidgetResolutionAuto)
            {
                //axis = SetAutoModeAsync(node, finalMatrix.AggregationCalculation);

                var auto = new AutoCalcFunction();
                var numInGroup = auto.CalcNumInGroup(node.AutoWishListNumber, finalMatrix.AggregationCalculation);
                groupByOperation = new GroupByFunctionInput(finalMatrix.AggregationCalculation, numInGroup);                                

                TimeSpan interval = TimeSpan.FromTicks(node.Duration.Ticks / node.AutoWishListNumber);
                resolutionInSeconds = (int)interval.TotalSeconds;
            }
            else
            {
                groupByOperation = new GroupByFunctionInput(finalMatrix.AggregationCalculation, resolutionInSeconds, node.CustomParameterResolutionRecalculatedInSeconds);
                //{
                //    Data = finalMatrix.AggregationCalculation,
                //    NumInGroup = resolutionInSeconds / node.CustomParameterResolutionRecalculatedInSeconds
                //    //ResolutionInSeconds = resolutionInSeconds,
                //    //SyncInSeconds = node.CustomParameterResolutionRecalculatedInSeconds,
                //};
            }
            var data = _engineCalculator.CalcGroupByAsync(groupByOperation);
            axis = CalculateQuantityFunction(node.WidgetAggragationFunction, node.StartDate.ToDateTimeOffsetInSeconds(), resolutionInSeconds, data);


            var baseParameterAdditionalInfos = new List<MissingBaseParameterInfo>();
            var parameterMatrix = node.ParameterMatrixes.ElementAt(index);
            if (parameterMatrix is not null)
            {
                foreach (var item in parameterMatrix.InvalidParameters)
                {
                    if (item.Value.Status != PQZStatus.OK)
                    {
                        if (item.Value.BaseParameters.IsCollectionEmpty())
                        {
                            baseParameterAdditionalInfos.Add(new MissingBaseParameterInfo(item.Key, item.Value.Status, item.Value.Status.ToString()));
                        }
                    }
                }
            }

            var graph = new GraphParametersComponentDtoV3(node.CustomParameterName, node.Feeders, node.CustomParameterType.ToString(), finalMatrix.DataUnitType, [], axis, baseParameterAdditionalInfos);
            responses.Add(graph);

            index++;
        }

        return responses;
    }

    //public IEnumerable<AxisValue> SetAutoModeAsync(CustomParameterNodeCalculator node, IEnumerable<BasicValue> externalCalculated)
    //{
    //    if (externalCalculated.IsCollectionEmpty())
    //    {
    //        return [];
    //    }

    //    var auto = new AutoCalcFunction();
    //    var data2 = auto.CalcNumInGroup(node.AutoWishListNumber, externalCalculated);
    //    var calculated = new List<AxisValue>();

    //    TimeSpan interval = TimeSpan.FromTicks(node.Duration.Ticks / data2.Count());
    //    var resolutionInSeconds = (int)interval.TotalSeconds;
    //    var response = CalculateQuantityAutoFunctionAsync222(node.WidgetAggragationFunction, node.StartDate.ToDateTimeOffsetInSeconds(), resolutionInSeconds, data2);
    //    return response;
    //}

    public GraphParametersComponentDtoV3 PrepareResponseForSPMCAndMPSC(CustomParameterNodeCalculator node, IEnumerable<AxisValue> axises)
    {
        var baseParameterAdditionalInfos = new List<MissingBaseParameterInfo>();
        var parameterMatrix = node.ParameterMatrixes.FirstOrDefault();
        DataUnitType dataUnitType = null;
        if (parameterMatrix is not null)
        {
            dataUnitType = parameterMatrix.DataUnitType;
            foreach (var item in parameterMatrix.InvalidParameters)
            {
                if (item.Value.Status != PQZStatus.OK)
                {
                    if (item.Value.BaseParameters.IsCollectionEmpty())
                    {
                        baseParameterAdditionalInfos.Add(new MissingBaseParameterInfo(item.Key, item.Value.Status, item.Value.Status.ToString()));
                        //baseParameterAdditionalInfos.Add(new MissingBaseParameterInfo(item.Key.BaseParameterName, item.Value.Status, item.Value.Status.ToString()));
                    }
                }
            }
        }

        var feeders = new List<FeederComponentInfo>();
        foreach (var feeder in node.Feeders)
        {
            feeders.Add(feeder);
        }

        var result = new GraphParametersComponentDtoV3(node.CustomParameterName, feeders, node.CustomParameterType.ToString(), dataUnitType, [], axises, baseParameterAdditionalInfos);
        return result;
    }

    public void CalculateFinalMatrixChildless(CustomParameterNodeCalculator node)
    {
        using (var mainLogger = PqbiStopwatch.Anchor($"{nameof(CalculateFinalMatrixChildless)}"))
        {
            node.AddFinalMaxtrixCalculationChildless();
            node.CalculateAggregationFinalMatrixChildless();
        }
    }

    public void AddFinalMaxtrixCalculationWithChildren(CustomParameterNodeCalculator node)
    {
        node.AddFinalMaxtrixCalculationWithChildren();
    }


    public IEnumerable<BasicValue> CalculatedInnerAlignment(CustomParameterNodeCalculator node, BaseParameterComponent item)
    {
        IEnumerable<BasicValue> calculated = null;
        using (var mainLogger = PqbiStopwatch.Anchor($"{nameof(CalculatedInnerAlignment)}"))
        {
            var points = item.Axis.DataTimeStamps.Select(x => new BasicValue(x.Point, x.DataValueStatus.ToPqbiDataValueStatus())).ToArray();
            calculated = CalculateOperatorAndAggregation(points, item.Operator, item.AggregationFunction, node.CustomParameterResolutionRecalculatedInSeconds, item.BaseParameterResolutionInSeconds);
        }

        return calculated;
    }

    private IEnumerable<BasicValue> CalculateOperatorAndAggregation(IEnumerable<BasicValue> points, string @operator, string? aggregationFunction, int customParameterResolution, int parameterResolution)
    {
        var calculated = CalculateOperator(points, @operator);

        if (string.IsNullOrEmpty(aggregationFunction) == false)
        {
            calculated = CalculatAggregation(calculated, aggregationFunction, customParameterResolution, parameterResolution);
        }

        return calculated;
    }

    private IEnumerable<BasicValue> CalculateOperator(IEnumerable<BasicValue> points, string @operator)
    {
        using (var mainLogger = PqbiStopwatch.Anchor($"{nameof(CalculateOperator)}"))
        {
            IEnumerable<BasicValue> InnerOperatorCalculated = null;

            if (string.IsNullOrEmpty(@operator))
            {
                InnerOperatorCalculated = points;
            }
            else
            {
                //var typeOperation = item.Operator.ToLower();
                InnerOperatorCalculated = _engineCalculator.SingleParameterCalculationAxis(@operator, @operator, points);

                if (InnerOperatorCalculated == null)
                {
                    InnerOperatorCalculated = _engineCalculator.SingleParameterCalculationAxis(@operator, string.Empty, points);
                }
            }

            return InnerOperatorCalculated;
        }
    }

    private IEnumerable<BasicValue> CalculatAggregation(IEnumerable<BasicValue> points, string quantityAggregationFunction, int resolutionResolutionInSeconds, int sycResolutionInSeconds)
    {
        using (var mainLogger = PqbiStopwatch.Anchor($"{nameof(CalculatAggregation)}"))
        {
            if (sycResolutionInSeconds == resolutionResolutionInSeconds)
            {
                return points;
            }

            var groupByOperation = new GroupByFunctionInput(points, resolutionResolutionInSeconds, sycResolutionInSeconds);
            var groupByResponse = _engineCalculator.CalcGroupByAsync(groupByOperation);

            var data = groupByResponse;
            var calculated = new List<BasicValue>();

            foreach (var list in data)
            {
                var tmp = _engineCalculator.AggregationCalculationAsync(quantityAggregationFunction, list);
                calculated.Add(tmp);
            }

            return calculated;
        }
    }

    private IEnumerable<AxisValue> CalculateQuantityFunction(string quantityAggregationFunction, long startPeriodInSeconds, int resolutionInSeconds, IEnumerable<IEnumerable<BasicValue>> data)
    {
        var calculated = new List<AxisValue>();

        foreach (var list in data)
        {
            var tmp = _engineCalculator.AggregationCalculationAsync(quantityAggregationFunction, list);
            calculated.Add(new AxisValue { TimeStempInSeconds = (long)startPeriodInSeconds, Value = tmp.Value });
            startPeriodInSeconds += resolutionInSeconds;
        }

        return calculated;
    }

    private IEnumerable<AxisValue> CalculateQuantityAndNormalizeFunction(string quantityAggregationFunction, long startPeriodInSeconds, int resolutionInSeconds, IEnumerable<IEnumerable<BasicValue>> data, double nominalVal)
    {
        var calculated = new List<AxisValue>();

        foreach (var list in data)
        {
            var tmp = _engineCalculator.AggregationCalculationAsync(quantityAggregationFunction, list);
            tmp = new BasicValue(ParameterMatrix.Normalize(nominalVal, tmp.Value), tmp.DataValueStatus);
            calculated.Add(new AxisValue { TimeStempInSeconds = startPeriodInSeconds, Value = tmp.Value });
            startPeriodInSeconds += resolutionInSeconds;
        }

        return calculated;
    }

    private IEnumerable<AxisValue> CalculateQuantityAutoFunctionAsync(string quantityAggregationFunction, long startPeriodInSeconds, int resolutionInSeconds, IEnumerable<BasicValue> data)
    {
        var calculated = new List<AxisValue>();

        foreach (var item in data)
        {
            calculated.Add(new AxisValue { TimeStempInSeconds = startPeriodInSeconds, Value = item.Value });
            startPeriodInSeconds += resolutionInSeconds;
        }

        return calculated;
    }


    private IEnumerable<AxisValue> CalculateQuantityAutoFunctionAsync222(string quantityAggregationFunction, long startPeriodInSeconds, int resolutionInSeconds, IEnumerable<BasicValue> data)
    {
        var calculated = new List<AxisValue>();

        foreach (var item in data)
        {
            calculated.Add(new AxisValue { TimeStempInSeconds = startPeriodInSeconds, Value = item.Value });
            startPeriodInSeconds += resolutionInSeconds;
        }

        return calculated;
    }
}

