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
using PQS.Data.Common;
using System.Data;

namespace PQBI.Network.RestApi.EngineCalculation;

public interface IEngineCalculationService
{
    BasicValue AggregationFunctionsAsync(string aggregationFunc, IEnumerable<BasicValue> numbers);
    GraphParametersComponentDtoV3 FullCalculation(CustomParameterNodeCalculator node);
    IEnumerable<BasicValue> CalculatedInnerAlignment(CustomParameterNodeCalculator node, BaseParameterComponent item);
    IEnumerable<BasicValue> CalculateOutterAggregation(CustomParameterNodeCalculator node);
}

public class EngineCalculationService : IEngineCalculationService
{

    public enum InterpolationType
    {
        Avg,
        Max,
        Min
    }


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

    private GraphParametersComponentDtoV3 CalculateBaseParameter222(CustomParameterNodeCalculator node)
    {
        GraphParametersComponentDtoV3 response = null;

        var missings = new List<MissingBaseParameterInfo>();
        foreach (var baseParameter in node.BaseParameterComponents)
        {
            var baseParameterAxis = new List<AxisValue>();

            if (baseParameter.Axis is null)
            {
                //todo investigate should not happen
                baseParameter.SetRawData(new PQBIAxisDataEmpty(baseParameter.ComponentID, baseParameter.FeederId, baseParameter.BaseParameterName, PQZStatus.GENERAL_ERROR, baseParameter.DataUnitType), false, null);
            }

            if (baseParameter.Axis.PQZStatus == PQZStatus.OK)
            {
                foreach (var dataTimeStamp in baseParameter.Axis.DataTimeStamps)
                {
                    baseParameterAxis.Add(new AxisValue { Value = dataTimeStamp.Point, TimeStempInSeconds = dataTimeStamp.DateTime.ToDateTimeOffsetInSeconds() });
                }
            }
            else
            {
                missings.Add(new MissingBaseParameterInfo(baseParameter.BaseParameterName, baseParameter.Axis.PQZStatus, baseParameter.Axis.PQZStatus.ToString()));
            }


            response = BaseParameterPrepareBaseParameterResponse2222(baseParameter.ScadaParameterName, baseParameter.ComponentID, baseParameter.FeederId, baseParameter.DataUnitType, node.CustomParameterType, baseParameterAxis, missings);
        }

        return response;
    }

    public GraphParametersComponentDtoV3 BaseParameterPrepareBaseParameterResponse2222(string parameterName, Guid componentId, int? feaederId, DataUnitType dataUnitType, CustomParameterType customParameterType,
        IEnumerable<AxisValue> axises, IEnumerable<MissingBaseParameterInfo> missingBaseParameterInfos)
    {
        //var feederInfo = new FeederComponentInfo
        //{
        //    ComponentId = componentId,
        //    Id = int.Parse(feaederId),
        //};

        var tmp = new GraphParametersComponentDtoV3(string.Empty, [new FeederComponentInfo { ComponentId = componentId, Id = feaederId }], customParameterType.ToString(), dataUnitType, [parameterName], axises, missingBaseParameterInfos);
        return tmp;
    }

    public GraphParametersComponentDtoV3 FullCalculation(CustomParameterNodeCalculator node)
    {
        GraphParametersComponentDtoV3 response = null;

        switch (node.CustomParameterType)
        {
            case CustomParameterType.SPMC:

                var axises = CalculateExceptionOrSingleParameter(node);
                response = PrepareResponseForSPMCAndMPSC(node, axises);

                break;

            case CustomParameterType.MPSC:

                var multiParameterAxis = CalculateMultiAxisses2222(node);
                response = PrepareResponseForSPMCAndMPSC(node, multiParameterAxis);

                break;

            case CustomParameterType.BPCP:

                response = CalculateBaseParameter222(node);
                break;

            case CustomParameterType.Exception:
                var exceptionParameters = CalculateMultiAxisses2222(node);
                response = PrepareResponseForSPMCAndMPSC(node, exceptionParameters);


                break;

            default:
                throw new UserFriendlyException("Tree calculation case doesnt not exist");
        }

        return response;
    }

    private IEnumerable<AxisValue> CalculateExceptionOrSingleParameter(CustomParameterNodeCalculator node)
    {
        IEnumerable<AxisValue> response = null;
        using (var mainLogger = PqbiStopwatch.Anchor($"{nameof(CalculateExceptionOrSingleParameter)}"))
        {
            if (node.IsWidgetResolutionAuto)
            {
                response = SetAutoModeAsync(node, node.FinalAggregationMatrix.AggregatedCalculated);
            }
            else
            {
                var groupByOperation = new GroupByFunctionInput { Data = node.FinalAggregationMatrix.AggregatedCalculated, ResolutionInSeconds = node.WidgetResolutionAutoOrInSeconds, SyncInSeconds = node.CustomParameterResolutionRecalculatedInSeconds };
                var data = _engineCalculator.CalcGroupByAsync(groupByOperation);

                var resolutionInSeconds = node.WidgetResolutionAutoOrInSeconds;
                //var resolutionInSeconds = GroupByCalcFunction.ParseAndConvertToSecond(node.WidgetResolution);
                response = CalculateQuantityFunctionAsync(node.WidgetAggragationFunction, node.StartDate.ToDateTimeOffsetInSeconds(), resolutionInSeconds, data);
            }
        }

        return response;
    }

    public IEnumerable<AxisValue> SetAutoModeAsync(CustomParameterNodeCalculator node, IEnumerable<BasicValue> externalCalculated)
    {
        if (externalCalculated.IsCollectionEmpty())
        {
            return [];
        }

        var auto = new AutoCalcFunction();
        var data2 = auto.Calc(node.AutoWishListNumber, externalCalculated);
        var calculated = new List<AxisValue>();

        TimeSpan interval = TimeSpan.FromTicks(node.Duration.Ticks / data2.Count());
        var resolutionInSeconds = (int)interval.TotalSeconds;
        var response = CalculateQuantityAutoFunctionAsync(node.WidgetAggragationFunction, node.StartDate.ToDateTimeOffsetInSeconds(), resolutionInSeconds, data2);
        return response;
    }

    private IEnumerable<AxisValue> CalculateMultiAxisses2222(CustomParameterNodeCalculator node)
    {
        var res = new List<GraphParametersComponentDtoV3>();

        IEnumerable<AxisValue> response = null;

        if (node.IsWidgetResolutionAuto)
        {
            response = SetAutoModeAsync(node, node.FinalAggregationMatrix.AggregatedCalculated);
        }
        else
        {
            var resolutionInSeconds = node.WidgetResolutionAutoOrInSeconds;
            var groupByOperation = new GroupByFunctionInput
            {
                Data = node.FinalAggregationMatrix.AggregatedCalculated,
                ResolutionInSeconds = resolutionInSeconds,
                SyncInSeconds = node.CustomParameterResolutionRecalculatedInSeconds
            };

            var data = _engineCalculator.CalcGroupByAsync(groupByOperation);
            response = CalculateQuantityFunctionAsync(node.WidgetAggragationFunction, node.StartDate.ToDateTimeOffsetInSeconds(), resolutionInSeconds, data);
        }
        return response;
    }


    public GraphParametersComponentDtoV3 PrepareResponseForSPMCAndMPSC(CustomParameterNodeCalculator node, IEnumerable<AxisValue> axises)
    {
        var baseParameterAdditionalInfos = new List<MissingBaseParameterInfo>();
        foreach (var item in node.ParameterMatrix222.InvalidParameters)
        //foreach (var item in node.ParameterMatrix.InvalidParameters)
        {
            if (item.Value.Status != PQZStatus.OK)
            {
                if (item.Value.BaseParameters.IsCollectionEmpty())
                {
                    baseParameterAdditionalInfos.Add(new MissingBaseParameterInfo(item.Key.BaseParameterName, item.Value.Status, item.Value.Status.ToString()));
                }
            }
        }

        var feeders = new List<FeederComponentInfo>();
        foreach (var feeder in node.Feeders)
        {
            feeders.Add(feeder);
        }

        var result = new GraphParametersComponentDtoV3(node.CustomParameterName, feeders, node.CustomParameterType.ToString(), node.ParameterMatrix222.DataUnitType, [], axises, baseParameterAdditionalInfos);
        return result;
    }


    public IEnumerable<BasicValue> CalculateOutterAggregation(CustomParameterNodeCalculator node)
    {
        IEnumerable<BasicValue> calculated = null;
        //var matrix = node.ParameterMatrix;

        node.ParameterMatrix222.DataUnitType = node.GetDataType();
        //matrix.DataUnitType = node.GetDataType();

        using (var mainLogger = PqbiStopwatch.Anchor($"{nameof(CalculateOutterAggregation)}"))
        {
            //calculated = matrix.CalculateAndSetOutterAggregation(nums => _engineCalculator.AggregationCalculationAsync(node.InnerAggregationFunction, nums));
            calculated = node.ParameterMatrix222.AggregationCalculation;

            if (node.InnerCustomParameter is not null)
            {
                var innerCustomParameter = node.InnerCustomParameter;
                if (node.CustomParameterResolutionRecalculatedInSeconds != innerCustomParameter.Resolution)
                {
                    var groupByOperation = new GroupByFunctionInput { Data = calculated, ResolutionInSeconds = innerCustomParameter.Resolution, SyncInSeconds = node.CustomParameterResolutionRecalculatedInSeconds };
                    var data = _engineCalculator.CalcGroupByAsync(groupByOperation);
                    calculated = _engineCalculator.AggregationCalculation(data, innerCustomParameter.Quantity);
                }
            }

            var lists = new List<IEnumerable<BasicValue>>();
            foreach (var child in node.Children)
            {
                lists.Add(child.ParameterMatrix222.AggregationCalculation);
                //lists.Add(child.ParameterMatrix.AggregationCalculation);
            }

            node.AddFinalMaxtrixCalculation(lists);
            var Calculated2 = node.CalculateAggregationMatrix(nums => _engineCalculator.AggregationCalculationAsync(node.InnerAggregationFunction, nums));
            calculated = Calculated2;
        }

        return calculated;
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

            var groupByOperation = new GroupByFunctionInput { Data = points, ResolutionInSeconds = resolutionResolutionInSeconds, SyncInSeconds = sycResolutionInSeconds };
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

    private IEnumerable<AxisValue> CalculateQuantityFunctionAsync(string quantityAggregationFunction, long startPeriodInSeconds, int resolutionInSeconds, IEnumerable<IEnumerable<BasicValue>> data)
    {
        var calculated = new List<AxisValue>();

        foreach (var list in data)
        {
            var tmp = _engineCalculator.AggregationCalculationAsync(quantityAggregationFunction, list);
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
}

