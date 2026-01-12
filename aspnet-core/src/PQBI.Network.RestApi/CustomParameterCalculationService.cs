using Abp.Domain.Repositories;
using Abp.Runtime.Caching;
using Abp.UI;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PQBI.CalculationEngine;
using PQBI.CalculationEngine.Functions;
using PQBI.CalculationEngine.Matrix;
using PQBI.Infrastructure;
using PQBI.Infrastructure.Extensions;
using PQBI.Infrastructure.Lockers;
using PQBI.Network.Base;
using PQBI.Network.RestApi.EngineCalculation;
using PQBI.PQS;
using PQBI.PQS.Cache.Calculation;
using PQBI.PQS.CalcEngine;
using PQBI.Requests;
using PQBI.Tenants.Dashboard.Dto;
using PQS.CommonUI.Data;
using PQS.CommonUI.Utils;
using PQS.Data.Common;
using PQS.Data.Common.Extensions;
using PQS.Data.Common.Units;
using PQS.Data.Common.Values;
using PQS.Data.Configurations;
using PQS.Data.Configurations.Enums;
using PQS.Data.Configurations.SystemElectricalMapping;
using PQS.Data.Configurations.Utilities;
using PQS.Data.Events;
using PQS.Data.Events.Enums;
using PQS.Data.Events.Filters;
using PQS.Data.Measurements;
using PQS.Data.Measurements.Enums;
using PQS.Data.RecordsContainer;
using PQS.Data.RecordsContainer.Records;
using PQS.PQZxml;
using PQZTimeFormat;
using System.Globalization;
using TimeZoneConverter;
using SelectorFunc = System.Func<
    System.Collections.Generic.IEnumerable<PQBI.PQS.CalcEngine.FeederComponentInfo>,
    bool,
    System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<PQBI.Network.RestApi.EngineCalculation.CustomParameterNodeCalculator>>>;
using Task = System.Threading.Tasks.Task;


namespace PQBI.Network.RestApi
{

    public class TrendConfig : PQSConfig<TrendConfig>
    {
        public int AmountBatchSendToScada { get; set; }
    }


    public interface ICustomParameterCalculationService
    {
        static string Alias = IPQSServiceBase.Alias;
        //Task<CalculationDto> CalculateTrendChartAsync(string url, string session, TrendCalcRequest222 input);
        Task<TrendResponse> CalculateTrendChartAsync(string url, string session, TrendCalcRequest input);
        Task<TableWidgetResponse> CalculateTableAsync(string url, string session, TableWidgetRequest input);
        Task<BarChartResponse> CalculateBarChartAsync(string url, string session, BarChartRequest input, List<SubGroup> subgroups);
        Task<TableWidgetResponse> CalculateCardAsync(string url, string session, TableWidgetRequest input);
        Task<TableWidgetResponse> CalculateGaugeAsync(string url, string session, TableWidgetRequest input);
    }

    public class CustomParameterCalculationService : PQSRestApiServiceBase, ICustomParameterCalculationService
    {
        private const short MAX_NUM_BARS = 40;

        private readonly IFunctionEngine _functionEngine;
        private readonly IEngineCalculationService _engineControllerService;
        private readonly IRepository<CustomParameters.CustomParameter> _customParameterRepository;
        private readonly ICacheManager _cacheManager;


        //Refactor!!!!!!!!!!!!!!!!!!!
        private readonly object _customerParameterLocker;
        private TrendConfig _config;

        public CustomParameterCalculationService(ILogger<PQSComponentOperationService> logger,
            IOptions<TrendConfig> config,

            IHttpClientFactory httpClientFactory,
            IPQZBinaryWriterWrapper pQZBinaryWriterCore,
            IPQSenderHelper pQSenderHelper,
            IFunctionEngine functionEngine,
            IEngineCalculationService engineControllerService,
            IRepository<PQBI.CustomParameters.CustomParameter> customParameterRepository,
            ICacheManager cacheManager

            ) : base(httpClientFactory, pQZBinaryWriterCore, pQSenderHelper, logger)
        {
            _functionEngine = functionEngine;
            _engineControllerService = engineControllerService;
            _customParameterRepository = customParameterRepository;
            _cacheManager = cacheManager;
            _customerParameterLocker = new object();
            _config = config.Value;
        }

        protected override string ClientAlias => ICustomParameterCalculationService.Alias;

        private IEnumerable<TrendParameter> GetParameterBundle(TrendCalcRequest input)
        {
            var parameters = new List<TrendParameter>();

            var multiCustomParameter = new Dictionary<int, (TrendParameter TrendParam, HashSet<FeederComponentInfo> FeederHashSet)>();

            foreach (var @param in input.Parameters)
            {
                TrendWidgetParameterType tmpType = CalculationStaticTypes.GetCustomParameterTrendType(@param.Type);
                if (tmpType == TrendWidgetParameterType.CustomParameter)
                {
                    var customParameter = GetCustomParameter(param.CustomData.Id);

                    if (customParameter == null)
                    {
                        continue;
                    }

                    var customParameterType = CalculationStaticTypes.GetCustomParameterType(customParameter.Type);
                    if (customParameterType == CustomParameterType.MPSC)
                    {
                        if (multiCustomParameter.TryGetValue(param.CustomData.Id, out var val))
                        {
                            val.FeederHashSet.Add(param.Feeders.FirstOrDefault());
                        }
                        else
                        {
                            var feeders = new HashSet<FeederComponentInfo> { param.Feeders.First() };
                            //var newParam = @param.CustomData with { };
                            multiCustomParameter[param.CustomData.Id] = (param, feeders);
                        }
                    }
                    else
                    {
                        parameters.Add(param);
                    }
                }
                else
                {
                    parameters.Add(param);
                }
            }

            foreach (var item in multiCustomParameter)
            {
                var customDataBundle = item.Value;

                var trendCustomWidget = new TrendCustomWidgetData
                {
                    Id = customDataBundle.TrendParam.CustomData.Id,
                    IgnoreAlignment = customDataBundle.TrendParam.CustomData.IgnoreAlignment,
                    Quantity = customDataBundle.TrendParam.CustomData.Quantity
                };

                var newTrendPrameter = new TrendParameter
                {
                    CustomData = trendCustomWidget,
                    Feeders = customDataBundle.FeederHashSet.ToList(),
                    Type = customDataBundle.TrendParam.Type
                };

                parameters.Add(newTrendPrameter);
            }


            return parameters;
        }

        private void SetAutoResolution(BaseParameter baseParameter, TrendCalcRequest input)
        {
            var syncStr = string.Empty;
            if (input.IsAutoResolution)
            //if (AutoCalcFunction.TryExtracMaxPoints(input.Resolution, out var maxPoints))
            {

                var period = input.EndDate - input.StartDate;
                var periodInSeconds = (double)period.TotalSeconds / input.ResolutionInSeconds;
                //var paramSync = SyncInterval.GetSyncEnum(periodInSeconds);
                //syncStr = paramSync.ToString();

                baseParameter.Resolution = (int)periodInSeconds;
                //baseParameter.Resolution = syncStr;
            }
            else
            {
                baseParameter.Resolution = input.ResolutionInSeconds;
                //baseParameter.Resolution = $"IS{input.ResolutionInSeconds}SEC";
                //baseParameter.Resolution = input.Resolution;
            }
        }

        private int GetTrendResolutionInSec(TrendCalcRequest input, int resolutionInSeconds, DateTime startDate, DateTime endDate)
        {
            var syncStr = string.Empty;
            if (input.IsAutoResolution)
            //if (AutoCalcFunction.TryExtracMaxPoints(input.Resolution, out var maxPoints))
            {
                double periodInSeconds = 0;
                var period = endDate - startDate;
                if (resolutionInSeconds != 0)
                    periodInSeconds = (double)period.TotalSeconds / resolutionInSeconds;
                //var paramSync = SyncInterval.GetSyncEnum(periodInSeconds);
                //syncStr = paramSync.ToString();

                return (int)periodInSeconds;
                //baseParameter.Resolution = syncStr;
            }
            else
            {
                return resolutionInSeconds;
                //baseParameter.Resolution = $"IS{input.ResolutionInSeconds}SEC";
                //baseParameter.Resolution = input.Resolution;
            }
        }


        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        public class FatherTreeItem
        {
            public FatherTreeItem Father { get; set; } = null;
            public List<FatherTreeItem> Children { get; set; } = new List<FatherTreeItem>();

        }

        private async Task<CustomParameterNodeCalculator> AssembleCustomParameterTree(
              int customParameterId,
              string url,
              string session,
              DateTime start,
              DateTime end,
              //DateTime startLocal,
              //DateTime endLocal,
              Infrastructure.TimeZone usertimezoneinfo,
              int widgetResolutionInSeconds,
              bool isAutoResolution,
              string parameterQuantity,
              IEnumerable<FeederComponentInfo> feeders,
              IEnumerable<(DateTime barStart, DateTime barEnd)> barTimeRangeList,
              bool isTagCalc,
              AdvancedSettings? advancedSettings = null)
        {
            async Task<CustomParameterNodeCalculator> BuildAsync(int id, IEnumerable<FeederComponentInfo> feeders, InnerCustomParameter innerCustomParameter)
            {
                var customParameter = GetCustomParameter(id);
                if (customParameter == null)
                    throw new InvalidOperationException($"CustomParameter {id} not found");

                // parse child specs
                var innerSpecs = JsonConvert
                    .DeserializeObject<InnerCustomParameter[]>(
                        customParameter.InnerCustomParameters ?? "[]"
                    ) ?? Array.Empty<InnerCustomParameter>();
                var firstInner = innerSpecs.FirstOrDefault();

                bool isIgnoreAligningFunction = false;
                string outerAggregationFunction = customParameter.AggregationFunction;

                if (isTagCalc && advancedSettings != null && id == customParameterId)
                {
                    isIgnoreAligningFunction = advancedSettings.IsIgnoreAligningFunction;

                    outerAggregationFunction = string.IsNullOrEmpty(advancedSettings.ReplaceOuterAggregationWith) ? outerAggregationFunction : advancedSettings.ReplaceOuterAggregationWith;
                }

                var prmType = CalculationStaticTypes.GetCustomParameterType(customParameter.Type);

                // construct this node
                var node = new CustomParameterNodeCalculator(
                    prmType,
                    customParameter.ResolutionInSeconds,
                    isAutoResolution,
                    outerAggregationFunction,
                    start,
                    end,
                    widgetResolutionInSeconds,
                    parameterQuantity,
                    barTimeRangeList,
                    false,
                    customParameter.Name,
                    innerCustomParameter,
                    advancedSettings
                );

                // 1️⃣ recurse into children
                foreach (var childSpec in innerSpecs)
                {
                    var childNode = await BuildAsync(childSpec.CustomParameterId, feeders, childSpec);
                    node.Children.Add(childNode);
                }

                // 2️⃣ once all children are ready, do this node’s own calculations

                // 2a) inner-aggregation (fetch & aggregate base-parameters)
                var baseParams = JsonConvert
                    .DeserializeObject<BaseParameter[]>(
                        customParameter.CustomBaseDataList ?? "[]"
                    ) ?? Array.Empty<BaseParameter>();

                if (prmType == CustomParameterType.MPSC)
                {
                    foreach (var feed in feeders)
                    {
                        await CalcInnerAndOuter(url, session, start, end, usertimezoneinfo, advancedSettings, isIgnoreAligningFunction, node, baseParams, [feed]);
                    }
                }
                else
                {
                    await CalcInnerAndOuter(url, session, start, end, usertimezoneinfo, advancedSettings, isIgnoreAligningFunction, node, baseParams, feeders);
                }
                node.AddFinalMatrixCalculation();

                //// 2c) final leaf vs. internal call
                //if (innerSpecs.Any())
                //    _engineControllerService.AddFinalMaxtrixCalculationWithChildren(node);
                //else
                //    _engineControllerService.CalculateFinalMatrixChildless(node);

                return node;
            }

            return await BuildAsync(customParameterId, feeders, null);

            //var customParameter = GetCustomParameter(customParameterId);
            //CustomParameterType customParameterType = CalculationStaticTypes.GetCustomParameterType(customParameter.Type);
            //if (customParameterType == CustomParameterType.MPSC)
            //{
            //    List<CustomParameterNodeCalculator> customParameterNodeCalculatorList = new List<CustomParameterNodeCalculator>();
            //    foreach (var feeder in feeders)
            //    {
            //        CustomParameterNodeCalculator nCalculator = await BuildAsync(customParameterId, [feeder], null);
            //        customParameterNodeCalculatorList.Add(nCalculator);
            //    }
            //    CustomParameterNodeCalculator nodeCalculator = customParameterNodeCalculatorList.First();
            //    for (int i = 1; i < customParameterNodeCalculatorList.Count; i++)
            //    {
            //        nodeCalculator.FinalAggregationMatrixes.AddRange(customParameterNodeCalculatorList[i].FinalAggregationMatrixes);
            //        nodeCalculator.ParameterMatrixes.AddRange(customParameterNodeCalculatorList[i].ParameterMatrixes);
            //    }
            //    return nodeCalculator;
            //}
            //else
            //    return await BuildAsync(customParameterId, feeders, null);
        }

        private async Task CalcInnerAndOuter(string url, string session, DateTime start, DateTime end, Infrastructure.TimeZone usertimezoneinfo, AdvancedSettings? advancedSettings, bool isIgnoreAligningFunction, CustomParameterNodeCalculator node, BaseParameter[] baseParams, IEnumerable<FeederComponentInfo> feeders)
        {
            var reqs = baseParams.CreateBaseParameterComponents(feeders);
            var prmMatrix = await SetAndCalculateInnerAggregation(url, session, start, end, usertimezoneinfo, node, reqs, advancedSettings);

            // 2b) outer-aggregation
            node.CalculatedParameterOuterMatrixAndAggregation(prmMatrix, isIgnoreAligningFunction);
        }




        //private async Task<CustomParameterNodeCalculator> AssembleCustomParameterTree(int customParameterId, string url, string session, DateTime start, DateTime end, int widgetResolutionInSeconds, bool isAutoResolution, string parameterQuantity, IEnumerable<FeederComponentInfo> feeders)
        //{
        //    CustomParameterNodeCalculator rootResult = null;

        //    var stack = new Stack<int>();
        //    stack.Push(customParameterId);

        //    var reverseTree = new Stack<CustomParameterNodeCalculator>();

        //    var nodes = new Dictionary<int, CustomParameterNodeCalculator>();
        //    var parentChildMap = new Dictionary<int, List<int>>();

        //    //var lastCustomParameterId = customParameterId;

        //    while (stack.Count > 0)
        //    {
        //        var currentId = stack.Pop();
        //        //var currentId = lastCustomParameterId = stack.Pop();
        //        var customParameter = GetCustomParameter(currentId);

        //        if (customParameter == null)
        //            continue;

        //        var customParameterType = CalculationStaticTypes.GetCustomParameterType(customParameter.Type);

        //        var nextInnerCustomParameters = JsonConvert.DeserializeObject<InnerCustomParameter[]>(customParameter.InnerCustomParameters ?? string.Empty) ?? [];
        //        var nextInnerCustomParameter = nextInnerCustomParameters.FirstOrDefault();

        //        var node = new CustomParameterNodeCalculator(customParameterType, customParameter.ResolutionInSeconds, isAutoResolution, customParameter.AggregationFunction, start, end, widgetResolutionInSeconds, parameterQuantity, customParameter.Name, nextInnerCustomParameter);

        //        if (rootResult is null)
        //        {
        //            rootResult = node;
        //        }

        //        nodes[currentId] = node;

        //        var parameterList = JsonConvert.DeserializeObject<BaseParameter[]>(customParameter.CustomBaseDataList ?? string.Empty) ?? [];

        //        if (node.CustomParameterType == CustomParameterType.MPSC)
        //        {
        //            foreach (var feeder in feeders)
        //            {
        //                var baseParameterRequests = parameterList.CreateBaseParameterComponents([feeder]);
        //                await SetAndCalculateInnerAggregation(url, session, start, end, node, baseParameterRequests);
        //            }
        //        }
        //        else
        //        {
        //            var baseParameterRequests = parameterList.CreateBaseParameterComponents(feeders);
        //            await SetAndCalculateInnerAggregation(url, session, start, end, node, baseParameterRequests);
        //        }

        //        node.CalculatedParameterOuterMatrixAndAggregation();

        //        if (nextInnerCustomParameters.IsCollectionExists())
        //        {
        //            reverseTree.Push(node);

        //            parentChildMap[currentId] = new List<int>();
        //            foreach (var child in nextInnerCustomParameters)
        //            {
        //                stack.Push(child.CustomParameterId);
        //                parentChildMap[currentId].Add(child.CustomParameterId);
        //            }
        //        }
        //        else
        //        {
        //            _engineControllerService.CalculateFinalMatrixChildless(node);
        //        }
        //    }

        //    foreach (var keyValue in parentChildMap)
        //    {
        //        var parentId = keyValue.Key;
        //        var parentNode = nodes[parentId];

        //        foreach (var childId in keyValue.Value)
        //        {
        //            if (nodes.TryGetValue(childId, out var childNode))
        //            {
        //                parentNode.Children.Add(childNode);
        //            }
        //        }
        //    }

        //    while (reverseTree.Count > 0)
        //    {
        //        var node = reverseTree.Pop();
        //        _engineControllerService.AddFinalMaxtrixCalculationWithChildren(node);
        //    }

        //    return rootResult;
        //}

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------


        private async Task<ParameterMatrix> SetAndCalculateInnerAggregation(string url, string session, DateTime start, DateTime end, Infrastructure.TimeZone userTimeZone, CustomParameterNodeCalculator node, IEnumerable<BaseParameterComponent> ptr, AdvancedSettings? advancedSettings)
        {
            node.PopulateWithBaseParameterComponents(ptr);
            await SendingAndStoringDataAsync(url, session, start, end, userTimeZone, (false, null), ptr, advancedSettings?.FiltersGroup, false, 0);
            return node.CalculatedInnerAlignment(ptr, advancedSettings);
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        #region Trend

        public async Task<TrendResponse> CalculateTrendChartAsync(string url, string session, TrendCalcRequest input)
        {
            var response = new TrendResponse();
            var calculationDataItems = new PqbiSafeEntityLockerSlim<List<CalculatedDataItem>>([]);
            var timeStamps = new PqbiSafeEntityLockerSlim<List<long>>([]);

            (DateTime startDate, DateTime endDate) = input.NormalizeDatesToUtc();
            Infrastructure.TimeZone userTimeZone = new Infrastructure.TimeZone(input.UserTimeZoneID, input.UserTimeZone);

            int resolutionInSeconds = 1334;
            if (input.ResolutionInSeconds == 0 || input.ResolutionInSeconds == null)
                input.ResolutionInSeconds = resolutionInSeconds;
            else
                resolutionInSeconds = input.ResolutionInSeconds.Value;

            if (string.IsNullOrEmpty(session))
            {
                throw new UserFriendlyException(nameof(session), "Cant be null");
            }

            IEnumerable<TrendParameter> parameters = GetParameterBundle(input);

            List<BarParameter> eventColWidgetTableList = new List<BarParameter>();
            List<BarParameter> otherColWidgetTableList = new List<BarParameter>();
            Dictionary<Guid, Dictionary<FiltersGroup, HashSet<BaseParameterComponent>>> baseParametersHashSet = new Dictionary<Guid, Dictionary<FiltersGroup, HashSet<BaseParameterComponent>>>();

            using (var mainLogger = PqbiStopwatch.AnchorAsync($"Trender - {input.WidgetName} {nameof(CalculateTrendChartAsync)}", Logger))
            {
                int intervalResolutionInSec = GetTrendResolutionInSec(input, resolutionInSeconds, startDate, endDate);

                foreach (var parameter in parameters)
                {
                    PreparePrmMapForTrendReq(startDate, endDate, parameter.Feeders, intervalResolutionInSec, baseParametersHashSet, parameter);
                }


                //int barsPerParam = Math.Max(1, MAX_NUM_BARS / numOfPrms);
                //(var bucketSize, IEnumerable<(DateTime barStart, DateTime barEnd)>? barTimeRangeList) = CalendarBuckets.ChooseBucket(startDate, endDate, barsPerParam);


                //(DateTime startSyncedUtc, DateTime endSyncedUtc) = ScadaRequestPlanner.Plan(input.StartDate, input.EndDate, bucketSize);
                //IEnumerable<(DateTime barStart, DateTime barEnd)> barTimeRangeList = CalendarBuckets.GenerateBuckets(startSyncedUtc, endSyncedUtc, bucketSize);




                int sampleIntervalInSec = intervalResolutionInSec;
                IntervalSynchronized syncEnum = IntervalSynchronized.ISX;
                IEnumerable<(DateTime Start, DateTime End)>? buckets = null;
             

                if (!input.IsAutoResolution)
                {
                    DateTime startDateSyncedLocal = IntervalSynchronizedAlignment.AlignFloor(input.StartDate, input.SelectedResolution);
                    DateTime endDateSyncedLocal = IntervalSynchronizedAlignment.AlignCeil(input.EndDate, input.SelectedResolution);

                    startDate = IntervalSynchronizedAlignment.AlignFloor(startDate, input.SelectedResolution);
                    endDate = IntervalSynchronizedAlignment.AlignCeil(endDate, input.SelectedResolution);

                    syncEnum = input.SelectedResolution;
                    int syncresolutionInSec = (int)new SyncInterval(syncEnum).TimeIntervalInSec;

                    if (intervalResolutionInSec != syncresolutionInSec)
                    {
                        float numOFSyncIntervals = ((float)intervalResolutionInSec) / syncresolutionInSec;
                        sampleIntervalInSec = syncresolutionInSec;

                        buckets = IntervalSynchronizedAlignment.GenerateBuckets(startDateSyncedLocal, endDateSyncedLocal, syncEnum, numOFSyncIntervals);
                    }

                    foreach (var item in baseParametersHashSet)
                    {
                        Dictionary<FiltersGroup, HashSet<BaseParameterComponent>> filterToPrmSetMap = item.Value;

                        foreach (var filterToPrmSet in filterToPrmSetMap)
                        {
                            foreach (BaseParameterComponent baseParameterComponent in filterToPrmSet.Value)
                            {
                                baseParameterComponent.MeasurementParameter.SyncInterval = new SyncInterval(syncEnum);
                            }
                        }
                    }
                }

                await LoadPrmToCache(url, session, startDate, endDate, userTimeZone, baseParametersHashSet, input.IsRealTime);

                //var list = new List<Task>();
                foreach (TrendParameter parameter in parameters)
                {
                    //var task = Task.Run(async () =>
                    {
                        using (var subLogger = mainLogger.CreateSubLogger("Parameter Calculation"))
                        {

                            var graphes = await CalculateTrendChartIntristicAsync(url, session, input, startDate, endDate, startDate, endDate, userTimeZone, parameter, intervalResolutionInSec, buckets, sampleIntervalInSec, syncEnum);

                            //var graphes = await CalculateTrendChartIntristicAsync(url, session, input, parameter);
                            foreach (var graph in graphes)
                            {
                                var data = new CalculatedDataItem
                                {
                                    ParameterType = graph.RequestType,
                                    Feeders = graph.Feeders.ToList()
                                };

                                if (graph.MissingInformation.IsCollectionExists())
                                {
                                    data.MissingInformation.AddRange(graph.MissingInformation);
                                }


                                if (graph.CustomParameterName.IsStringExists())
                                {
                                    data.ParameterName = graph.CustomParameterName;
                                }
                                else
                                {
                                    data.ParameterName = graph.CustomParameterName; // graph.ParameterNames.FirstOrDefault() ?? "xxx";
                                }

                                foreach (var item in graph.Data)
                                {
                                    data.Data.Add(item.Value);
                                    data.Status.Add(item.DataValueStatus);
                                }

                                await calculationDataItems.DoLockAsync(list => list.Add(data));


                                await timeStamps.DoLockAsync(list =>
                                {
                                    if (list.IsCollectionEmpty())
                                    {
                                        list.AddRange(graph.Data.Select(x => x.TimeStempInSeconds));
                                    }
                                });
                            }
                            //result.AddRange(res);
                        }
                    }
                    //);

                    //list.Add(task);
                }

                //await Task.WhenAll(list);

                response.Data = calculationDataItems.Value;
                response.TimeStamps = timeStamps.Value;
            }

            return response;
            //return new CalculationDto(result, true, string.Empty);
        }

        private async Task<IEnumerable<GraphParametersComponentDtoV3>> CalculateTrendChartIntristicAsync(string url, string session, TrendCalcRequest input, DateTime startDate, DateTime endDate, DateTime startLocal,
              DateTime endLocal, Infrastructure.TimeZone timezoneinfo, TrendParameter parameter, int intervalRessolutionInSec, IEnumerable<(DateTime Start, DateTime End)>? buckets, int sampleIntervalInSec, IntervalSynchronized syncEnum)
        {
            var result = new List<GraphParametersComponentDtoV3>();

            TrendWidgetParameterType customParameterType = CalculationStaticTypes.GetCustomParameterTrendType(parameter.Type);

            switch (customParameterType)
            {
                case TrendWidgetParameterType.CustomParameter:

                    TrendCustomWidgetData customWidgetData = parameter.CustomData;
                    var customParameterId = customWidgetData.Id;

                    var calculationNode = await AssembleCustomParameterTree(customParameterId, url, session, startDate, endDate, new Infrastructure.TimeZone(input.UserTimeZoneID, input.UserTimeZone), intervalRessolutionInSec, input.IsAutoResolution, parameter.CustomData.Quantity, parameter.Feeders, buckets, false);
                    calculationNode.IsTrend = true;
                    var results = _engineControllerService.RootCalculation(calculationNode);
                    result.AddRange(results);

                    break;

                case TrendWidgetParameterType.BaseParameter:
                    var baseData = parameter.BaseData;
                    //TrendBaseData baseData = parameter.BaseData;
                    var baseParameter = baseData.ToBaseParameter();
                    baseParameter.IntervalSync = syncEnum;
                    baseParameter.Resolution = sampleIntervalInSec;

                    //SetAutoResolution(baseParameter, input);// input.StartDate, input.EndDate, input.Resolution,input.ResolutionInSeconds, input.IsAutoResolution);

                    var root = new CustomParameterNodeCalculator(CustomParameterType.BPCP, -1, input.IsAutoResolution, string.Empty, startDate, endDate, intervalRessolutionInSec, baseParameter.Quantity, buckets);
                    root.IsTrend = true;
                    var baseParameterRequests = baseParameter.CreateBaseParameterComponents(parameter.Feeders);

                    root.PopulateWithBaseParameterComponents(baseParameterRequests);

                    //SelectAssemble(root, parameter.Feeders);

                    await SendingAndStoringDataAsync(url, session, startDate, endDate, timezoneinfo, (false, null), root.BaseParameterComponents, null, input.IsRealTime, input.RefreshRateInSeconds.Value);
                    //var baseParameterGraph = _engineControllerService.FullCalculation(root);
                    //result.Add(baseParameterGraph);


                    var baseParameterGraphes = _engineControllerService.RootCalculation(root);
                    result.AddRange(baseParameterGraphes);

                    return result;


                case TrendWidgetParameterType.Exception:

                    TrendCustomWidgetData exceptionCustomWidgetData = parameter.CustomData;
                    var exceptionCustomParameterId = exceptionCustomWidgetData.Id;

                    var exceptionnode = await AssembleCustomParameterTree(exceptionCustomParameterId, url, session, startDate, endDate, new Infrastructure.TimeZone(input.UserTimeZoneID, input.UserTimeZone), intervalRessolutionInSec, input.IsAutoResolution, parameter.CustomData.Quantity, [], buckets, false);
                    exceptionnode.IsTrend = true;
                    //foreach (var exceptionnode in exceptionNodes)
                    {
                        var exceptionGraphes = _engineControllerService.RootCalculation(exceptionnode);
                        result.AddRange(exceptionGraphes);
                    }

                    break;

                default:
                    break;
            }
            return result;
        }

        #endregion

        #region Bar Chart

        public async Task<BarChartResponse> CalculateBarChartAsync(string url, string session, BarChartRequest input, List<SubGroup> subgroups)
        {
            var responseItems = new List<TableWidgetResponseItem>();
            var paramComponents = new List<BaseParameterComponent>();

            List<BarParameter> eventColWidgetTableList = new List<BarParameter>();
            List<BarParameter> otherColWidgetTableList = new List<BarParameter>();
            Dictionary<Guid, Dictionary<FiltersGroup, HashSet<BaseParameterComponent>>> baseParametersHashSet = new Dictionary<Guid, Dictionary<FiltersGroup, HashSet<BaseParameterComponent>>>();

            (DateTime startDate, DateTime endDate) = input.NormalizeDatesToUtc();

            Infrastructure.TimeZone userTimeZone = new Infrastructure.TimeZone(input.UserTimeZoneID, input.UserTimeZone);
            List<BarGroup>? barGroups = null;
            DataUnitType dataUnitType = null;
            switch (input.Category.Type)
            {
                case DimensionType.Dates:
                    switch (input.SeriesBy.Type)
                    {
                        case DimensionType.Parameters:
                        case DimensionType.Feeders:
                            {
                                (TimeBucket timeBucket, IntervalSynchronized intervalSynchronized, IEnumerable<(DateTime barStart, DateTime barEnd)>? barTimeRangeList, int resolutionInSec, DateTime startTimeUTCSynced, DateTime endTimeUTCSynced) = await GetDatesForBars(url, session, input, userTimeZone, startDate, endDate, eventColWidgetTableList, otherColWidgetTableList, baseParametersHashSet);

                                //input.StartDate = startTimeUTCSynced;
                                //input.EndDate = endTimeUTCSynced;

                                barGroups = barTimeRangeList?
                                    .Select(r => new BarGroup(
                                        Category: FormatCategory(r.barStart, timeBucket),
                                        Bars: new List<BarItem>()))
                                    .ToList();

                                int numOfFeeders = input.Feeders.Count;
                                int numOfDates = barTimeRangeList.Count();

                                List<EventParameterDto> dtoList = eventColWidgetTableList
                                  .Select(c => new EventParameterDto
                                  {
                                      TableEvent = c.TableEvent,
                                      Normalize = NormalizeEnum.NO,
                                      NormalValue = 0,
                                      ParameterName = c.ParameterName,
                                      ReplaceAggregationWith = string.Empty
                                  })
                                  .ToList();
                                Guid sessionID = Guid.Parse(session);

                                List<TableWidgetResponseItem> feedersTableWidgetResponseItemList = await PopulateEventsValForFeedersInTable(url, dtoList, sessionID, new PQZDateTime(startTimeUTCSynced), new PQZDateTime(endTimeUTCSynced), input.Feeders, null!, null!, 0, false, barTimeRangeList);

                                int evIdx = 0;
                                int numOfEvPrms = dtoList.Count;
                                for (int barItemNum = 0; barItemNum < feedersTableWidgetResponseItemList.Count; barItemNum++)
                                {
                                    TableWidgetResponseItem eventsInfo = feedersTableWidgetResponseItemList[barItemNum];
                                    BarItem barItem = new BarItem(eventsInfo.ParameterName, eventsInfo.Calculated, eventsInfo.DataUnitType, eventsInfo.DataValueStatus);
                                    dataUnitType = eventsInfo.DataUnitType;
                                    int bucketIdx = evIdx % numOfDates;
                                    barGroups[bucketIdx].Bars.Add(barItem);
                                    evIdx++;
                                }

                                List<BarGroup> barGroupList = new List<BarGroup>();
                                for (int i = 0; i < otherColWidgetTableList.Count; i++)
                                {
                                    BarParameter barPrm = otherColWidgetTableList[i];
                                    try
                                    {
                                        List<BarItem> barItemList = null;
                                        TableWidgetParameterType widgetTableType = CalculationStaticTypes.GetTableWidgetParameterType(barPrm.ParameterType);

                                        //AdvancedSettings advancedSettings = new AdvancedSettings(parameter.NormalValue, parameter.Normalize, parameter.IsExcludeFlaggedData, parameter.ExcludeFlagged, parameter.IgnoreAligningFunction, parameter.ReplaceAggregationWith);

                                        switch (widgetTableType)
                                        {
                                            case TableWidgetParameterType.CustomParameter:
                                                var customParameterId = barPrm.CustomData.Id;
                                                var customParameter = GetCustomParameter(customParameterId);
                                                CustomParameterType customParameterType = CalculationStaticTypes.GetCustomParameterType(customParameter.Type);
                                                barItemList = (await CustomParameterCreateBarAsync(url, session, userTimeZone, input, barPrm,
                                                    startTimeUTCSynced, endTimeUTCSynced, barTimeRangeList, customParameterType, true)).ToList();
                                                break;

                                            case TableWidgetParameterType.BaseParameter:
                                                barItemList = (await BaseParameterCreateBarAsync(url, session, input, userTimeZone, startTimeUTCSynced, endTimeUTCSynced, barPrm, resolutionInSec, false, barTimeRangeList, intervalSynchronized)).ToList();
                                                break;

                                            default:
                                                throw new UserFriendlyException("TableWidgetParameterType Supports only BaseParameter");
                                        }

                                        for (int idx = 0; idx < barItemList.Count; idx++)
                                        {
                                            dataUnitType = barItemList[idx].DataUnitType;
                                            int bucketIdx = idx % numOfDates;
                                            barGroups[bucketIdx].Bars.Add(barItemList[idx]);
                                        }
                                    }
                                    catch (SessionExpiredException sessionExpiredException)
                                    {
                                        throw;
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.LogError(ex.Message);
                                        throw new UserFriendlyException($"{barPrm.ParameterName} - Failed [{ex.Message}] please rerun without it.");
                                    }

                                    //var buckets = GenerateBuckets(input.StartDate, input.EndDate, bucketSize).ToList();
                                }
                            }
                            break;
                    }
                    break;
                case DimensionType.Parameters:
                    switch (input.SeriesBy.Type)
                    {
                        case DimensionType.Dates:
                            {
                                (dataUnitType, barGroups) = await ParametersFeedersDatesBarChart(url, session, input, userTimeZone, startDate, endDate, eventColWidgetTableList, otherColWidgetTableList, baseParametersHashSet);
                            }
                            break;
                        case DimensionType.Feeders:
                            {
                                //barGroups = Enumerable.Range(0, input.Feeders.Count)
                                //         .Select(_ => new BarGroup(
                                //                     Category: null!,          // fill as needed
                                //                     Bars: new List<BarItem>()))
                                //         .ToList();

                                barGroups = new List<BarGroup>();
                                foreach (var parameter in input.BarPrmList)
                                {
                                    PreparePrmMapForReq(startDate, endDate, true, input.Feeders, eventColWidgetTableList, otherColWidgetTableList, baseParametersHashSet, parameter, input.RefreshRateInSeconds.Value);
                                }

                                //List<TableWidgetResponseItem> tableEventWidgetResponseItemList = null;
                                //if (eventColWidgetTableList.IsCollectionExists())
                                //{
                                //    tableEventWidgetResponseItemList = await WidgetTableEventCalculation(url, session, input, eventColWidgetTableList, input.StartDate, input.EndDate);
                                //    responseItems.AddRange(tableEventWidgetResponseItemList);
                                //}

                                int resInSec = await LoadPrmToCache(url, session, startDate, endDate, userTimeZone, baseParametersHashSet, input.IsRealTime);

                                List<EventParameterDto> dtoList = eventColWidgetTableList
                                       .Select(c => new EventParameterDto
                                       {
                                           TableEvent = c.TableEvent,
                                           Normalize = NormalizeEnum.NO,
                                           NormalValue = 0,
                                           ParameterName = c.ParameterName,
                                           ReplaceAggregationWith = string.Empty
                                       })
                                       .ToList();
                                Guid sessionID = Guid.Parse(session);

                                List<TableWidgetResponseItem> feedersTableWidgetResponseItemList = await PopulateEventsValForFeedersInTable(url, dtoList, sessionID, new PQZDateTime(startDate), new PQZDateTime(endDate), input.Feeders, null!, null!, 0, true);

                                int numOfEvPrms = dtoList.Count;
                                for (int i = 0; i < dtoList.Count; i++)
                                {
                                    barGroups.Add(new BarGroup(dtoList[i].ParameterName, new List<BarItem>()));
                                }
                                for (int barItemNum = 0; barItemNum < feedersTableWidgetResponseItemList.Count; barItemNum++)
                                {
                                    TableWidgetResponseItem eventsInfo = feedersTableWidgetResponseItemList[barItemNum];
                                    BarItem barItem = new BarItem(GetMsrPointName(input.Feeders[barItemNum]), eventsInfo.Calculated, eventsInfo.DataUnitType, eventsInfo.DataValueStatus);
                                    dataUnitType = barItem.DataUnitType;
                                    barGroups[barItemNum % numOfEvPrms].Bars.Add(barItem);
                                }

                                //if (baseParametersHashSet.IsCollectionExists())
                                //{
                                //    foreach (var keyAndValue in baseParametersHashSet)
                                //    {
                                //        var filterAndParameterComponents = keyAndValue.Value;
                                //        foreach (var item in filterAndParameterComponents)
                                //        {
                                //            FiltersGroup filterGroup = item.Key;
                                //            HashSet<BaseParameterComponent> prmCompSet = item.Value;
                                //            await SendingAndStoringDataAsync(url, session, input.StartDate, input.EndDate, (false, null), prmCompSet, filterGroup);
                                //        }
                                //    }
                                //}

                                using (var mainLogger = PqbiStopwatch.AnchorAsync($"Tablo - {input.WidgetName} {nameof(CalculateTableAsync)}", Logger))
                                {
                                    for (int i = 0; i < otherColWidgetTableList.Count; i++)
                                    {
                                        BarParameter barPrm = otherColWidgetTableList[i];
                                        try
                                        {
                                            List<BarItem> barItemList = null;
                                            using (var sub = mainLogger.CreateSubLogger(barPrm.ParameterName))
                                            {
                                                TableWidgetParameterType widgetTableType = CalculationStaticTypes.GetTableWidgetParameterType(barPrm.ParameterType);

                                                switch (widgetTableType)
                                                {
                                                    case TableWidgetParameterType.CustomParameter:

                                                        barItemList = (await CustomParameterCreateBarAsync(url, session, userTimeZone, input, barPrm, startDate, endDate, null, CustomParameterType.BPCP, false)).ToList();
                                                        break;

                                                    case TableWidgetParameterType.BaseParameter:

                                                        barItemList = (await BaseParameterCreateBarAsync(url, session, input, userTimeZone, startDate, endDate, barPrm, resInSec, true, isHideMsrPointName: true)).ToList();
                                                        break;

                                                    default:
                                                        throw new UserFriendlyException("TableWidgetParameterType Supports only BaseParameter");
                                                }
                                                string groupPrmName = barPrm.ParameterName;
                                                for (int feederIndex = 0; feederIndex < input.Feeders.Count; feederIndex++)
                                                {
                                                    groupPrmName = barItemList[0].SeriesName;
                                                    if (feederIndex < barItemList.Count)
                                                    {
                                                        dataUnitType = barItemList[feederIndex].DataUnitType;
                                                        barItemList[feederIndex].SeriesName = GetMsrPointName(input.Feeders[feederIndex]);
                                                    }
                                                }

                                                BarGroup barGroup = new BarGroup(groupPrmName, barItemList);
                                                barGroups.Add(barGroup);
                                            }
                                        }
                                        catch (SessionExpiredException sessionExpiredException)
                                        {
                                            throw;
                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.LogError(ex.Message);
                                            throw new UserFriendlyException($"{barPrm.ParameterName} - Failed [{ex.Message}] please rerun without it.");
                                        }
                                    }
                                }
                            }
                            break;
                        case DimensionType.CustomGroup:
                            {
                                (barGroups, dataUnitType) = await ParametersAndFeedersCustomGroupBarChart(url, session, input, userTimeZone, startDate, endDate, subgroups, eventColWidgetTableList, otherColWidgetTableList, baseParametersHashSet);
                            }
                            break;
                        default:
                            break;
                    }
                    break;
                case DimensionType.Feeders:
                    switch (input.SeriesBy.Type)
                    {
                        case DimensionType.Parameters:
                            {

                                //barGroups = new List<BarGroup>(input.Feeders.Count);

                                barGroups = input.Feeders
                                        .Select(f => new BarGroup(
                                            Category: GetMsrPointName(f),   // fall back to "" if Name is null
                                            Bars: new List<BarItem>()))
                                        .ToList();

                                foreach (var parameter in input.BarPrmList)
                                {
                                    PreparePrmMapForReq(startDate, endDate, true, input.Feeders, eventColWidgetTableList, otherColWidgetTableList, baseParametersHashSet, parameter, input.RefreshRateInSeconds.Value);
                                }

                                //List<TableWidgetResponseItem> tableEventWidgetResponseItemList = null;
                                //if (eventColWidgetTableList.IsCollectionExists())
                                //{
                                //    tableEventWidgetResponseItemList = await WidgetTableEventCalculation(url, session, input, eventColWidgetTableList, input.StartDate, input.EndDate);
                                //    responseItems.AddRange(tableEventWidgetResponseItemList);
                                //}

                                int resInSec = await LoadPrmToCache(url, session, startDate, endDate, userTimeZone, baseParametersHashSet, input.IsRealTime);
                                List<EventParameterDto> dtoList = eventColWidgetTableList
                                    .Select(c => new EventParameterDto
                                    {
                                        TableEvent = c.TableEvent,
                                        Normalize = NormalizeEnum.NO,
                                        NormalValue = 0,
                                        ParameterName = c.ParameterName,
                                        ReplaceAggregationWith = string.Empty
                                    })
                                    .ToList();
                                Guid sessionID = Guid.Parse(session);

                                int numOfEvPrms = dtoList.Count;
                                List<TableWidgetResponseItem> feedersTableWidgetResponseItemList = await PopulateEventsValForFeedersInTable(url, dtoList, sessionID, new PQZDateTime(startDate), new PQZDateTime(endDate), input.Feeders, null!, null!, 0, true);
                                int barGroupIndex = -1;
                                for (int barItemNum = 0; barItemNum < feedersTableWidgetResponseItemList.Count; barItemNum++)
                                {
                                    TableWidgetResponseItem eventsInfo = feedersTableWidgetResponseItemList[barItemNum];
                                    BarItem barItem = new BarItem(eventsInfo.ParameterName, eventsInfo.Calculated, eventsInfo.DataUnitType, eventsInfo.DataValueStatus);
                                    dataUnitType = barItem.DataUnitType;

                                    if (barItemNum % numOfEvPrms == 0)
                                        barGroupIndex++;
                                    barGroups[barGroupIndex].Bars.Add(barItem);
                                }

                                using (var mainLogger = PqbiStopwatch.AnchorAsync($"Tablo - {input.WidgetName} {nameof(CalculateTableAsync)}", Logger))
                                {
                                    for (int i = 0; i < otherColWidgetTableList.Count; i++)
                                    {
                                        BarParameter barPrm = otherColWidgetTableList[i];
                                        try
                                        {
                                            List<BarItem> barItemList = null;
                                            using (var sub = mainLogger.CreateSubLogger(barPrm.ParameterName))
                                            {
                                                TableWidgetParameterType widgetTableType = CalculationStaticTypes.GetTableWidgetParameterType(barPrm.ParameterType);


                                                //AdvancedSettings advancedSettings = new AdvancedSettings(parameter.NormalValue, parameter.Normalize, parameter.IsExcludeFlaggedData, parameter.ExcludeFlagged, parameter.IgnoreAligningFunction, parameter.ReplaceAggregationWith);

                                                switch (widgetTableType)
                                                {
                                                    case TableWidgetParameterType.CustomParameter:

                                                        barItemList = (await CustomParameterCreateBarAsync(url, session, userTimeZone, input, barPrm, startDate, endDate, null, CustomParameterType.MPSC, false)).ToList();
                                                        break;

                                                    case TableWidgetParameterType.BaseParameter:

                                                        barItemList = (await BaseParameterCreateBarAsync(url, session, input, userTimeZone, startDate, endDate, barPrm, isHideMsrPointName: true, resolutionInSec: resInSec, isExpectedSingleRes: true)).ToList();
                                                        break;

                                                    default:
                                                        throw new UserFriendlyException("TableWidgetParameterType Supports only BaseParameter");
                                                }
                                                for (int barItemNum = 0; barItemNum < barItemList.Count; barItemNum++)
                                                {
                                                    dataUnitType = barItemList[barItemNum].DataUnitType;
                                                    barGroups[barItemNum].Bars.Add(barItemList[barItemNum]);
                                                }
                                            }
                                        }
                                        catch (SessionExpiredException sessionExpiredException)
                                        {
                                            throw;
                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.LogError(ex.Message);
                                            throw new UserFriendlyException($"{barPrm.ParameterName} - Failed [{ex.Message}] please rerun without it.");
                                        }
                                    }
                                }
                            }
                            break;
                        case DimensionType.CustomGroup:
                            {
                                (barGroups, dataUnitType) = await ParametersAndFeedersCustomGroupBarChart(url, session, input, userTimeZone, startDate, endDate, subgroups, eventColWidgetTableList, otherColWidgetTableList, baseParametersHashSet);
                            }
                            break;
                        case DimensionType.Dates:
                            {
                                (dataUnitType, barGroups) = await ParametersFeedersDatesBarChart(url, session, input, userTimeZone, startDate, endDate, eventColWidgetTableList, otherColWidgetTableList, baseParametersHashSet);
                            }
                            break;
                        default:
                            break;
                    }
                    break;
                case DimensionType.CustomGroup:
                    switch (input.SeriesBy.Type)
                    {
                        case DimensionType.Parameters:
                        case DimensionType.Feeders:
                            {
                                (barGroups, dataUnitType) = await CustomGroupPrmAndFeedersBarChart(url, session, input, userTimeZone, startDate, endDate, subgroups, eventColWidgetTableList, otherColWidgetTableList, baseParametersHashSet);
                            }
                            break;
                    }
                    break;
                default:
                    break;
            }

            BarChartResponse barChartResponse = new BarChartResponse(dataUnitType, barGroups);

            return barChartResponse;
        }

        private async Task<(DataUnitType, List<BarGroup>)> ParametersFeedersDatesBarChart(string url, string session, BarChartRequest input, Infrastructure.TimeZone userTimeZone, DateTime startDate, DateTime endDate, List<BarParameter> eventColWidgetTableList, List<BarParameter> otherColWidgetTableList, Dictionary<Guid, Dictionary<FiltersGroup, HashSet<BaseParameterComponent>>> baseParametersHashSet)
        {
            List<BarGroup> barGroups = new List<BarGroup>();
            DataUnitType? dataUnitType = null;
            (TimeBucket timeBucket, IntervalSynchronized intervalSynchronized, IEnumerable<(DateTime barStart, DateTime barEnd)> barTimeRangeList, int resolutionInSec, DateTime startTimeUTCSynced, DateTime endTimeUTCSynced) = await GetDatesForBars(url, session, input, userTimeZone, startDate, endDate, eventColWidgetTableList, otherColWidgetTableList, baseParametersHashSet);
            int numOfFeeders = input.Feeders.Count;

            //input.StartDate = startTimeUTCSynced;
            //input.EndDate = endTimeUTCSynced;

            int numOfDates = barTimeRangeList.Count();
            List<BarItem>? barItemGroupList = null;

            Dictionary<int, string> timeRangeToDateFormatMap = new Dictionary<int, string>();
            List<(DateTime barStart, DateTime barEnd)> barTimeRanges = barTimeRangeList.ToList();
            for (int i = 0; i < barTimeRanges.Count; i++)
            {
                timeRangeToDateFormatMap.Add(i, FormatCategory(barTimeRanges[i].barStart, timeBucket));
            }

            List<EventParameterDto> dtoList = eventColWidgetTableList
            .Select(c => new EventParameterDto
            {
                TableEvent = c.TableEvent,
                Normalize = NormalizeEnum.NO,
                NormalValue = 0,
                ParameterName = c.ParameterName,
                ReplaceAggregationWith = string.Empty
            })
            .ToList();
            Guid sessionID = Guid.Parse(session);

            List<TableWidgetResponseItem> feedersTableWidgetResponseItemList = await PopulateEventsValForFeedersInTable(url, dtoList, sessionID, new PQZDateTime(startTimeUTCSynced), new PQZDateTime(endTimeUTCSynced), input.Feeders, null!, null!, 0, false, barTimeRangeList);

            int numOfEvPrms = dtoList.Count;
            for (int barItemNum = 0; barItemNum < feedersTableWidgetResponseItemList.Count; barItemNum++)
            {
                TableWidgetResponseItem eventsInfo = feedersTableWidgetResponseItemList[barItemNum];

                if (barItemNum % numOfDates == 0)
                {
                    barItemGroupList = new List<BarItem>();
                    BarGroup barGroup = new BarGroup(eventsInfo.ParameterName, barItemGroupList);
                    barGroups?.Add(barGroup);
                }
                BarItem barItem = new BarItem(timeRangeToDateFormatMap[barItemNum % numOfDates], eventsInfo.Calculated, eventsInfo.DataUnitType, eventsInfo.DataValueStatus);
                dataUnitType = eventsInfo.DataUnitType;
                barItemGroupList?.Add(barItem);
            }

            List<BarGroup> barGroupList = new List<BarGroup>();
            for (int i = 0; i < otherColWidgetTableList.Count; i++)
            {
                BarParameter barPrm = otherColWidgetTableList[i];
                try
                {
                    List<BarItem> barItemList = null;
                    TableWidgetParameterType widgetTableType = CalculationStaticTypes.GetTableWidgetParameterType(barPrm.ParameterType);

                    switch (widgetTableType)
                    {
                        case TableWidgetParameterType.CustomParameter:
                            var customParameterId = barPrm.CustomData.Id;
                            var customParameter = GetCustomParameter(customParameterId);
                            CustomParameterType customParameterType = CalculationStaticTypes.GetCustomParameterType(customParameter.Type);
                            barItemList = (await CustomParameterCreateBarAsync(url, session, userTimeZone, input, barPrm, startTimeUTCSynced, endTimeUTCSynced, barTimeRangeList, customParameterType, true)).ToList();
                            break;

                        case TableWidgetParameterType.BaseParameter:

                            barItemList = (await BaseParameterCreateBarAsync(url, session, input, userTimeZone, startTimeUTCSynced, endTimeUTCSynced, barPrm, resolutionInSec, false, barTimeRangeList, intervalSynchronized)).ToList();
                            break;

                        default:
                            throw new UserFriendlyException("TableWidgetParameterType Supports only BaseParameter");
                    }
                    for (int barItemNum = 0; barItemNum < barItemList.Count; barItemNum++)
                    {
                        BarItem barItem = barItemList[barItemNum];
                        dataUnitType = barItem.DataUnitType;
                        if (barItemNum % numOfDates == 0)
                        {
                            barItemGroupList = new List<BarItem>();
                            BarGroup barGroup = new BarGroup(barItem.SeriesName, barItemGroupList);
                            barGroups?.Add(barGroup);
                        }

                        barItem = new BarItem(timeRangeToDateFormatMap[barItemNum % numOfDates], barItem.Value, barItem.DataUnitType, barItem.Status);
                        barItemGroupList?.Add(barItem);
                    }
                    //BarGroup barGroup = new BarGroup(barPrm.ParameterName, barItemList);
                    //barGroups.Add(barGroup);

                }
                catch (SessionExpiredException sessionExpiredException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex.Message);
                    throw new UserFriendlyException($"{barPrm.ParameterName} - Failed [{ex.Message}] please rerun without it.");
                }

                //var buckets = GenerateBuckets(input.StartDate, input.EndDate, bucketSize).ToList();
            }

            return (dataUnitType, barGroups);
        }

        private async Task<(List<BarGroup>? barGroups, DataUnitType dataUnitType)> ParametersAndFeedersCustomGroupBarChart(string url, string session, BarChartRequest input, Infrastructure.TimeZone userTimeZone, DateTime startDate, DateTime endDate, List<SubGroup> subgroups, List<BarParameter> eventColWidgetTableList, List<BarParameter> otherColWidgetTableList, Dictionary<Guid, Dictionary<FiltersGroup, HashSet<BaseParameterComponent>>> baseParametersHashSet)
        {
            DataUnitType? dataUnitType = null;
            List<BarGroup>? barGroups = new List<BarGroup>();
            foreach (var parameter in input.BarPrmList)
            {
                PreparePrmMapForReq(startDate, endDate, true, input.Feeders, eventColWidgetTableList, otherColWidgetTableList, baseParametersHashSet, parameter, input.RefreshRateInSeconds.Value);
            }

            //List<TableWidgetResponseItem> tableEventWidgetResponseItemList = null;
            //if (eventColWidgetTableList.IsCollectionExists())
            //{
            //    tableEventWidgetResponseItemList = await WidgetTableEventCalculation(url, session, input, eventColWidgetTableList, input.StartDate, endDate);
            //    responseItems.AddRange(tableEventWidgetResponseItemList);
            //}

            int resInSec = await LoadPrmToCache(url, session, startDate, endDate, userTimeZone, baseParametersHashSet, input.IsRealTime);

            List<EventParameterDto> dtoList = eventColWidgetTableList
                   .Select(c => new EventParameterDto
                   {
                       TableEvent = c.TableEvent,
                       Normalize = NormalizeEnum.NO,
                       NormalValue = 0,
                       ParameterName = c.ParameterName,
                       ReplaceAggregationWith = string.Empty
                   })
                   .ToList();

            bool isCountQuantity = dtoList.Any(c =>
               Enum.TryParse(c.TableEvent.Quantity, ignoreCase: true, out PQBIQuantityType quantityType)
               && quantityType == PQBIQuantityType.count
           );
            Guid sessionID = Guid.Parse(session);

            int numOfEvPrms = dtoList.Count;
            int numOfGroups = subgroups.Count;
            if (isCountQuantity)
            {
                List<TableWidgetResponseItem> feedersTableWidgetResponseItemList = await PopulateEventsValForFeedersInTable(url, dtoList, sessionID, new PQZDateTime(startDate), new PQZDateTime(endDate), input.Feeders, null!, null!, 0, false, subgroups: subgroups);

                List<BarItem>? barItemList = null;
                for (int barItemNum = 0; barItemNum < feedersTableWidgetResponseItemList.Count; barItemNum++)
                {
                    int itemInGroup = barItemNum % numOfGroups;
                    TableWidgetResponseItem eventsInfo = feedersTableWidgetResponseItemList[barItemNum];
                    BarItem barItem = new BarItem(subgroups[itemInGroup].ToString(), eventsInfo.Calculated, eventsInfo.DataUnitType, eventsInfo.DataValueStatus);
                    dataUnitType = barItem.DataUnitType;

                    if (itemInGroup == 0)
                    {
                        barItemList = new List<BarItem>();
                        BarGroup barGroup = new BarGroup(eventsInfo.ParameterName, barItemList);
                        barGroups.Add(barGroup);
                    }
                    barItemList?.Add(barItem);
                }
            }
            else
            {
                List<TableWidgetResponseItem> feedersTableWidgetResponseItemList = await PopulateEventsValForFeedersInTable(url, dtoList, sessionID, new PQZDateTime(startDate), new PQZDateTime(endDate), input.Feeders, null!, null!, 0, false);

                foreach (var item in feedersTableWidgetResponseItemList) // your List<TableWidgetResponseItem>
                {
                    var value = item.Calculated;

                    // Find subgroup index by range
                    int idx = -1;
                    if (value.HasValue)
                    {
                        idx = subgroups.FindIndex(sg => sg.FromVal <= value.Value && value.Value <= sg.ToVal);
                    }

                    dataUnitType = item.DataUnitType;
                    if (idx >= 0)
                    {
                        BarItem barItem = new BarItem(
                            seriesName: subgroups[idx].ToString(),
                            value: item.Calculated,                 // keep null if you want to visualize "no data"
                            dataUnitType: item.DataUnitType,
                            status: item.DataValueStatus);

                        BarGroup barGroup = new BarGroup(item.ParameterName, [barItem]);
                        barGroups.Add(barGroup);
                    }
                }
            }

            using (var mainLogger = PqbiStopwatch.AnchorAsync($"Tablo - {input.WidgetName} {nameof(CalculateTableAsync)}", Logger))
            {
                for (int i = 0; i < otherColWidgetTableList.Count; i++)
                {
                    BarParameter barPrm = otherColWidgetTableList[i];
                    try
                    {
                        List<BarItem> barItemList = null;
                        using (var sub = mainLogger.CreateSubLogger(barPrm.ParameterName))
                        {
                            TableWidgetParameterType widgetTableType = CalculationStaticTypes.GetTableWidgetParameterType(barPrm.ParameterType);

                            switch (widgetTableType)
                            {
                                case TableWidgetParameterType.CustomParameter:

                                    barItemList = (await CustomParameterCreateBarAsync(url, session, userTimeZone, input, barPrm, startDate, endDate, null, CustomParameterType.BPCP, false)).ToList();
                                    break;

                                case TableWidgetParameterType.BaseParameter:

                                    barItemList = (await BaseParameterCreateBarAsync(url, session, input, userTimeZone, startDate, endDate, barPrm, resInSec, true, null)).ToList();
                                    break;

                                default:
                                    throw new UserFriendlyException("TableWidgetParameterType Supports only BaseParameter");
                            }

                            for (int barItemIndex = 0; barItemIndex < barItemList.Count; barItemIndex++)
                            {
                                var barItem = barItemList[barItemIndex];
                                var value = barItem.Value;

                                // Find subgroup index by range
                                int idx = -1;
                                if (value.HasValue)
                                {
                                    idx = subgroups.FindIndex(sg => sg.FromVal <= value.Value && value.Value <= sg.ToVal);
                                }
                                dataUnitType = barItem.DataUnitType;
                                if (idx >= 0)
                                {
                                    BarGroup barGroup = new BarGroup(barItem.SeriesName, [barItem]);
                                    barItem.SeriesName = subgroups[idx].ToString();
                                    barGroups.Add(barGroup);
                                }
                            }
                        }
                    }
                    catch (SessionExpiredException sessionExpiredException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex.Message);
                        throw new UserFriendlyException($"{barPrm.ParameterName} - Failed [{ex.Message}] please rerun without it.");
                    }
                }
            }

            return (barGroups, dataUnitType);
        }

        private async Task<(List<BarGroup>? barGroups, DataUnitType dataUnitType)> CustomGroupPrmAndFeedersBarChart(string url, string session, BarChartRequest input, Infrastructure.TimeZone userTimeZone,
                DateTime startDate, DateTime endDate, List<SubGroup> subgroups, List<BarParameter> eventColWidgetTableList, List<BarParameter> otherColWidgetTableList, Dictionary<Guid, Dictionary<FiltersGroup, HashSet<BaseParameterComponent>>> baseParametersHashSet)
        {
            DataUnitType? dataUnitType = null;
            List<BarGroup>? barGroups;
            barGroups = subgroups!
                                                    .Select(group => new BarGroup(
                                                        Category: group.ToString(),   // fall back to "" if Name is null
                                                        Bars: new List<BarItem>()))
                                                    .ToList();

            foreach (var parameter in input.BarPrmList)
            {
                PreparePrmMapForReq(startDate, endDate, true, input.Feeders, eventColWidgetTableList, otherColWidgetTableList, baseParametersHashSet, parameter, input.RefreshRateInSeconds!.Value);
            }

            //List<TableWidgetResponseItem> tableEventWidgetResponseItemList = null;
            //if (eventColWidgetTableList.IsCollectionExists())
            //{
            //    tableEventWidgetResponseItemList = await WidgetTableEventCalculation(url, session, input, eventColWidgetTableList, input.StartDate, input.EndDate);
            //    responseItems.AddRange(tableEventWidgetResponseItemList);
            //}

            int resInSec = await LoadPrmToCache(url, session, startDate, endDate, userTimeZone, baseParametersHashSet, input.IsRealTime);
            List<EventParameterDto> dtoList = eventColWidgetTableList
                .Select(c => new EventParameterDto
                {
                    TableEvent = c.TableEvent,
                    Normalize = NormalizeEnum.NO,
                    NormalValue = 0,
                    ParameterName = c.ParameterName,
                    ReplaceAggregationWith = string.Empty
                })
                .ToList();
            Guid sessionID = Guid.Parse(session);

            bool isCountQuantity = dtoList.Any(c =>
                Enum.TryParse(c.TableEvent.Quantity, ignoreCase: true, out PQBIQuantityType quantityType)
                && quantityType == PQBIQuantityType.count
            );

            int numOfEvPrms = dtoList.Count;
            int numOfGroups = subgroups.Count;
            //numOfEvPrms * subgroups.
            if (isCountQuantity)
            {
                List<TableWidgetResponseItem> feedersTableWidgetResponseItemList = await PopulateEventsValForFeedersInTable(url, dtoList, sessionID, new PQZDateTime(startDate), new PQZDateTime(endDate), input.Feeders, null!, null!, 0, false, subgroups: subgroups);

                int barGroupIndex = -1;
                for (int barItemNum = 0; barItemNum < feedersTableWidgetResponseItemList.Count; barItemNum++)
                {
                    TableWidgetResponseItem eventsInfo = feedersTableWidgetResponseItemList[barItemNum];
                    BarItem barItem = new BarItem(eventsInfo.ParameterName, eventsInfo.Calculated, eventsInfo.DataUnitType, eventsInfo.DataValueStatus);
                    dataUnitType = barItem.DataUnitType;

                    barGroupIndex = barItemNum % numOfGroups;
                    barGroups[barGroupIndex].Bars.Add(barItem);
                }
            }
            else
            {
                List<TableWidgetResponseItem> feedersTableWidgetResponseItemList = await PopulateEventsValForFeedersInTable(url, dtoList, sessionID, new PQZDateTime(startDate), new PQZDateTime(endDate), input.Feeders, null!, null!, 0, false);

                foreach (var item in feedersTableWidgetResponseItemList) // your List<TableWidgetResponseItem>
                {
                    var value = item.Calculated;

                    // Find subgroup index by range
                    int idx = -1;
                    if (value.HasValue)
                    {
                        idx = subgroups.FindIndex(sg => sg.FromVal <= value.Value && value.Value <= sg.ToVal);
                    }
                    dataUnitType = item.DataUnitType;

                    if (idx >= 0)
                    {
                        barGroups[idx].Bars.Add(new BarItem(
                            seriesName: item.ParameterName,
                            value: item.Calculated,                 // keep null if you want to visualize "no data"
                            dataUnitType: item.DataUnitType,
                            status: item.DataValueStatus));
                    }
                }
            }

            using (var mainLogger = PqbiStopwatch.AnchorAsync($"Tablo - {input.WidgetName} {nameof(CalculateTableAsync)}", Logger))
            {
                for (int i = 0; i < otherColWidgetTableList.Count; i++)
                {
                    BarParameter barPrm = otherColWidgetTableList[i];
                    try
                    {
                        List<BarItem> barItemList = null;
                        using (var sub = mainLogger.CreateSubLogger(barPrm.ParameterName))
                        {
                            TableWidgetParameterType widgetTableType = CalculationStaticTypes.GetTableWidgetParameterType(barPrm.ParameterType);


                            //AdvancedSettings advancedSettings = new AdvancedSettings(parameter.NormalValue, parameter.Normalize, parameter.IsExcludeFlaggedData, parameter.ExcludeFlagged, parameter.IgnoreAligningFunction, parameter.ReplaceAggregationWith);

                            switch (widgetTableType)
                            {
                                case TableWidgetParameterType.CustomParameter:

                                    barItemList = (await CustomParameterCreateBarAsync(url, session, userTimeZone, input, barPrm, startDate, endDate, null, CustomParameterType.MPSC, false)).ToList();
                                    break;

                                case TableWidgetParameterType.BaseParameter:

                                    barItemList = (await BaseParameterCreateBarAsync(url, session, input, userTimeZone, startDate, endDate, barPrm, resInSec, true)).ToList();
                                    break;

                                default:
                                    throw new UserFriendlyException("TableWidgetParameterType Supports only BaseParameter");
                            }
                            foreach (var item in barItemList) // your List<TableWidgetResponseItem>
                            {
                                var value = item.Value;

                                // Find subgroup index by range
                                int idx = -1;
                                if (value.HasValue)
                                {
                                    idx = subgroups.FindIndex(sg => sg.FromVal <= value.Value && value.Value <= sg.ToVal);
                                }
                                dataUnitType = item.DataUnitType;

                                if (idx >= 0)
                                {
                                    barGroups[idx].Bars.Add(item);
                                }
                            }
                        }
                    }
                    catch (SessionExpiredException sessionExpiredException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex.Message);
                        throw new UserFriendlyException($"{barPrm.ParameterName} - Failed [{ex.Message}] please rerun without it.");
                    }
                }
            }

            return (barGroups, dataUnitType);
        }

        private async Task<(TimeBucket timeBucket, IntervalSynchronized syncEnum, IEnumerable<(DateTime barStart, DateTime barEnd)>? barTimeRangeList, int resolutionInSec, DateTime startTimeUTCSynced, DateTime endTimeUTCSynced)> GetDatesForBars(string url, string session, BarChartRequest input, Infrastructure.TimeZone userTimeZone, DateTime startDate, DateTime endDate, List<BarParameter> eventColWidgetTableList, List<BarParameter> otherColWidgetTableList, Dictionary<Guid, Dictionary<FiltersGroup, HashSet<BaseParameterComponent>>> baseParametersHashSet)
        {
            int numOfPrms = 0;
            foreach (var parameter in input.BarPrmList)
            {
                numOfPrms += PreparePrmMapForReq(startDate, endDate, false, input.Feeders, eventColWidgetTableList, otherColWidgetTableList, baseParametersHashSet, parameter, input.RefreshRateInSeconds!.Value);
            }

            int barsPerParam = Math.Max(1, MAX_NUM_BARS / numOfPrms);
            (var bucketSize, IEnumerable<(DateTime barStart, DateTime barEnd)>? barTimeRangeList) = CalendarBuckets.ChooseBucket(input.StartDate, input.EndDate, barsPerParam);


            //(DateTime startSyncedUtc, DateTime endSyncedUtc) = ScadaRequestPlanner.Plan(input.StartDate, input.EndDate, bucketSize);
            //IEnumerable<(DateTime barStart, DateTime barEnd)> barTimeRangeList = CalendarBuckets.GenerateBuckets(startSyncedUtc, endSyncedUtc, bucketSize);

            IntervalSynchronized syncEnum = bucketSize.ToIntervalSync();
            int resolutionInSec = 0;
            //if (!input.IsRealTime)
            {
                resolutionInSec = (int)new SyncInterval(syncEnum).TimeIntervalInSec;
                foreach (var item in baseParametersHashSet)
                {
                    Dictionary<FiltersGroup, HashSet<BaseParameterComponent>> filterToPrmSetMap = item.Value;

                    foreach (var filterToPrmSet in filterToPrmSetMap)
                    {
                        foreach (BaseParameterComponent baseParameterComponent in filterToPrmSet.Value)
                        {
                            baseParameterComponent.MeasurementParameter.SyncInterval = new SyncInterval(syncEnum);
                        }
                    }
                }
            }
            //else
            //{
            //    resolutionInSec = input.RefreshRateInSeconds.Value;
            //    syncEnum = IntervalSynchronized.ISX;
            //    foreach (var item in baseParametersHashSet)
            //    {
            //        Dictionary<FiltersGroup, HashSet<BaseParameterComponent>> filterToPrmSetMap = item.Value;

            //        foreach (var filterToPrmSet in filterToPrmSetMap)
            //        {
            //            foreach (BaseParameterComponent baseParameterComponent in filterToPrmSet.Value)
            //            {
            //                baseParameterComponent.MeasurementParameter.SyncInterval = new SyncInterval(resolutionInSec);
            //            }
            //        }
            //    }

            //}

            DateTime endTimeUTCSynced = CalendarBuckets.AlignCeil(endDate, bucketSize);
            DateTime startTimeUTCSynced = CalendarBuckets.AlignFloor(startDate, bucketSize);

            await LoadPrmToCache(url, session, startTimeUTCSynced, endTimeUTCSynced, userTimeZone, baseParametersHashSet, input.IsRealTime);
            return (bucketSize, syncEnum, barTimeRangeList, resolutionInSec, startTimeUTCSynced, endTimeUTCSynced);
        }

        public Task<IEnumerable<BarItem>> CustomParameterCreateBarAsync(
                string url,
                string session,
                Infrastructure.TimeZone userTimeZoneInfo,
                BarChartRequest request,
                BarParameter parameter,
                DateTime startTimeUTC, 
                DateTime endTimeUTC,
                IEnumerable<(DateTime barStart, DateTime barEnd)>? barTimeRangeList,
                CustomParameterType customParameterType,
                bool isSharedFeeders,
                AdvancedSettings? advanced = null)
                => CustomParameterCreateAsync<BarItem, BarChartRequest, BarParameter>(
                       url, session, userTimeZoneInfo, startTimeUTC, endTimeUTC, request, parameter,
                       p => p.CustomData.Id,             // how to get CP id
                       selector => RealCalculateBarAsync(
                                        url, session, request, parameter,
                                        selector,
                                        customParameterType,
                                        isSharedFeeders,
                                        parameter.CustomData.Quantity),
                       validate: null,
                       barTimeRangeList,
                       advanced);

        //private async Task<IEnumerable<BarItem>> CustomParameterCreateBarAsync(string url, string session, BarChartRequest input, BarParameter parameter, AdvancedSettings? advancedSettings = null)
        //{
        //    var customParameterId = parameter.CustomData.Id;

        //    var customParameter = GetCustomParameter(customParameterId);
        //    //CustomParameterTableValidate(input, customParameter);
        //    Func<IEnumerable<FeederComponentInfo>, bool, Task<IEnumerable<CustomParameterNodeCalculator>>> selector = async (feeders, isTag) =>
        //    {
        //        var nodes = await AssembleCustomParameterTree(customParameterId, url, session, input.StartDate, input.EndDate, -1, false, parameter.CustomData.Quantity, feeders, isTag, advancedSettings);
        //        //var nodes = await AssembleCustomParameterTree(customParameterId, url, session, input.StartDate, input.EndDate, -1, false, parameter.CustomData.Quantity, feeders);
        //        return [nodes];
        //    };

        //    var responseItems = await RealCalculateBarAsync(url, session, input, parameter, selector, parameter.CustomData.Quantity);
        //    return responseItems;
        //}

        private async Task<IEnumerable<BarItem>> BaseParameterCreateBarAsync(string url, string session, BarChartRequest input, Infrastructure.TimeZone userTimeZone, DateTime startDate, DateTime endDate, BarParameter parameter, int resolutionInSec, bool isExpectedSingleRes, IEnumerable<(DateTime barStart, DateTime barEnd)>? barTimeRangeList = null, IntervalSynchronized intervalSync = IntervalSynchronized.ISX, bool isHideMsrPointName = false, AdvancedSettings? advancedSettings = null)
        {
            var baseParameter = JsonConvert.DeserializeObject<BaseParameter>(parameter.BaseData);
            //baseParameter.SetISXResolution(input.StartDate, input.EndDate);
            //    int totalSeconds = (int)(endDate - startDate).TotalSeconds;

            bool isNeedFilterByEvents = false;
            if (advancedSettings?.IsExcludeFlaggedData == true)
                isNeedFilterByEvents = true;

            CustomParameterNodeCalculator node;
            if (barTimeRangeList != null)
            {
                baseParameter.Resolution = resolutionInSec;
                baseParameter.IntervalSync = intervalSync;
                node = new CustomParameterNodeCalculator(CustomParameterType.BPCP, -1, false, string.Empty, startDate, endDate, widgetResolutionInSecond: resolutionInSec, baseParameter.Quantity, isHideMsrPointName: isHideMsrPointName, barTimeRangeList: barTimeRangeList, advancedSettingsForTable: advancedSettings);
                node.IsSinglePointRes = isExpectedSingleRes;
            }
            else
            {
                baseParameter.Resolution = resolutionInSec;
                //if (!isNeedFilterByEvents)
                //{
                //    baseParameter.Resolution = (int)((input.EndDate - input.StartDate).TotalSeconds);
                //}
                node = new CustomParameterNodeCalculator(CustomParameterType.BPCP, -1, false, string.Empty, startDate, endDate, -1, baseParameter.Quantity, isHideMsrPointName: isHideMsrPointName, advancedSettingsForTable: advancedSettings);
                node.IsSinglePointRes = isExpectedSingleRes;
            }

            Func<IEnumerable<FeederComponentInfo>, bool, Task<IEnumerable<CustomParameterNodeCalculator>>> selector = async (feeders, isTag) =>
            {
                var parameterComponents = baseParameter.CreateBaseParameterComponents(feeders);
                node.ClearMsrPrmCollection();
                node.PopulateWithBaseParameterComponents(parameterComponents);

                //SelectAssemble(node, feeders);

                await SendingAndStoringDataAsync(url, session, startDate, endDate, userTimeZone, (false, null), parameterComponents, advancedSettings?.FiltersGroup, input.IsRealTime, input.RefreshRateInSeconds!.Value);

                return [node];
            };

            var responseItems = await RealCalculateBarAsync(url, session, input, parameter, selector, CustomParameterType.BPCP, false, baseParameter.Quantity);
            return responseItems;
        }

        private async Task<IEnumerable<BarItem>> RealCalculateBarAsync(
                 string url, string session, BarChartRequest input, BarParameter parameter,
                 Func<IEnumerable<FeederComponentInfo>, bool,
                      Task<IEnumerable<CustomParameterNodeCalculator>>> calculationSelector,
                 CustomParameterType customParameterType,
                 bool isSharedFeeders,
                 string quantity, AdvancedSettings? advancedSettings = null)
        {
            using var logger = PqbiStopwatch.AnchorAsync(nameof(RealCalculateBarAsync), Logger);

            (List<BarItem> items, string? outerAggFunction, CustomParameterType customPrmType) = await CalculatePerFeederAsync(
                feeders: input.Feeders,
                calcSelector: calculationSelector,
                resultSelector: (node, graphs, feeder) =>
                    ArrangingForBarChart(graphs,
                                         feeder.ComponentId.ToString(),
                                         feeder.Id),
                customParameterType,
                isSharedFeeders,
                map: null,   // not used for bar chart
                logger: logger);

            return items;
        }

        private static string FormatCategory(DateTime start, TimeBucket unit)
            => unit switch
            {
                TimeBucket.Hour => start.ToString("HH:mm dd-MMM", CultureInfo.InvariantCulture),
                TimeBucket.Day => start.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture),
                TimeBucket.Week => $"W{ISOWeek.GetWeekOfYear(start)} {start:yyyy}",
                TimeBucket.Month => start.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                TimeBucket.Quarter => $"Q{((start.Month - 1) / 3) + 1} {start:yyyy}",
                TimeBucket.Year => start.ToString("yyyy"),
                TimeBucket.FiveYears => $"{start.Year}-{start.Year + 4}",
                TimeBucket.TenYears => $"{start.Year}-{start.Year + 9}",
                _ => start.ToString(CultureInfo.InvariantCulture)   // fallback
            };

        #endregion

        #region Table

        public async Task<TableWidgetResponse> CalculateTableAsync(string url, string session, TableWidgetRequest input)
        {
            (DateTime startDate, DateTime endDate) = input.NormalizeDatesToUtc();
            Infrastructure.TimeZone userTimeZone = new Infrastructure.TimeZone(input.UserTimeZoneID, input.UserTimeZone);
            var responseItems = new List<TableWidgetResponseItem>();
            var paramComponents = new List<BaseParameterComponent>();

            List<ColumnWidgetTable> eventColWidgetTableList = new List<ColumnWidgetTable>();
            List<ColumnWidgetTable> otherColWidgetTableList = new List<ColumnWidgetTable>();
            Dictionary<Guid, Dictionary<FiltersGroup, HashSet<BaseParameterComponent>>> baseParametersHashSet = new Dictionary<Guid, Dictionary<FiltersGroup, HashSet<BaseParameterComponent>>>();

            using (var mainLogger = PqbiStopwatch.AnchorAsync($"Table - {input.WidgetName} {nameof(CalculateTableAsync)}", Logger))
            {
                foreach (var parameter in input.ColumnWidgetTables)
                {
                    PreparePrmMapForReq(startDate, endDate, true, input.Rows.Feeders, eventColWidgetTableList, otherColWidgetTableList, baseParametersHashSet, parameter, input.RefreshRateInSeconds!.Value);
                }

                List<TableWidgetResponseItem> tableEventWidgetResponseItemList = null;
                if (eventColWidgetTableList.IsCollectionExists())
                {
                    tableEventWidgetResponseItemList = await WidgetTableEventCalculation(url, session, input, eventColWidgetTableList, startDate, endDate);
                    responseItems.AddRange(tableEventWidgetResponseItemList);
                }

                int resInSec = await LoadPrmToCache(url, session, startDate, endDate, userTimeZone, baseParametersHashSet, input.IsRealTime);

                foreach (var parameter in otherColWidgetTableList)
                {
                    try
                    {
                        using (var sub = mainLogger.CreateSubLogger(parameter.ParameterName))
                        {
                            TableWidgetParameterType widgetTableType = CalculationStaticTypes.GetTableWidgetParameterType(parameter.ParameterType);

                            AdvancedSettings advancedSettings = new AdvancedSettings(parameter.NormalValue, parameter.Normalize, parameter.IsExcludeFlaggedData, parameter.ExcludeFlagged, parameter.IgnoreAligningFunction, parameter.ReplaceAggregationWith);

                            switch (widgetTableType)
                            {
                                case TableWidgetParameterType.CustomParameter:

                                    var customParameter = GetCustomParameter(parameter.CustomData.Id);
                                    CustomParameterType customParameterType = CalculationStaticTypes.GetCustomParameterType(customParameter.Type);
                                    var items = await CustomParameterCreateTableNodeAsync(url, session, userTimeZone, input, parameter, startDate, endDate, false, customParameterType, advancedSettings);
                                    responseItems.AddRange(items);

                                    break;

                                case TableWidgetParameterType.BaseParameter:

                                    var baseParamaterItems = await BaseParameterCreateTableNodeAsync(url, session, input, startDate, endDate, parameter, false, resInSec, true, advancedSettings);
                                    responseItems.AddRange(baseParamaterItems);
                                    break;

                                default:
                                    throw new UserFriendlyException("TableWidgetParameterType Supports only BaseParameter");
                            }
                        }

                    }
                    catch (SessionExpiredException sessionExpiredException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex.Message);
                        throw new UserFriendlyException($"{parameter.ParameterName} - Failed [{ex.Message}] please rerun without it.");
                    }

                }
            }

            return new TableWidgetResponse { Items = responseItems };
        }

        private void CustomParameterTableValidate(TableWidgetRequest input, CustomParameters.CustomParameter customParameter, bool isFeedersAreShared)
        {
            if (!isFeedersAreShared)
            {
                var customParameterType = CalculationStaticTypes.GetCustomParameterType(customParameter.Type);

                if (customParameterType == CustomParameterType.Exception)
                {

                    throw new UserFriendlyException("In table exception mode is not allowrd.");
                }
            }
        }

        public Task<IEnumerable<TableWidgetResponseItem>> CustomParameterCreateTableNodeAsync(
                string url,
                string session,
                Infrastructure.TimeZone timeZoneInfo,
                TableWidgetRequest request,
                ColumnWidgetTable parameter,
                DateTime startTimeUTC, DateTime endTimeUTC,
                bool isFeedersAreShared,
                CustomParameterType customParameterType,
                AdvancedSettings? advanced = null)
                => CustomParameterCreateAsync<TableWidgetResponseItem,
                                              TableWidgetRequest,
                                              ColumnWidgetTable>(
                       url, session, timeZoneInfo, startTimeUTC, endTimeUTC, 
                       request, parameter,                       
                       p => p.CustomData.Id,
                       selector => RealCalculateTableAsync(
                                        url, session, request, parameter,
                                        selector,
                                        parameter.CustomData.Quantity,
                                        isFeedersAreShared,
                                        customParameterType,
                                        advanced),
                       validate: (req, cp) => CustomParameterTableValidate(req, cp, isFeedersAreShared),
                       advanced: advanced);

        private TableWidgetResponseItem ArrangingForTable(BasicValue calculated, string? componentId, int? feederId, string parameterName, string quantity, DataUnitType dataType, string? TagName = null, string? TagValue = null, MissingBaseParameterInfo missingBaseParameterInfo = null)
        {
            Tag tag = null;
            if (TagName is not null)
            {
                tag = new Tag { TagId = TagName, TagValue = TagValue };
            }

            var result = new TableWidgetResponseItem
            {
                Calculated = calculated.Value,
                DataValueStatus = calculated.DataValueStatus,
                ComponentId = componentId,
                ParameterName = parameterName,
                FeederId = feederId,
                Quantity = quantity,
                MissingBaseParameterInfo = missingBaseParameterInfo,
                Tag = tag,
                DataUnitType = dataType,
            };

            return result;
        }

        private TableWidgetResponseItem ArrangingForTable(GraphParametersComponentDtoV3 graph, string parameterName, string quantity, bool isFeedersAreShared, string? TagName = null, string? TagValue = null)
        {
            //double?  value = 
            var componentId = graph.Feeders.FirstOrDefault()?.ComponentId;
            var feederId = graph.Feeders.FirstOrDefault()?.Id;
            string prmName = parameterName;
            if (isFeedersAreShared)
                prmName = graph.CustomParameterName;
            return ArrangingForTable(graph.FirstValue(), componentId?.ToString(), feederId, prmName, quantity, graph.DataUnitType, TagName, TagValue, graph.MissingInformation?.FirstOrDefault());
        }

        private async Task<IEnumerable<TableWidgetResponseItem>> RealCalculateTableAsync(string url, string session, TableWidgetRequest input, ColumnWidgetTable parameter, Func<IEnumerable<FeederComponentInfo>, bool, Task<IEnumerable<CustomParameterNodeCalculator>>> calculationSelector, string quantity, bool isFeedersAreShared, CustomParameterType customParameterType, AdvancedSettings? advancedSettings = null)
        {
            using var logger = PqbiStopwatch.AnchorAsync(nameof(RealCalculateTableAsync), Logger);

            var feederMap = new Dictionary<FeederComponentInfo, GraphParametersComponentDtoV3?>();

            (List<TableWidgetResponseItem> items, string? outerAggFunction, CustomParameterType customPrmType) = await CalculatePerFeederAsync(
                feeders: input.Rows.Feeders,
                calcSelector: calculationSelector,
                resultSelector: (node, graphs, feeder, isFeedersAreShared) =>
                    ArrangingForTable(graphs,
                                     quantity,
                                     parameter.ParameterName,
                                     isFeedersAreShared
                                      ),
                isFeedersAreShared,
                map: feederMap,   // we’ll need it later for tags
                logger: logger,
                customParameterType,
                advancedSettings);

            //try
            {

                string outerAggregationFunction = outerAggFunction!;
                if (advancedSettings != null)
                    outerAggregationFunction = string.IsNullOrEmpty(advancedSettings.ReplaceOuterAggregationWith) ? outerAggregationFunction : advancedSettings.ReplaceOuterAggregationWith;

                foreach (var tag in input.Rows.Tags)
                {
                    if (customParameterType == CustomParameterType.SPMC)
                    {
                        var nodes = await calculationSelector(tag.Feeders, true);
                        var node = nodes.First();
                        var graphes = _engineControllerService.RootCalculation(node);

                        items.AddRange(ArrangingForTable(graphes, quantity, parameter.ParameterName, isFeedersAreShared, tag.Id, tag.Name));
                    }
                    else
                    {
                        string tagQuantity = quantity;
                        if (customParameterType == CustomParameterType.MPSC)
                            tagQuantity = outerAggregationFunction;

                        CalculateForMltiAndBaseParameter(feederMap, tag.Feeders, out var calculated, tagQuantity, out var missingBaseParameterInfo);
                        var responseItem = ArrangingForTable(calculated, null, null, parameter.ParameterName, quantity, new EmptyDataUnitType(), tag.Id, tag.Name, missingBaseParameterInfo: missingBaseParameterInfo);
                        items.Add(responseItem);
                    }
                }
            }
            //catch (Exception ex)
            {
            }

            return items;
        }

        private static void PrepareEventDataForTagCalculation(Dictionary<string, Dictionary<FeederComponentInfo, List<PQEvent>>> columnToFeederEventsMap, Dictionary<string, Dictionary<FeederComponentInfo, double>> columnToFeederEventsResMap, int tagCount, FeederComponentInfo feederComponentInfo, double calcValue, EventParameterDto columnWidgetTable, List<PQEvent> pqEventList, PQBIQuantityType quantityType)
        {
            if (tagCount > 0)
            {
                Dictionary<FeederComponentInfo, List<PQEvent>> feederIdToEvsMap;
                Dictionary<FeederComponentInfo, double> feederIdToRowResMap;
                if (quantityType == PQBIQuantityType.avg || quantityType == PQBIQuantityType.percentile)
                {
                    if (!columnToFeederEventsMap.TryGetValue(columnWidgetTable.ParameterName, out feederIdToEvsMap))
                    {
                        feederIdToEvsMap = new Dictionary<FeederComponentInfo, List<PQEvent>>();
                        columnToFeederEventsMap.Add(columnWidgetTable.ParameterName, feederIdToEvsMap);
                    }

                    List<PQEvent> pqEvList;
                    if (!feederIdToEvsMap.TryGetValue(feederComponentInfo, out pqEvList))
                    {
                        pqEvList = new List<PQEvent>();
                        feederIdToEvsMap.Add(feederComponentInfo, pqEvList);
                    }
                    pqEvList.AddRange(pqEventList);
                }
                else
                {
                    if (!columnToFeederEventsResMap.TryGetValue(columnWidgetTable.ParameterName, out feederIdToRowResMap))
                    {
                        feederIdToRowResMap = new Dictionary<FeederComponentInfo, double>();
                        columnToFeederEventsResMap.Add(columnWidgetTable.ParameterName, feederIdToRowResMap);
                    }

                    if (!feederIdToRowResMap.TryGetValue(feederComponentInfo, out double oldColRes))
                    {
                        feederIdToRowResMap[feederComponentInfo] = calcValue;
                    }
                }
            }
        }

        #endregion

        #region Card

        public async Task<TableWidgetResponse> CalculateCardAsync(string url, string session, TableWidgetRequest input)
        {
            (DateTime startDate, DateTime endDate) = input.NormalizeDatesToUtc();
            Infrastructure.TimeZone userTimeZone = new Infrastructure.TimeZone(input.UserTimeZoneID, input.UserTimeZone);
            var responseItems = new List<TableWidgetResponseItem>();
            var paramComponents = new List<BaseParameterComponent>();

            List<ColumnWidgetTable> eventColWidgetTableList = new List<ColumnWidgetTable>();
            List<ColumnWidgetTable> otherColWidgetTableList = new List<ColumnWidgetTable>();
            Dictionary<Guid, Dictionary<FiltersGroup, HashSet<BaseParameterComponent>>> baseParametersHashSet = new Dictionary<Guid, Dictionary<FiltersGroup, HashSet<BaseParameterComponent>>>();

            using (var mainLogger = PqbiStopwatch.AnchorAsync($"Tablo - {input.WidgetName} {nameof(CalculateTableAsync)}", Logger))
            {
                foreach (var parameter in input.ColumnWidgetTables)
                {
                    PreparePrmMapForReq(startDate, endDate, true, input.Rows.Feeders, eventColWidgetTableList, otherColWidgetTableList, baseParametersHashSet, parameter, input.RefreshRateInSeconds!.Value);
                }

                List<TableWidgetResponseItem> tableEventWidgetResponseItemList = null;
                if (eventColWidgetTableList.IsCollectionExists())
                {
                    tableEventWidgetResponseItemList = await WidgetTableEventCalculationForCard(url, session, input, eventColWidgetTableList, startDate, endDate);
                    responseItems.AddRange(tableEventWidgetResponseItemList);

                    //tableEventWidgetResponseItemList = await WidgetTableEventCalculation(url, session, input, eventColWidgetTableList, input.StartDate, input.EndDate);                   
                }

                int resInSec = await LoadPrmToCache(url, session, startDate, endDate, userTimeZone, baseParametersHashSet, input.IsRealTime);

                foreach (var parameter in otherColWidgetTableList)
                {
                    try
                    {
                        using (var sub = mainLogger.CreateSubLogger(parameter.ParameterName))
                        {
                            TableWidgetParameterType widgetTableType = CalculationStaticTypes.GetTableWidgetParameterType(parameter.ParameterType);

                            AdvancedSettings advancedSettings = new AdvancedSettings(parameter.NormalValue, parameter.Normalize, parameter.IsExcludeFlaggedData, parameter.ExcludeFlagged, parameter.IgnoreAligningFunction, parameter.ReplaceAggregationWith);

                            switch (widgetTableType)
                            {
                                case TableWidgetParameterType.CustomParameter:
                                case TableWidgetParameterType.Exception:
                                    var customParameter = GetCustomParameter(parameter.CustomData.Id);
                                    CustomParameterType customParameterType = CalculationStaticTypes.GetCustomParameterType(customParameter.Type);
                                    var items = await CustomParameterCreateTableNodeAsync(url, session, userTimeZone, input, parameter, startDate, endDate, true, customParameterType, advancedSettings);
                                    responseItems.AddRange(items);

                                    break;

                                case TableWidgetParameterType.BaseParameter:

                                    var baseParamaterItems = await BaseParameterCreateTableNodeAsync(url, session, input, startDate, endDate, parameter, true, resInSec, true, advancedSettings);
                                    responseItems.AddRange(baseParamaterItems);
                                    break;

                                default:
                                    throw new UserFriendlyException("TableWidgetParameterType Supports only BaseParameter");
                            }
                        }

                    }
                    catch (SessionExpiredException sessionExpiredException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex.Message);
                        throw new UserFriendlyException($"{parameter.ParameterName} - Failed [{ex.Message}] please rerun without it.");
                    }

                }
            }

            return new TableWidgetResponse { Items = responseItems };
        }

        #endregion

        #region Gauge

        public async Task<TableWidgetResponse> CalculateGaugeAsync(string url, string session, TableWidgetRequest input)
        {
            (DateTime startDate, DateTime endDate) = input.NormalizeDatesToUtc();
            Infrastructure.TimeZone userTimeZone = new Infrastructure.TimeZone(input.UserTimeZoneID, input.UserTimeZone);
            var responseItems = new List<TableWidgetResponseItem>();
            var paramComponents = new List<BaseParameterComponent>();

            List<ColumnWidgetTable> eventColWidgetTableList = new List<ColumnWidgetTable>();
            List<ColumnWidgetTable> otherColWidgetTableList = new List<ColumnWidgetTable>();
            Dictionary<Guid, Dictionary<FiltersGroup, HashSet<BaseParameterComponent>>> baseParametersHashSet = new Dictionary<Guid, Dictionary<FiltersGroup, HashSet<BaseParameterComponent>>>();

            using (var mainLogger = PqbiStopwatch.AnchorAsync($"Tablo - {input.WidgetName} {nameof(CalculateTableAsync)}", Logger))
            {
                foreach (var parameter in input.ColumnWidgetTables)
                {
                    if (parameter.Markers != null)
                    {
                        List<GaugeMarkerDto> markerList = new List<GaugeMarkerDto>();
                        foreach (var marker in parameter.Markers)
                        {
                            if (marker.Operation.HasValue)
                                markerList.Add(marker);
                        }
                        if (markerList.Count > 0)
                            parameter.Markers = markerList;
                        else
                            parameter.Markers = null;
                    }
                    TableWidgetParameterType widgetTableType = CalculationStaticTypes.GetTableWidgetParameterType(parameter.ParameterType);
                    if (widgetTableType == TableWidgetParameterType.BaseParameter)
                    {
                        PreparePrmMapForReq(startDate, endDate, true, input.Rows.Feeders, eventColWidgetTableList, otherColWidgetTableList, baseParametersHashSet, parameter, input.RefreshRateInSeconds!.Value);
                        if (parameter.Markers != null)
                        {
                            foreach (var marker in parameter.Markers)
                            {
                                PreparePrmMapForReq(startDate, endDate, true, input.Rows.Feeders, eventColWidgetTableList, otherColWidgetTableList, baseParametersHashSet, parameter, input.RefreshRateInSeconds!.Value, marker);
                            }
                        }
                    }
                    else if (widgetTableType == TableWidgetParameterType.Event)
                    {
                        PreparePrmMapForReq(startDate, endDate, true, input.Rows.Feeders, eventColWidgetTableList, otherColWidgetTableList, baseParametersHashSet, parameter, input.RefreshRateInSeconds!.Value);
                        if (parameter.Markers != null)
                        {
                            foreach (var marker in parameter.Markers)
                            {
                                TableWidgetEvent widgetEvent = CloneEventWithQuantity(parameter.TableEvent, marker.Operation.ToString());
                                ColumnWidgetTable parameterDto = new ColumnWidgetTable
                                {
                                    TableEvent = widgetEvent,
                                    IsExcludeFlaggedData = parameter.IsExcludeFlaggedData,
                                    ExcludeFlagged = parameter.ExcludeFlagged,
                                    Normalize = parameter.Normalize,
                                    NormalValue = parameter.NormalValue,
                                    ParameterName = parameter.ParameterName,
                                    ReplaceAggregationWith = parameter.ReplaceAggregationWith
                                };
                                eventColWidgetTableList.Add(parameterDto);
                                //PreparePrmMapForReq(input.StartDate, input.EndDate, true, input.Rows.Feeders, eventColWidgetTableList, otherColWidgetTableList, baseParametersHashSet, parameterDto);
                            }
                        }
                    }
                    else
                        PreparePrmMapForReq(startDate, endDate, true, input.Rows.Feeders, eventColWidgetTableList, otherColWidgetTableList, baseParametersHashSet, parameter, input.RefreshRateInSeconds!.Value);
                }

                if (eventColWidgetTableList.IsCollectionExists())
                {
                    List<TableWidgetResponseItem> tableEventWidgetResponseItemList = await WidgetTableEventCalculationForCard(url, session, input, eventColWidgetTableList, startDate, endDate);

                    if (tableEventWidgetResponseItemList.Count > 0)
                    {
                        AddGaugeToResponse(responseItems, tableEventWidgetResponseItemList);
                    }
                }

                int resInSec = await LoadPrmToCache(url, session, startDate, endDate, userTimeZone, baseParametersHashSet, input.IsRealTime);

                for (int i = 0; i < otherColWidgetTableList.Count; i++)
                {
                    var parameter = otherColWidgetTableList[i];

                    try
                    {
                        using (var sub = mainLogger.CreateSubLogger(parameter.ParameterName))
                        {
                            TableWidgetParameterType widgetTableType = CalculationStaticTypes.GetTableWidgetParameterType(parameter.ParameterType);
                            AdvancedSettings advancedSettings = new AdvancedSettings(parameter.NormalValue, parameter.Normalize, parameter.IsExcludeFlaggedData, parameter.ExcludeFlagged, parameter.IgnoreAligningFunction, parameter.ReplaceAggregationWith);

                            switch (widgetTableType)
                            {
                                case TableWidgetParameterType.CustomParameter:
                                case TableWidgetParameterType.Exception:
                                    {
                                        var customParameter = GetCustomParameter(parameter.CustomData.Id);
                                        CustomParameterType customParameterType = CalculationStaticTypes.GetCustomParameterType(customParameter.Type);
                                        advancedSettings.Markers = parameter.Markers;
                                        var items = await CustomParameterCreateTableNodeAsync(url, session, userTimeZone, input, parameter, startDate, endDate, true, customParameterType, advancedSettings);
                                        var tableParamWidgetResponseItemList = items.ToList();
                                        AddGaugeToResponse(responseItems, tableParamWidgetResponseItemList);
                                    }
                                    break;

                                case TableWidgetParameterType.BaseParameter:
                                    {
                                        if (i > 0)
                                            advancedSettings.Markers = [parameter.Markers[i - 1]];
                                        else
                                            advancedSettings.Markers = null;
                                        var baseParamaterItems = await BaseParameterCreateTableNodeAsync(url, session, input, startDate, endDate, parameter, true, resInSec, true, advancedSettings);

                                        if (i == 0)
                                        {
                                            responseItems.AddRange(baseParamaterItems);
                                            foreach (var item in baseParamaterItems)
                                            {
                                                item.GaugeMarkerResultList = [];
                                            }
                                        }
                                        else
                                        {
                                            for (int responseNum = 0; responseNum < responseItems.Count; responseNum++)
                                            {
                                                GaugeMarkerResultDto gaugeMarkerDto = new GaugeMarkerResultDto();
                                                TableWidgetResponseItem markerResponseItem = baseParamaterItems.ElementAt(responseNum);
                                                gaugeMarkerDto.Value = markerResponseItem.Calculated;
                                                gaugeMarkerDto.DataValueStatus = markerResponseItem.DataValueStatus;

                                                responseItems[responseNum].GaugeMarkerResultList.Add(gaugeMarkerDto);
                                            }
                                        }
                                    }
                                    break;

                                default:
                                    throw new UserFriendlyException("TableWidgetParameterType Supports only BaseParameter");
                            }
                        }

                    }
                    catch (SessionExpiredException sessionExpiredException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex.Message);
                        throw new UserFriendlyException($"{parameter.ParameterName} - Failed [{ex.Message}] please rerun without it.");
                    }

                }
            }

            return new TableWidgetResponse { Items = responseItems };
        }

        private static void AddGaugeToResponse(List<TableWidgetResponseItem> responseItems, List<TableWidgetResponseItem> tableEventWidgetResponseItemList)
        {
            TableWidgetResponseItem tableWidgetResponseItem = tableEventWidgetResponseItemList[0];
            responseItems.Add(tableWidgetResponseItem);
            List<GaugeMarkerResultDto> gaugeMarkerDtoList = [];
            for (int i = 1; i < tableEventWidgetResponseItemList.Count; i++)
            {
                GaugeMarkerResultDto gaugeMarkerDto = new GaugeMarkerResultDto();
                gaugeMarkerDto.Value = tableEventWidgetResponseItemList[i].Calculated;
                gaugeMarkerDto.DataValueStatus = tableEventWidgetResponseItemList[i].DataValueStatus;

                gaugeMarkerDtoList.Add(gaugeMarkerDto);
            }
            tableWidgetResponseItem.GaugeMarkerResultList = gaugeMarkerDtoList;
        }

        #endregion

        //#region Gauge

        //public async Task<TableWidgetResponse> CalculateGaugeAsync(string url, string session, TableWidgetRequest input)
        //{
        //    var responseItems = new List<TableWidgetResponseItem>();
        //    var paramComponents = new List<BaseParameterComponent>();

        //    List<ColumnWidgetTable> eventColWidgetTableList = new List<ColumnWidgetTable>();
        //    List<ColumnWidgetTable> otherColWidgetTableList = new List<ColumnWidgetTable>();
        //    Dictionary<Guid, Dictionary<FiltersGroup, HashSet<BaseParameterComponent>>> baseParametersHashSet = new Dictionary<Guid, Dictionary<FiltersGroup, HashSet<BaseParameterComponent>>>();

        //    using (var mainLogger = PqbiStopwatch.AnchorAsync($"Tablo - {input.WidgetName} {nameof(CalculateTableAsync)}", Logger))
        //    {
        //        foreach (var parameter in input.ColumnWidgetTables)
        //        {
        //            TableWidgetParameterType widgetTableType = CalculationStaticTypes.GetTableWidgetParameterType(parameter.ParameterType);
        //            if (widgetTableType == TableWidgetParameterType.BaseParameter)
        //            {
        //                if (parameter.Markers != null)
        //                {
        //                    foreach (var marker in parameter.Markers)
        //                    {
        //                        PreparePrmMapForReq(input.StartDate, input.EndDate, true, input.Rows.Feeders, eventColWidgetTableList, otherColWidgetTableList, baseParametersHashSet, parameter, marker);
        //                    }
        //                }
        //                else
        //                    PreparePrmMapForReq(input.StartDate, input.EndDate, true, input.Rows.Feeders, eventColWidgetTableList, otherColWidgetTableList, baseParametersHashSet, parameter);
        //            }
        //            else if (widgetTableType == TableWidgetParameterType.Event)
        //            {
        //                if (parameter.Markers != null)
        //                {
        //                    foreach (var marker in parameter.Markers)
        //                    {
        //                        TableWidgetEvent widgetEvent = CloneEventWithQuantity(parameter.TableEvent, marker.Operation.ToString());
        //                        ColumnWidgetTable parameterDto = new ColumnWidgetTable
        //                        {
        //                            TableEvent = widgetEvent,
        //                            IsExcludeFlaggedData = parameter.IsExcludeFlaggedData,
        //                            ExcludeFlagged = parameter.ExcludeFlagged,
        //                            Normalize = parameter.Normalize,
        //                            NormalValue = parameter.NormalValue,
        //                            ParameterName = parameter.ParameterName,
        //                            ReplaceAggregationWith = parameter.ReplaceAggregationWith
        //                        };

        //                        PreparePrmMapForReq(input.StartDate, input.EndDate, true, input.Rows.Feeders, eventColWidgetTableList, otherColWidgetTableList, baseParametersHashSet, parameterDto);
        //                    }
        //                }
        //                else
        //                    PreparePrmMapForReq(input.StartDate, input.EndDate, true, input.Rows.Feeders, eventColWidgetTableList, otherColWidgetTableList, baseParametersHashSet, parameter);
        //            }
        //            else
        //                PreparePrmMapForReq(input.StartDate, input.EndDate, true, input.Rows.Feeders, eventColWidgetTableList, otherColWidgetTableList, baseParametersHashSet, parameter);
        //        }

        //        List<TableWidgetResponseItem> tableEventWidgetResponseItemList = null;
        //        if (eventColWidgetTableList.IsCollectionExists())
        //        {
        //            tableEventWidgetResponseItemList = await WidgetTableEventCalculationForCard(url, session, input, eventColWidgetTableList, input.StartDate, input.EndDate);
        //            responseItems.AddRange(tableEventWidgetResponseItemList);

        //            //tableEventWidgetResponseItemList = await WidgetTableEventCalculation(url, session, input, eventColWidgetTableList, input.StartDate, input.EndDate);                   
        //        }

        //        await LoadPrmToCache(url, session, input.StartDate, input.EndDate, baseParametersHashSet);

        //        foreach (var parameter in otherColWidgetTableList)
        //        {
        //            try
        //            {
        //                using (var sub = mainLogger.CreateSubLogger(parameter.ParameterName))
        //                {
        //                    TableWidgetParameterType widgetTableType = CalculationStaticTypes.GetTableWidgetParameterType(parameter.ParameterType);

        //                    AdvancedSettings advancedSettings = new AdvancedSettings(parameter.NormalValue, parameter.Normalize, parameter.IsExcludeFlaggedData, parameter.ExcludeFlagged, parameter.IgnoreAligningFunction, parameter.ReplaceAggregationWith);

        //                    switch (widgetTableType)
        //                    {
        //                        case TableWidgetParameterType.CustomParameter:

        //                            var items = await CustomParameterCreateTableNodeAsync(url, session, input, parameter, advancedSettings);
        //                            responseItems.AddRange(items);

        //                            break;

        //                        case TableWidgetParameterType.BaseParameter:

        //                            var baseParamaterItems = await BaseParameterCreateTableNodeAsync(url, session, input, parameter, advancedSettings);
        //                            responseItems.AddRange(baseParamaterItems);
        //                            break;

        //                        default:
        //                            throw new UserFriendlyException("TableWidgetParameterType Supports only BaseParameter");
        //                    }
        //                }

        //            }
        //            catch (SessionExpiredException sessionExpiredException)
        //            {
        //                throw;
        //            }
        //            catch (Exception ex)
        //            {
        //                Logger.LogError(ex.Message);
        //                throw new UserFriendlyException($"{parameter.ParameterName} - Failed [{ex.Message}] please rerun without it.");
        //            }

        //        }
        //    }

        //    return new TableWidgetResponse { Items = responseItems };
        //}

        //#endregion
        //private async Task<IEnumerable<TableWidgetResponseItem>> CustomParameterCreateTableNodeAsync(string url, string session, TableWidgetRequest input, ColumnWidgetTable parameter, AdvancedSettings? advancedSettings = null)
        //{
        //    var customParameterId = parameter.CustomData.Id;

        //    var customParameter = GetCustomParameter(customParameterId);
        //    CustomParameterTableValidate(input, customParameter);
        //    Func<IEnumerable<FeederComponentInfo>, bool, Task<IEnumerable<CustomParameterNodeCalculator>>> selector = async (feeders, isTag) =>
        //    {
        //        var nodes = await AssembleCustomParameterTree(customParameterId, url, session, input.StartDate, input.EndDate, -1, false, parameter.CustomData.Quantity, feeders, isTag, advancedSettings);
        //        //var nodes = await AssembleCustomParameterTree(customParameterId, url, session, input.StartDate, input.EndDate, -1, false, parameter.CustomData.Quantity, feeders);
        //        return [nodes];
        //    };

        //    var responseItems = await RealCalculateTableAsync(url, session, input, parameter, selector, parameter.CustomData.Quantity);
        //    return responseItems;
        //}   

        private CustomParameters.CustomParameter GetCustomParameter(int customParameterId)
        {
            lock (_customerParameterLocker)
            {
                return _customParameterRepository.Get(customParameterId);
            }
        }

        private async Task<IEnumerable<TableWidgetResponseItem>> BaseParameterCreateTableNodeAsync(string url, string session, TableWidgetRequest input, DateTime startDate, DateTime endDate, ColumnWidgetTable parameter, bool isFeedersAreShared, int resInSec, bool isExpectedSingleRes, AdvancedSettings? advancedSettings = null)
        {
            var baseParameter = JsonConvert.DeserializeObject<BaseParameter>(parameter.BaseData);
            //baseParameter.SetISXResolution(input.StartDate, input.EndDate);
            //    int totalSeconds = (int)(endDate - startDate).TotalSeconds;

            bool isNeedFilterByEvents = false;
            List<GaugeMarkerDto>? quantityForMarker = null;
            if (advancedSettings != null)
            {
                if (advancedSettings.IsExcludeFlaggedData == true)
                    isNeedFilterByEvents = true;

                if (advancedSettings.Markers != null)
                {
                    GaugeMarkerDto gaugeMarkerDto = advancedSettings.Markers.First();
                    baseParameter.Quantity = gaugeMarkerDto.Operation.ToString();
                    quantityForMarker = [gaugeMarkerDto];
                }
            }

            baseParameter.Resolution = resInSec;
            //if (!isNeedFilterByEvents)
            //    baseParameter.Resolution = (int)((endDate - input.StartDate).TotalSeconds);
            //else
            //    baseParameter.Resolution = (int)(new SyncInterval(IntervalSynchronized.IS1MIN).TimeIntervalInSec);

            var node = new CustomParameterNodeCalculator(CustomParameterType.BPCP, -1, false, string.Empty, startDate, endDate, -1, baseParameter.Quantity, advancedSettingsForTable: advancedSettings);
            node.IsSinglePointRes = isExpectedSingleRes;
            node.Markers = quantityForMarker;

            Logger.LogWarning(
                       $"BaseParameterCreateTableNodeAsync: startDate: {startDate}");

            Func<IEnumerable<FeederComponentInfo>, bool, Task<IEnumerable<CustomParameterNodeCalculator>>> selector = async (feeders, isTag) =>
            {
                var parameterComponents = baseParameter.CreateBaseParameterComponents(feeders);
                node.ClearMsrPrmCollection();
                node.PopulateWithBaseParameterComponents(parameterComponents);

                //SelectAssemble(node, feeders);

                await SendingAndStoringDataAsync(url, session, startDate, endDate, new Infrastructure.TimeZone(input.UserTimeZoneID, input.UserTimeZone), (false, null), parameterComponents, advancedSettings?.FiltersGroup, input.IsRealTime, input.RefreshRateInSeconds!.Value);

                return [node];
            };

            var responseItems = await RealCalculateTableAsync(url, session, input, parameter, selector, baseParameter.Quantity, isFeedersAreShared, CustomParameterType.BPCP);
            return responseItems;
        }
        //private async Task<IEnumerable<TableWidgetResponseItem>> RealCalculateTableAsync(string url, string session, TableWidgetRequest input, ColumnWidgetTable parameter, Func<IEnumerable<FeederComponentInfo>, bool, Task<IEnumerable<CustomParameterNodeCalculator>>> calculationSelector, string quantity, AdvancedSettings? advancedSettings = null)
        //{
        //    var responseItems = new List<TableWidgetResponseItem>();

        //    using (var mainLogger = PqbiStopwatch.AnchorAsync(nameof(RealCalculateTableAsync), Logger))
        //    {
        //        var feederMap = new Dictionary<FeederComponentInfo, GraphParametersComponentDtoV3?>();
        //        var customParameterType = CustomParameterType.BPCP;
        //        string outerAggFunction = null;

        //        //try
        //        {
        //            foreach (var feeder in input.Rows.Feeders)
        //            {
        //                var nodes = await calculationSelector([feeder], false);
        //                var node = nodes.First();
        //                outerAggFunction = node.OuterAggregationFunction;

        //                int? feederId = feeder.Id;

        //                if (node.CustomParameterType == CustomParameterType.BPCP)
        //                {
        //                    var bpComponent = node.BaseParameterComponents.First();
        //                    if (bpComponent.ParameterListItemType == ParameterListItemType.Channel)
        //                    {
        //                        feederId = null;
        //                    }
        //                }

        //                customParameterType = node.CustomParameterType;

        //                var graph = _engineControllerService.RootCalculation(node).First();
        //                //var graph = _engineControllerService.FullCalculation(node);


        //                if (graph.TryGetMissingParameterInfo(out var invalidParameter))
        //                {
        //                    mainLogger.LogError($"{invalidParameter.PropertyName} failed with PQZStatus = {invalidParameter.Status}");
        //                }

        //                //responseItems.AddRange(ArrangingForTable([graph], quantity, parameter.ParameterName));

        //                var responseItem = ArrangingForTable(graph.FirstValue(), feeder.ComponentId.ToString(), feederId, parameter.ParameterName, quantity, graph.DataUnitType, missingBaseParameterInfo: graph.MissingInformation?.FirstOrDefault());
        //                responseItems.Add(responseItem);
        //                feederMap[feeder] = graph;

        //            }
        //        }
        //        //catch (Exception ex)
        //        {
        //        }

        //        //try
        //        {

        //            string outerAggregationFunction = outerAggFunction!;
        //            if (advancedSettings != null)
        //                outerAggregationFunction = string.IsNullOrEmpty(advancedSettings.ReplaceOuterAggregationWith) ? outerAggregationFunction : advancedSettings.ReplaceOuterAggregationWith;

        //            foreach (var tag in input.Rows.Tags)
        //            {
        //                if (customParameterType == CustomParameterType.SPMC)
        //                {
        //                    var nodes = await calculationSelector(tag.Feeders, true);
        //                    var node = nodes.First();
        //                    var graphes = _engineControllerService.RootCalculation(node);

        //                    responseItems.AddRange(ArrangingForTable(graphes, quantity, parameter.ParameterName, tag.Id, tag.Name));
        //                }
        //                else
        //                {
        //                    string tagQuantity = quantity;
        //                    if (customParameterType == CustomParameterType.MPSC)
        //                        tagQuantity = outerAggregationFunction;

        //                    CalculateForMltiAndBaseParameter(feederMap, tag.Feeders, out var calculated, tagQuantity, out var missingBaseParameterInfo);
        //                    var responseItem = ArrangingForTable(calculated, null, null, parameter.ParameterName, quantity, new EmptyDataUnitType(), tag.Id, tag.Name, missingBaseParameterInfo: missingBaseParameterInfo);
        //                    responseItems.Add(responseItem);
        //                }
        //            }
        //        }
        //        //catch (Exception ex)
        //        {
        //        }
        //    }

        //    return responseItems;
        //}
        private async Task<IEnumerable<TResult>> CustomParameterCreateAsync<TResult, TReq, TPrm>(
      string url,
      string session,
      Infrastructure.TimeZone timeZoneInfo,
      DateTime startDateUTC,
      DateTime endDateUTC,
      TReq request,
      TPrm parameter,
      Func<TPrm, int> getCustomParameterId,
      Func<SelectorFunc, Task<IEnumerable<TResult>>> realCalculateFunc,
      Action<TReq, CustomParameters.CustomParameter?>? validate = null,
      IEnumerable<(DateTime barStart, DateTime barEnd)>? barTimeRangeList = null,
      AdvancedSettings? advanced = null)
        {
            var customParameterId = getCustomParameterId(parameter);

            // optional validation (table version uses it, bar version passes null)
            validate?.Invoke(request, GetCustomParameter(customParameterId));

            var selector = BuildSelector(
                customParameterId,
                url, session,
                startDateUTC,
                endDateUTC,
                //(request as dynamic).StartDate,   // Both request types expose these two props
                //(request as dynamic).EndDate,               
                timeZoneInfo,
                (parameter as dynamic).CustomData.Quantity,
                barTimeRangeList,
                advanced);

            // delegate jumps into the specialised calculation (bar / table)
            return await realCalculateFunc(selector);
        }

        private Func<IEnumerable<FeederComponentInfo>, bool,
                Task<IEnumerable<CustomParameterNodeCalculator>>> BuildSelector(
                    int customParameterId,
                    string url,
                    string session,
                    DateTime start,
                    DateTime end,                   
                    Infrastructure.TimeZone userTimeZoneInfo,
                    string quantity,
                    IEnumerable<(DateTime barStart, DateTime barEnd)> barTimeRangeList,
                    AdvancedSettings? advanced)
        {
            return async (feeders, isTag) =>
            {
                var node = await AssembleCustomParameterTree(
                    customParameterId, url, session,
                    start, end, userTimeZoneInfo,
                    widgetResolutionInSeconds: -1,
                    isAutoResolution: false,
                    quantity,
                    feeders,
                    barTimeRangeList,
                    isTag,
                    advanced);

                return new[] { node };
            };
        }

        private async Task<(List<TResult>, string?, CustomParameterType)> CalculatePerFeederAsync<TResult>(
               IEnumerable<FeederComponentInfo> feeders,
               Func<IEnumerable<FeederComponentInfo>, bool,
                    Task<IEnumerable<CustomParameterNodeCalculator>>> calcSelector,
               Func<CustomParameterNodeCalculator,            // node
                    IEnumerable<GraphParametersComponentDtoV3>,            // graph
                    FeederComponentInfo,
                    bool,
                    List<TResult>> resultSelector,
               bool isFeedersAreShared,
               Dictionary<FeederComponentInfo, GraphParametersComponentDtoV3?>? map,
               LoggerStopwatch logger,
               CustomParameterType customParameterType,
               AdvancedSettings? advancedSettings = null)
        {
            var list = new List<TResult>();
            string? outerAggFunction = null;

            if (isFeedersAreShared && (customParameterType == CustomParameterType.SPMC || customParameterType == CustomParameterType.Exception))
            {
                var node = (await calcSelector(feeders, false)).First();
                node.Markers = advancedSettings?.Markers;
                outerAggFunction = node.OuterAggregationFunction;
                customParameterType = node.CustomParameterType;
                var graph = _engineControllerService.RootCalculation(node);

                GraphParametersComponentDtoV3 firstGraph = graph.First();
                if (firstGraph.TryGetMissingParameterInfo(out var bad))
                    logger.LogError($"{bad.PropertyName} failed with PQZStatus = {bad.Status}");

                // Give caller a chance to build whatever object it needs
                list.AddRange(resultSelector(node, graph, feeders.Count() > 0 ? feeders.First() : null, isFeedersAreShared));
            }
            else
            {
                foreach (var feeder in feeders)
                {
                    var node = (await calcSelector([feeder], false)).First();
                    node.Markers = advancedSettings?.Markers;
                    outerAggFunction = node.OuterAggregationFunction;

                    node.Feeders = [feeder];
                    int? feederId = feeder.Id;

                    if (node.CustomParameterType == CustomParameterType.BPCP)
                    {
                        var bpComponent = node.BaseParameterComponents.First();
                        if (bpComponent.ParameterListItemType == ParameterListItemType.Channel)
                        {
                            feederId = null;
                        }
                    }

                    customParameterType = node.CustomParameterType;
                    var graph = _engineControllerService.RootCalculation(node);
                    GraphParametersComponentDtoV3 firstGraph = graph.First();
                    if (firstGraph.TryGetMissingParameterInfo(out var bad))
                        logger.LogError($"{bad.PropertyName} failed with PQZStatus = {bad.Status}");

                    // Give caller a chance to build whatever object it needs
                    list.AddRange(resultSelector(node, graph, feeder, isFeedersAreShared));

                    // Optional collector for the Table method
                    map?.Add(feeder, firstGraph);
                }
            }
            return (list, outerAggFunction, customParameterType);
        }

        private async Task<(List<TResult>, string?, CustomParameterType)> CalculatePerFeederAsync<TResult>(
                IEnumerable<FeederComponentInfo> feeders,
                Func<IEnumerable<FeederComponentInfo>, bool,
                     Task<IEnumerable<CustomParameterNodeCalculator>>> calcSelector,
                Func<CustomParameterNodeCalculator,            // node
                     IEnumerable<GraphParametersComponentDtoV3>,            // graph
                     FeederComponentInfo,                      // feeder
                     List<TResult>> resultSelector,
                CustomParameterType customParameterType,
                bool isFeedersAreShared,
                Dictionary<FeederComponentInfo, GraphParametersComponentDtoV3?>? map,
                LoggerStopwatch logger)
        {
            var list = new List<TResult>();
            string? outerAggFunction = null;

            if (isFeedersAreShared && customParameterType == CustomParameterType.SPMC)
            {
                var node = (await calcSelector(feeders, false)).First();
                outerAggFunction = node.OuterAggregationFunction;
                customParameterType = node.CustomParameterType;
                var graph = _engineControllerService.RootCalculation(node);

                GraphParametersComponentDtoV3 firstGraph = graph.First();
                if (firstGraph.TryGetMissingParameterInfo(out var bad))
                    logger.LogError($"{bad.PropertyName} failed with PQZStatus = {bad.Status}");

                // Give caller a chance to build whatever object it needs
                list.AddRange(resultSelector(node, graph, feeders.First()));
            }
            else
            {
                foreach (var feeder in feeders)
                {
                    var node = (await calcSelector([feeder], false)).First();
                    outerAggFunction = node.OuterAggregationFunction;

                    int? feederId = feeder.Id;

                    if (node.CustomParameterType == CustomParameterType.BPCP)
                    {
                        var bpComponent = node.BaseParameterComponents.First();
                        if (bpComponent.ParameterListItemType == ParameterListItemType.Channel)
                        {
                            feederId = null;
                        }
                    }

                    customParameterType = node.CustomParameterType;
                    var graph = _engineControllerService.RootCalculation(node);
                    GraphParametersComponentDtoV3 firstGraph = graph.First();
                    if (firstGraph.TryGetMissingParameterInfo(out var bad))
                        logger.LogError($"{bad.PropertyName} failed with PQZStatus = {bad.Status}");

                    // Give caller a chance to build whatever object it needs
                    list.AddRange(resultSelector(node, graph, feeder));

                    // Optional collector for the Table method
                    map?.Add(feeder, firstGraph);
                }
            }

            return (list, outerAggFunction, customParameterType);
        }

        bool CalculateForMltiAndBaseParameter(Dictionary<FeederComponentInfo, GraphParametersComponentDtoV3?> fMap, IEnumerable<FeederComponentInfo> list, out BasicValue calculated, string quantity, out MissingBaseParameterInfo? missingBaseParameterInfo)
        {
            missingBaseParameterInfo = null;
            var values = new List<BasicValue>();
            MissingBaseParameterInfo? missingParameterInfo = null;
            foreach (var feeder in list)
            {
                GraphParametersComponentDtoV3? graphInfo = fMap[feeder];
                if (graphInfo?.TryGetMissingParameterInfo(out missingParameterInfo) == false)
                {
                    //Valid
                    if (missingParameterInfo == null)
                        missingParameterInfo = new MissingBaseParameterInfo(graphInfo.ParameterNames.FirstOrDefault(), PQZStatus.OK, "");
                    missingBaseParameterInfo = missingParameterInfo;
                    var axisValue = fMap[feeder].FirstAxis();
                    values.Add(axisValue.ToBasicValue());
                }
            }

            if (missingBaseParameterInfo == null)
                missingBaseParameterInfo = missingParameterInfo;

            calculated = new BasicValue();
            if (values.IsCollectionEmpty() == false)
            {
                calculated = _engineControllerService.AggregationFunctionsAsync(quantity, values);
            }

            return missingBaseParameterInfo == null;
        }

        private async Task<List<TableWidgetResponseItem>> WidgetTableEventCalculation(string url, string session, TableWidgetRequest input, List<ColumnWidgetTable> eventColWidgetTableList, DateTime start, DateTime end)
        {
            Guid sessionID = Guid.Parse(session);

            PQZDateTime startDate = new PQZDateTime(start);
            PQZDateTime endDate = new PQZDateTime(end);

            var responseItems = new List<TableWidgetResponseItem>();
            string generatedByPQServer = GeneratedByEnum.PQServer.ToString();
            Dictionary<int, ColumnEventData> idToColEventData = new Dictionary<int, ColumnEventData>();

            Dictionary<string, Dictionary<FeederComponentInfo, List<PQEvent>>> columnToFeederEventsMap = new Dictionary<string, Dictionary<FeederComponentInfo, List<PQEvent>>>();
            Dictionary<string, Dictionary<FeederComponentInfo, double>> columnToFeederEventsResMap = new Dictionary<string, Dictionary<FeederComponentInfo, double>>();

            List<EventParameterDto> dtoList = eventColWidgetTableList
                .Select(c => new EventParameterDto
                {
                    TableEvent = c.TableEvent,
                    Normalize = c.Normalize,
                    NormalValue = c.NormalValue,
                    ParameterName = c.ParameterName,
                    ReplaceAggregationWith = c.ReplaceAggregationWith
                })
                .ToList();
            List<TableWidgetResponseItem> tableWidgetResponseItemList = new List<TableWidgetResponseItem>();

            List<TableWidgetResponseItem> feedersTableWidgetResponseItemList = await PopulateEventsValForFeedersInTable(url, dtoList, sessionID, startDate, endDate, input.Rows.Feeders, columnToFeederEventsMap, columnToFeederEventsResMap, input.Rows.Tags.Count, true);
            tableWidgetResponseItemList.AddRange(feedersTableWidgetResponseItemList);

            for (int tagNum = 0; tagNum < input.Rows.Tags.Count; tagNum++)
            {
                TagTableWidget tagTableWidget = input.Rows.Tags[tagNum];
                foreach (var colTable in eventColWidgetTableList)
                {
                    TableWidgetEvent tableWidgetEvent = colTable.TableEvent;
                    string colName = colTable.ParameterName;
                    double normalizeBy = 1;
                    if (colTable.Normalize == NormalizeEnum.VALUE)
                        normalizeBy = colTable.NormalValue ?? 1;

                    string tagQuantity = tableWidgetEvent.Quantity;
                    tagQuantity = string.IsNullOrEmpty(colTable.ReplaceAggregationWith) ? tagQuantity : colTable.ReplaceAggregationWith;
                    Enum.TryParse<PQBIQuantityType>(tagQuantity.ToLower(), out PQBIQuantityType quantityType);

                    double calculatedTagVal = 0;
                    if (quantityType == PQBIQuantityType.avg || quantityType == PQBIQuantityType.percentile)
                    {
                        List<PQEvent> colEvList = new List<PQEvent>();
                        for (int feederNum = 0; feederNum < tagTableWidget.Feeders.Count; feederNum++)
                        {
                            if (columnToFeederEventsMap.TryGetValue(colName, out var feederToEvListMap))
                            {
                                colEvList.AddRange(feederToEvListMap[tagTableWidget.Feeders[feederNum]]);
                            }
                        }
                        calculatedTagVal = Compute(colEvList, tableWidgetEvent.Parameter, quantityType, normalizeBy, colTable.Normalize);
                    }
                    else
                    {
                        List<double> colResList = new List<double>(tagTableWidget.Feeders.Count);
                        for (int feederNum = 0; feederNum < tagTableWidget.Feeders.Count; feederNum++)
                        {
                            if (columnToFeederEventsResMap.TryGetValue(colName, out var feederToColResListMap))
                            {
                                colResList.Add(feederToColResListMap[tagTableWidget.Feeders[feederNum]]);
                            }
                        }
                        calculatedTagVal = quantityType switch
                        {
                            PQBIQuantityType.min => colResList.Min(),
                            PQBIQuantityType.max => colResList.Max(),
                            PQBIQuantityType.count => colResList.Sum(),  // number of cells
                            _ => throw new ArgumentOutOfRangeException(nameof(quantityType))
                        };
                    }

                    DataUnitType dataUnitType = GetDataUnitType(colTable.Normalize == NormalizeEnum.VALUE, tableWidgetEvent, tableWidgetEvent.EventClass, quantityType);

                    Tag tag = new Tag();
                    //string tagID = tagTableWidget.Id;
                    //string tagName = tagTableWidget.Name;
                    tag.TagId = tagTableWidget.Id;
                    tag.TagValue = tagTableWidget.Name;
                    TableWidgetResponseItem tableResItem = new TableWidgetResponseItem()
                    {
                        Tag = tag,
                        Quantity = tableWidgetEvent.Quantity,
                        ParameterName = colName,
                        Calculated = calculatedTagVal,
                        DataUnitType = dataUnitType  // Example usage
                    };
                    tableWidgetResponseItemList.Add(tableResItem);
                }
            }

            return tableWidgetResponseItemList;
        }

        private async Task<List<TableWidgetResponseItem>> WidgetTableEventCalculationForCard(string url, string session, TableWidgetRequest input, List<ColumnWidgetTable> eventColWidgetTableList, DateTime start, DateTime end)
        {
            Guid sessionID = Guid.Parse(session);

            PQZDateTime startDate = new PQZDateTime(start);
            PQZDateTime endDate = new PQZDateTime(end);

            var responseItems = new List<TableWidgetResponseItem>();
            string generatedByPQServer = GeneratedByEnum.PQServer.ToString();
            Dictionary<int, ColumnEventData> idToColEventData = new Dictionary<int, ColumnEventData>();

            Dictionary<string, Dictionary<FeederComponentInfo, List<PQEvent>>> columnToFeederEventsMap = new Dictionary<string, Dictionary<FeederComponentInfo, List<PQEvent>>>();
            Dictionary<string, Dictionary<FeederComponentInfo, double>> columnToFeederEventsResMap = new Dictionary<string, Dictionary<FeederComponentInfo, double>>();


            //List<EventParameterDto> dtoList = new List<EventParameterDto>();
            //foreach (var colWidget in eventColWidgetTableList)
            //{
            //    if (colWidget.Markers != null)
            //    {
            //        foreach (var marker in colWidget.Markers)
            //        {
            //            TableWidgetEvent widgetEvent = CloneEventWithQuantity(colWidget.TableEvent, marker.Operation.ToString());
            //            EventParameterDto eventParameterDto = new EventParameterDto
            //            {
            //                TableEvent = widgetEvent,
            //                Normalize = colWidget.Normalize,
            //                NormalValue = colWidget.NormalValue,
            //                ParameterName = colWidget.ParameterName,
            //                ReplaceAggregationWith = colWidget.ReplaceAggregationWith
            //            };
            //            dtoList.Add(eventParameterDto);
            //        }
            //    }
            //    else
            //    {
            //        EventParameterDto eventParameterDto = new EventParameterDto
            //        {
            //            TableEvent = colWidget.TableEvent,
            //            Normalize = colWidget.Normalize,
            //            NormalValue = colWidget.NormalValue,
            //            ParameterName = colWidget.ParameterName,
            //            ReplaceAggregationWith = colWidget.ReplaceAggregationWith
            //        };
            //        dtoList.Add(eventParameterDto);
            //    }
            //}

            EnsureParameterNamesContainQuantities(eventColWidgetTableList);
            List<EventParameterDto> dtoList = eventColWidgetTableList
                .Select(c => new EventParameterDto
                {
                    TableEvent = c.TableEvent,
                    Normalize = c.Normalize,
                    NormalValue = c.NormalValue,
                    ParameterName = c.ParameterName,
                    ReplaceAggregationWith = c.ReplaceAggregationWith
                })
                .ToList();

            List<TableWidgetResponseItem> tableWidgetResponseItemList = new List<TableWidgetResponseItem>();

            if (input.Rows.Feeders.Count == 1)
            {
                List<TableWidgetResponseItem> feedersTableWidgetResponseItemList = await PopulateEventsValForFeedersInTable(url, dtoList, sessionID, startDate, endDate, input.Rows.Feeders, columnToFeederEventsMap, columnToFeederEventsResMap, 0, false);
                tableWidgetResponseItemList.AddRange(feedersTableWidgetResponseItemList);
            }
            else
            {
                List<TableWidgetResponseItem> feedersTableWidgetResponseItemList = await PopulateEventsValForFeedersInTable(url, dtoList, sessionID, startDate, endDate, input.Rows.Feeders, columnToFeederEventsMap, columnToFeederEventsResMap, 2, false);

                foreach (var colTable in eventColWidgetTableList)
                {
                    TableWidgetEvent tableWidgetEvent = colTable.TableEvent;
                    string colName = colTable.ParameterName;
                    double normalizeBy = 100;
                    if (colTable.Normalize == NormalizeEnum.VALUE)
                        normalizeBy = colTable.NormalValue ?? 100;

                    string tagQuantity = tableWidgetEvent.Quantity;
                    tagQuantity = string.IsNullOrEmpty(colTable.ReplaceAggregationWith) ? tagQuantity : colTable.ReplaceAggregationWith;
                    Enum.TryParse<PQBIQuantityType>(tagQuantity.ToLower(), out PQBIQuantityType quantityType);

                    double calculatedTagVal = 0;
                    if (quantityType == PQBIQuantityType.avg || quantityType == PQBIQuantityType.percentile)
                    {
                        List<PQEvent> colEvList = new List<PQEvent>();
                        if (columnToFeederEventsMap.TryGetValue(colName, out var feederToEvListMap))
                        {
                            foreach (var item in feederToEvListMap)
                            {
                                colEvList.AddRange(item.Value);
                            }
                        }

                        calculatedTagVal = Compute(colEvList, tableWidgetEvent.Parameter, quantityType, normalizeBy, colTable.Normalize);
                    }
                    else
                    {
                        List<double> colResList = new List<double>(columnToFeederEventsResMap.Values.Count);

                        if (columnToFeederEventsResMap.TryGetValue(colName, out var feederToEvListMap))
                        {
                            foreach (var item in feederToEvListMap)
                            {
                                colResList.Add(item.Value);
                            }
                        }

                        calculatedTagVal = quantityType switch
                        {
                            PQBIQuantityType.min => colResList.Min(),
                            PQBIQuantityType.max => colResList.Max(),
                            PQBIQuantityType.count => colResList.Sum(),  // number of cells
                            _ => throw new ArgumentOutOfRangeException(nameof(quantityType))
                        };

                    }

                    DataUnitType dataUnitType = GetDataUnitType(colTable.Normalize == NormalizeEnum.VALUE, tableWidgetEvent, tableWidgetEvent.EventClass, quantityType);

                    FeederComponentInfo feederComponentInfo = input.Rows.Feeders.First();
                    TableWidgetResponseItem tableResItem = new TableWidgetResponseItem()
                    {
                        ComponentId = feederComponentInfo.ComponentId.ToString(),
                        FeederId = feederComponentInfo.Id,
                        Quantity = tableWidgetEvent.Quantity,
                        ParameterName = feedersTableWidgetResponseItemList.First().ParameterName, //colName,
                        Calculated = calculatedTagVal,
                        DataUnitType = dataUnitType  // Example usage
                    };

                    tableWidgetResponseItemList.Add(tableResItem);
                }
            }

            return tableWidgetResponseItemList;
        }

        public static void EnsureParameterNamesContainQuantities(List<ColumnWidgetTable> columns)
        {
            if (columns == null) return;

            foreach (var column in columns)
            {
                if (column?.TableEvent == null || string.IsNullOrEmpty(column.TableEvent.Quantity))
                    continue;

                string quantity = column.TableEvent.Quantity;

                if (string.IsNullOrEmpty(column.ParameterName))
                {
                    column.ParameterName = quantity;
                }
                else if (!column.ParameterName.Contains(quantity, StringComparison.OrdinalIgnoreCase))
                {
                    column.ParameterName += $" {quantity.ToUpper()}";
                }
            }
        }


        private TableWidgetEvent CloneEventWithQuantity(TableWidgetEvent src, string quantity)
        {
            return new TableWidgetEvent
            {
                Phases = src.Phases != null ? new List<string>(src.Phases) : new List<string>(),
                EventId = src.EventId,
                EventClass = src.EventClass,
                IsShared = src.IsShared,
                Parameter = src.Parameter,
                IsPolyphase = src.IsPolyphase,
                AggregationInSeconds = src.AggregationInSeconds,
                Quantity = quantity
            };
        }

        //private async Task<List<TableWidgetResponseItem>> PopulateEventsValForFeedersInTable(string url, List<ColumnWidgetTable> eventColWidgetTableList, Guid sessionID, PQZDateTime startDate, PQZDateTime endDate, List<FeederComponentInfo> feederComponentInfoList, string tagID, string tagValue)
        private async Task<List<TableWidgetResponseItem>> PopulateEventsValForFeedersInTable(string url, List<EventParameterDto> eventColWidgetTableList, Guid sessionID, PQZDateTime startDate, PQZDateTime endDate, List<FeederComponentInfo> feederComponentInfoList, Dictionary<string, Dictionary<FeederComponentInfo, List<PQEvent>>> columnToFeederEventsMap, Dictionary<string, Dictionary<FeederComponentInfo, double>> columnToFeederEventsResMap, int tagCount, bool isHideMsrPointName, IEnumerable<(DateTime barStart, DateTime barEnd)>? barTimeRangeList = null, List<SubGroup>? subgroups = null)
        {
            if (eventColWidgetTableList.Count == 0)
                return new List<TableWidgetResponseItem>();

            if (barTimeRangeList == null)
                barTimeRangeList = [(startDate.DateTime, endDate.DateTime)];

            TableWidgetResponseItem tableResItem = null;
            Dictionary<string, List<PQEvent>> prmNameToEventsMap = new Dictionary<string, List<PQEvent>>();
            //Dictionary<string, Dictionary<string, List<PQEvent>>> compPrmNameToEventsMap = new Dictionary<string, Dictionary<string, List<PQEvent>>>();

            DataUnitType dataUnitType;

            List<TableWidgetResponseItem> tableWidgetResponseItemList = new List<TableWidgetResponseItem>();
            for (int feederNum = 0; feederNum < feederComponentInfoList.Count; feederNum++)
            {
                FeederComponentInfo feederComponentInfo = feederComponentInfoList[feederNum];
                //List<EventClass> eventClassEnumList = new List<EventClass>();
                //for (int colNum = 0; colNum < eventColWidgetTableList.Count; colNum++)
                //{
                //    ColumnWidgetTable columnWidgetTable = eventColWidgetTableList[colNum];

                //    if (!idToColEventData.TryGetValue(colNum, out ColumnEventData columnEventData))
                //    {
                //        columnEventData = null; // JsonConvert.DeserializeObject<ColumnEventData>(columnWidgetTable.EventData);
                //        idToColEventData.Add(colNum, columnEventData);
                //    }
                //    //EventClass eventClassEnum = (EventClass)columnEventData.Event.EventClass;

                //    //eventClassEnumList.Add(eventClassEnum);
                //}

                int feederID = feederComponentInfo.Id.Value;
                Guid feederCompID = feederComponentInfo.ComponentId;

                PQSRequest req = new PQSRequest(Guid.NewGuid(), sessionID);

                ConfigurationParameterBase complianceRunningConf = StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_COMPONENT_RUNNING_EVENTS);

                ConfigurationParameterBase systemElectricalConf = StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_COMPONENT_SYSTEM_ELECTRICAL_MAP_BY_TIME);

                //List<ConfigurationParameterBase> confBaseList = new List<ConfigurationParameterBase>();
                ////confBaseList.Add(complianceRunningConf);
                //confBaseList.Add(systemElectricalConf);

                GetInstantConfigurationRecord getInstantConfiguration = new GetInstantConfigurationRecord(feederComponentInfo.ComponentId, [complianceRunningConf]);
                GetBaseConfigurationRecord getBaseConfRec = new GetBaseConfigurationRecord(feederComponentInfo.ComponentId, startDate, endDate, [systemElectricalConf]);
                req.AddRecord(getInstantConfiguration);
                req.AddRecord(getBaseConfRec);
                var eventAndConfsResponse = await SendRecordsContainerPostBinaryRequestAndException(url, req);

                TopologyEnum topologyType = TopologyEnum.WYE;
                List<uint> runEventsIDList = null;
                foreach (var confRecBase in eventAndConfsResponse.GetRecords())
                {
                    if (confRecBase is BaseConfigurationRecord baseConfRec)
                    {
                        //BaseConfigurationRecord baseConfRec = recBase as BaseConfigurationRecord;
                        bool isFoundTopology = false;
                        foreach (KeyValuePair<PQZDateTime, ConfigurationParameterAndValueContainer> item in baseConfRec.TimeToConfigurationContainerDictionary)
                        {
                            if (item.Value.TryGetConfigurationValue<string>(systemElectricalConf, out string systemElectricalConfVal))
                            {
                                if (!string.IsNullOrEmpty(systemElectricalConfVal))
                                {
                                    SystemElectricalMappingByTime systemElectricalMappingXML = XMLSystemElectricalMappingUtils.ReadElectricalMappingMessage(systemElectricalConfVal);

                                    if (systemElectricalMappingXML.NetworkMapping.Count > 0)
                                    {
                                        (isFoundTopology, topologyType) = FindNetworkWithFeederID((uint)feederID, systemElectricalMappingXML.NetworkMapping);
                                        if (isFoundTopology)
                                            break;
                                    }

                                    if (systemElectricalMappingXML.FeedersWithoutNetworksByTime.Count > 0)
                                    {
                                        (isFoundTopology, topologyType) = FindFeederTopology((uint)feederID, systemElectricalMappingXML.FeedersWithoutNetworksByTime);
                                        if (isFoundTopology)
                                            break;
                                    }
                                }
                            }
                        }
                    }
                    else if (confRecBase is InstantConfigurationRecord instantConfRec)
                    {
                        //InstantConfigurationRecord instantConfRec = recBase as InstantConfigurationRecord;
                        instantConfRec.Configuration.TryGetConfigurationValue<ListValuesContainer<uint>>(complianceRunningConf, out ListValuesContainer<uint> runEventsID);
                        if (runEventsID != null)
                            runEventsIDList = runEventsID.ToList();
                    }
                }

                Dictionary<int, (EventClass, GeneratedByEnum)> confIDToGeneratedByMap = new Dictionary<int, (EventClass, GeneratedByEnum)>();

                for (int colNum = 0; colNum < eventColWidgetTableList.Count; colNum++)
                {
                    EventParameterDto columnWidgetTable = eventColWidgetTableList[colNum];
                    TableWidgetEvent tableWidgetEvent = columnWidgetTable.TableEvent;

                    if (runEventsIDList != null && runEventsIDList.Contains(tableWidgetEvent.EventId))
                    {
                        confIDToGeneratedByMap[(int)tableWidgetEvent.EventId] = (tableWidgetEvent.EventClass, GeneratedByEnum.PQServer);
                    }
                    else if (tableWidgetEvent.IsShared)
                    {
                        confIDToGeneratedByMap[(int)tableWidgetEvent.EventId] = (tableWidgetEvent.EventClass, GeneratedByEnum.MeasuringDevice);
                    }
                    else
                    {
                        confIDToGeneratedByMap[(int)tableWidgetEvent.EventId] = (tableWidgetEvent.EventClass, GeneratedByEnum.NotCalculated);
                    }
                }

                //GetEventGeneratedBy(Dictionary<int, (EventClass, bool)> eventTypeMap, List<uint> runningEvents);

                FiltersGroupContainer filtersGroupContainer = BuildEventFilter(confIDToGeneratedByMap);

                req = new PQSRequest(Guid.NewGuid(), sessionID);
                //FiltersGroupContainer filtersGroupContainer = GetPQEventsFilter(eventClassEnumList);
                GetEventsRecord getEventRec = new GetEventsRecord(feederComponentInfo.ComponentId, startDate.TicksPQZTimeFormat, endDate.TicksPQZTimeFormat, EventRequestTypeEnum.DETAILED_EVENT_STRUCTURE, 1000000, LimitTypeEnum.TIME_ASC, SegmentationTypeEnum.None, filtersGroupContainer);

                req.AddRecord(getEventRec);

#if DEBUG

                string xmlReq = PQZxmlWriter.WriteMessage(req, true);

#endif

                eventAndConfsResponse = await SendRecordsContainerPostBinaryRequestAndException(url, req);

                PQSRecordBase recBase = eventAndConfsResponse.GetRecord(0);
                UnitsEnum unitsEnum = UnitsEnum.STD_PERCENT;
                double calcValue = 0;
                if (recBase is EventsRecord)
                {
                    EventsRecord evRec = recBase as EventsRecord;

                    EventsContainer eventsContainer = evRec.GetEventsContainer();
                    ICollection<EventBase> eventBaseCollection = eventsContainer.GetAllEvents();

                    List<PQEvent> compPQEventList = eventBaseCollection.Cast<PQEvent>().ToList();

                    for (int colNum = 0; colNum < eventColWidgetTableList.Count; colNum++)
                    {
                        EventParameterDto columnWidgetTable = eventColWidgetTableList[colNum];
                        TableWidgetEvent tableWidgetEvent = columnWidgetTable.TableEvent;

                        string prmName = columnWidgetTable.ParameterName;
                        if (!isHideMsrPointName)
                        {
                            if (!string.IsNullOrEmpty(feederComponentInfo.CompName))
                                prmName = $"{feederComponentInfo.CompName}-{feederComponentInfo.Name} {columnWidgetTable.ParameterName}";
                            else
                                prmName = $"{feederComponentInfo.Name} {columnWidgetTable.ParameterName}";
                        }

                        uint confID = tableWidgetEvent.EventId;
                        EventClass eventClassEnum = tableWidgetEvent.EventClass;
                        int eventConfID = (int)confID;
                        (EventClass, GeneratedByEnum) classGeneratedByTuple;
                        confIDToGeneratedByMap.TryGetValue(eventConfID, out classGeneratedByTuple);
                        GeneratedByEnum generatedByEnum = classGeneratedByTuple.Item2;
                        Enum.TryParse<PQBIQuantityType>(tableWidgetEvent.Quantity.ToLower(), out PQBIQuantityType quantityType);

                        dataUnitType = GetDataUnitType(columnWidgetTable.Normalize == NormalizeEnum.VALUE, tableWidgetEvent, tableWidgetEvent.EventClass, quantityType);

                        if (generatedByEnum == GeneratedByEnum.NotCalculated)
                        {
                            foreach (var timeRange in barTimeRangeList)
                            {
                                PrepareEventDataForTagCalculation(columnToFeederEventsMap, columnToFeederEventsResMap, tagCount, feederComponentInfo, 0, columnWidgetTable, new List<PQEvent>(), quantityType);

                                if (subgroups == null)
                                {
                                    tableResItem = new TableWidgetResponseItem()
                                    {
                                        FeederId = feederID,
                                        ComponentId = feederCompID.ToString(),
                                        ParameterName = prmName,
                                        Quantity = tableWidgetEvent.Quantity,
                                        //DataUnitType = new EmptyDataUnitType(),
                                        DataUnitType = dataUnitType,
                                        Calculated = 0
                                    };

                                    tableWidgetResponseItemList.Add(tableResItem);
                                }
                                else
                                {
                                    foreach (var subGroup in subgroups)
                                    {
                                        tableResItem = new TableWidgetResponseItem()
                                        {
                                            FeederId = feederID,
                                            ComponentId = feederCompID.ToString(),
                                            ParameterName = prmName,
                                            Quantity = tableWidgetEvent.Quantity,
                                            //DataUnitType = new EmptyDataUnitType(),
                                            DataUnitType = dataUnitType,
                                            Calculated = 0
                                        };

                                        tableWidgetResponseItemList.Add(tableResItem);
                                    }
                                }
                            }
                            continue;
                        }

                        foreach (var timeRange in barTimeRangeList)
                        {
                            List<PQEvent> pqEventList = new List<PQEvent>();

                            if (subgroups != null)
                            {
                                Func<PQEvent, double> selector = tableWidgetEvent.Parameter switch
                                {
                                    WidgetTableParameterType.Deviation => e => e.Deviation,
                                    WidgetTableParameterType.Value => e => e.Value,
                                    WidgetTableParameterType.Duration => e => e.Duration.TotalSeconds,
                                    _ => throw new ArgumentOutOfRangeException(nameof(tableWidgetEvent.Parameter))
                                };
                                foreach (PQEvent pqEvent in compPQEventList)
                                {

                                    bool isEventBelongToMsrPoint = IsEventBelongToMsrPoint(feederID, topologyType, tableWidgetEvent, eventClassEnum, eventConfID, generatedByEnum, pqEvent);
                                    if (isEventBelongToMsrPoint)
                                        pqEventList.Add(pqEvent);
                                }

                                foreach (var subGroup in subgroups)
                                {
                                    List<PQEvent> subGroupEventList = new List<PQEvent>();
                                    foreach (var item in pqEventList)
                                    {
                                        var pqVal = selector(item);
                                        if (subGroup.FromVal <= pqVal && pqVal <= subGroup.ToVal)
                                            subGroupEventList.Add(item);
                                    }
                                    AggragateAndComputeEventVal(columnToFeederEventsMap, columnToFeederEventsResMap, tagCount, out tableResItem, dataUnitType, tableWidgetResponseItemList, feederComponentInfo, feederID, feederCompID, prmName, out calcValue, columnWidgetTable, tableWidgetEvent, quantityType, subGroupEventList);
                                }

                                //List<PQEvent> pqEventList = compPQEventList
                                //                    .Where(pqEvent => IsEventBelongToMsrPoint(
                                //                                          feederID, topologyType, tableWidgetEvent,
                                //                                          eventClassEnum, eventConfID, generatedByEnum, pqEvent))
                                //                    .Select(pqEvent => new { pqEvent, Value = selector(pqEvent) }) // compute once
                                //                    .Where(x => sg.FromVal <= x.Value && x.Value <= sg.ToVal))
                                //                    .Select(x => x.pqEvent)
                                //                    .Distinct() // just in case ranges overlap
                                //            ).ToList();


                            }
                            else
                            {
                                if (barTimeRangeList.Count() == 1)
                                {
                                    foreach (PQEvent pqEvent in compPQEventList)
                                    {
                                        bool isEventBelongToMsrPoint = IsEventBelongToMsrPoint(feederID, topologyType, tableWidgetEvent, eventClassEnum, eventConfID, generatedByEnum, pqEvent);
                                        if (isEventBelongToMsrPoint)
                                            pqEventList.Add(pqEvent);
                                    }
                                }
                                else
                                {
                                    foreach (PQEvent pqEvent in compPQEventList)
                                    {
                                        if (timeRange.barEnd < pqEvent.StartTime.DateTime)
                                            break;

                                        if (timeRange.barStart < pqEvent.EndTime.DateTime && pqEvent.StartTime.DateTime < timeRange.barEnd)
                                        {
                                            bool isEventBelongToMsrPoint = IsEventBelongToMsrPoint(feederID, topologyType, tableWidgetEvent, eventClassEnum, eventConfID, generatedByEnum, pqEvent);
                                            if (isEventBelongToMsrPoint)
                                                pqEventList.Add(pqEvent);
                                        }
                                    }
                                }

                                //bool isUsePQServerGeneratedEvents = false;
                                //foreach (var pqEvent in pqEventList)
                                //{
                                //    if (pqEvent.GeneratedBy == generatedByPQServer)
                                //    {
                                //        isUsePQServerGeneratedEvents = true;
                                //        break;
                                //    }
                                //}

                                //if (isUsePQServerGeneratedEvents)
                                //{
                                //    pqEventList = new List<PQEvent>();
                                //    foreach (var pqEvent in pqEventList)
                                //    {
                                //        if (pqEvent.GeneratedBy == generatedByPQServer)
                                //        {
                                //            pqEventList.Add(pqEvent);
                                //        }
                                //    }
                                //}

                                AggragateAndComputeEventVal(columnToFeederEventsMap, columnToFeederEventsResMap, tagCount, out tableResItem, dataUnitType, tableWidgetResponseItemList, feederComponentInfo, feederID, feederCompID, prmName, out calcValue, columnWidgetTable, tableWidgetEvent, quantityType, pqEventList);
                            }
                        }
                    }
                }


            }

            //foreach (KeyValuePair<string, Dictionary<string, List<PQEvent>>> compToColAndEvPair in compPrmNameToEventsMap)
            //{
            //    string compID = compToColAndEvPair.Key;
            //    Dictionary<string, List<PQEvent>> colToEvMap = compToColAndEvPair.Value;
            //    for (int colNum = 0; colNum < eventColWidgetTableList.Count; colNum++)
            //    {
            //        ColumnWidgetTable columnWidgetTable = eventColWidgetTableList[colNum];
            //        TableWidgetEvent tableWidgetEvent = columnWidgetTable.TableEvent;

            //        List<PQEvent> pqEventList = null;
            //        if (!colToEvMap.TryGetValue(columnWidgetTable.ParameterName, out pqEventList))
            //            pqEventList = new List<PQEvent>();

            //        Enum.TryParse<PQBIQuantityType>(tableWidgetEvent.Quantity, out PQBIQuantityType quantityType);

            //        double value = Compute(pqEventList, tableWidgetEvent.Parameter, quantityType);


            //        tableResItem = new TableWidgetResponseItem()
            //        {                       
            //            ComponentId = compID,
            //            ParameterName = columnWidgetTable.ParameterName,
            //            Quantity = tableWidgetEvent.Quantity,
            //            Calculated = value,
            //            DataUnitType = new EmptyDataUnitType()
            //        };
            //        tableWidgetResponseItemList.Add(tableResItem);
            //    }
            //}



            //if (!string.IsNullOrEmpty(tagID))
            //{
            //    for (int colNum = 0; colNum < eventColWidgetTableList.Count; colNum++)
            //    {
            //        ColumnWidgetTable columnWidgetTable = eventColWidgetTableList[colNum];
            //        TableWidgetEvent tableWidgetEvent = columnWidgetTable.TableEvent;

            //        List<PQEvent> pqEventList = null;
            //        if (!prmNameToEventsMap.TryGetValue(columnWidgetTable.ParameterName, out pqEventList))
            //            pqEventList = new List<PQEvent>();

            //        Enum.TryParse<PQBIQuantityType>(tableWidgetEvent.Quantity.ToLower(), out PQBIQuantityType quantityType);

            //        double value = Compute(pqEventList, tableWidgetEvent.Parameter, quantityType);

            //        Tag tag = new Tag();
            //        tag.TagId = tagID;
            //        tag.TagValue = tagValue;
            //        tableResItem = new TableWidgetResponseItem()
            //        {
            //            Tag = tag,
            //            Quantity = tableWidgetEvent.Quantity,
            //            ParameterName = columnWidgetTable.ParameterName,
            //            Calculated = value,
            //            DataUnitType = new EmptyDataUnitType()  // Example usage
            //        };
            //        tableWidgetResponseItemList.Add(tableResItem);
            //    }
            //}

            return tableWidgetResponseItemList;
        }

        private void AggragateAndComputeEventVal(Dictionary<string, Dictionary<FeederComponentInfo, List<PQEvent>>> columnToFeederEventsMap, Dictionary<string, Dictionary<FeederComponentInfo, double>> columnToFeederEventsResMap, int tagCount, out TableWidgetResponseItem tableResItem, DataUnitType dataUnitType, List<TableWidgetResponseItem> tableWidgetResponseItemList, FeederComponentInfo feederComponentInfo, int feederID, Guid feederCompID,
          string prmName, out double calcValue, EventParameterDto columnWidgetTable, TableWidgetEvent tableWidgetEvent, PQBIQuantityType quantityType, List<PQEvent> pqEventList)
        {
            PQZTimeSpan timeSpan = PQZTimeSpan.Zero;
            if (tableWidgetEvent.AggregationInSeconds != null && tableWidgetEvent.AggregationInSeconds != 0)
                timeSpan = PQZTimeSpan.FromSeconds((double)tableWidgetEvent.AggregationInSeconds);

            AggregateEvents(pqEventList, timeSpan);

            double normalizeBy = 1;
            if (columnWidgetTable.Normalize == NormalizeEnum.VALUE)
                normalizeBy = columnWidgetTable.NormalValue ?? 1;

            string tagQuantity = tableWidgetEvent.Quantity;
            tagQuantity = string.IsNullOrEmpty(columnWidgetTable.ReplaceAggregationWith) ? tagQuantity : columnWidgetTable.ReplaceAggregationWith;
            Enum.TryParse<PQBIQuantityType>(tagQuantity.ToLower(), out PQBIQuantityType tagQuantityType);

            calcValue = Compute(pqEventList, tableWidgetEvent.Parameter, quantityType, normalizeBy, columnWidgetTable.Normalize);
            PrepareEventDataForTagCalculation(columnToFeederEventsMap, columnToFeederEventsResMap, tagCount, feederComponentInfo, calcValue, columnWidgetTable, pqEventList, tagQuantityType);
            tableResItem = new TableWidgetResponseItem()
            {
                ComponentId = feederCompID.ToString(),
                FeederId = feederID,
                ParameterName = prmName,
                Quantity = tableWidgetEvent.Quantity,
                Calculated = calcValue,
                DataUnitType = dataUnitType
            };
            tableWidgetResponseItemList.Add(tableResItem);
        }

        private DataUnitType GetDataUnitType(bool isNormalized, TableWidgetEvent tableWidgetEvent, EventClass eventClassEnum, PQBIQuantityType quantityType)
        {
            UnitsEnum unitsEnum = UnitsEnum.STD_DB;
            DataUnitType dataUnitType;

            if (isNormalized)
            {
                unitsEnum = UnitsEnum.STD_PERCENT;
            }
            else
            {
                if (quantityType != PQBIQuantityType.count)
                {
                    switch (tableWidgetEvent.Parameter)
                    {
                        case WidgetTableParameterType.Deviation:
                            unitsEnum = UnitsEnum.STD_PERCENT;
                            break;
                        case WidgetTableParameterType.Duration:
                            unitsEnum = UnitsEnum.STD_SECONDS;
                            break;
                        case WidgetTableParameterType.Value:
                            switch (eventClassEnum)
                            {
                                case EventClass.EVENT_CLASSIFICATION_FREQUENCY:
                                    unitsEnum = UnitsEnum.STD_HERTZ;
                                    break;
                                case EventClass.EVENT_CLASSIFICATION_INRUSH_CURRENT:
                                case EventClass.EVENT_CLASSIFICATION_LONG_CURRENT_HARMONIC_DISTORTION:
                                case EventClass.EVENT_CLASSIFICATION_LONG_CURRENT_TDD:
                                case EventClass.EVENT_CLASSIFICATION_LONG_CURRENT_THD:
                                case EventClass.EVENT_CLASSIFICATION_SHORT_CURRENT_HARMONIC_DISTORTION:
                                case EventClass.EVENT_CLASSIFICATION_SHORT_CURRENT_TDD:
                                case EventClass.EVENT_CLASSIFICATION_SHORT_CURRENT_THD:
                                    unitsEnum = UnitsEnum.STD_AMP;
                                    break;
                                default:
                                    unitsEnum = UnitsEnum.STD_VOLT;
                                    break;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            if (unitsEnum != UnitsEnum.STD_DB)
            {
                var token = UnitsEnumHelper.GetLocalizedDescriptionKey(unitsEnum);
                dataUnitType = new DataUnitType((int)unitsEnum, token);
            }
            else
            {
                dataUnitType = new EmptyDataUnitType();
            }
            return dataUnitType;
        }

        private bool IsEventBelongToMsrPoint(int feederID, TopologyEnum topologyType, TableWidgetEvent tableWidgetEvent, EventClass eventClassEnum, int eventConfID, GeneratedByEnum generatedByEnum, PQEvent pqEvent)
        {
            if (pqEvent.Class != eventClassEnum)
                return false;

            if (generatedByEnum == GeneratedByEnum.PQServer)
            {
                if (pqEvent.GeneratedBy != GeneratedByEnum.PQServer.ToString())
                    return false;
                else if (pqEvent.ConfigurationID != eventConfID)
                    return false;
            }
            else if (pqEvent.GeneratedBy == GeneratedByEnum.PQServer.ToString())
                return false;

            bool isEventInFeederNetwork = IsEventInFeederNetwork(pqEvent, (uint)feederID, 600);
            if (!isEventInFeederNetwork)
                return false;

            if (!IsEventInPhase(pqEvent, topologyType, tableWidgetEvent.Phases))
                return false;

            return true;
        }


        public static double Compute(
                   IReadOnlyCollection<PQEvent> events,
                   WidgetTableParameterType parameter,
                   PQBIQuantityType quantity,
                   double normalizeBy,
                   NormalizeEnum normalizeEnum,
                   double percentileRank = 95)
        {
            if (events == null)
                throw new ArgumentException("No events supplied.", nameof(events));
            if (events.Count == 0)
                return 0;

            // 1. Pick the field we’re interested in and project it to double
            Func<PQEvent, double> selector = parameter switch
            {
                WidgetTableParameterType.Deviation => e => e.Deviation,
                WidgetTableParameterType.Value => e => e.Value,
                WidgetTableParameterType.Duration => e => e.Duration.TotalSeconds,
                _ => throw new ArgumentOutOfRangeException(nameof(parameter))
            };

            //var data = events.Select(selector).OrderBy(x => x).ToArray();  // sorted once, cheap
            //var data = events.Select(selector);

            // 2. Aggregate
            double res = quantity switch
            {
                PQBIQuantityType.min => events.Min(selector),
                PQBIQuantityType.max => events.Max(selector),
                PQBIQuantityType.avg => events.Average(selector),
                PQBIQuantityType.count => events.Count,
                //QuantityType.Percentile => Percentile(data, percentileRank),
                _ => throw new ArgumentOutOfRangeException(nameof(quantity))
            };

            if (normalizeEnum == NormalizeEnum.VALUE)
                return (res / normalizeBy) * 100;
            return res;
        }

        //private DataUnitType GetEventDataUnit(EventClass eventType, PQBIQuantityType quantityType)
        //{
        //    switch (quantityType)
        //    {
        //        case PQBIQuantityType.min:                    
        //        case PQBIQuantityType.max:                  
        //        case PQBIQuantityType.average:                  
        //        case PQBIQuantityType.percentile:
        //            {
        //                switch (eventType)
        //                {

        //                    case EventClass.EVENT_CLASSIFICATION_DIP:
        //                    case EventClass.EVENT_CLASSIFICATION_SWELL:
        //                    case EventClass.EVENT_CLASSIFICATION_INTERRUPTION:

        //                    default:
        //                }
        //            }
        //            break;
        //        case PQBIQuantityType.count:
        //            break;
        //        default:
        //            break;
        //    }

        //    UnitsEnum units;

        //    var unitState = UnitsUtility.GetUnitsFromGroupAndPhase(networkFeederParam.Group, networkFeederParam.Phase);
        //    var token = UnitsEnumHelper.GetLocalizedDescriptionKey(unitState);
        //    dataUnitType = new DataUnitType((int)unitState, token);
        //}

        public (bool, TopologyEnum) FindNetworkWithFeederID(uint feederNum, Dictionary<uint, NetworkMappingSortedByTime> NetworkMapping)
        {
            foreach (var netMap in NetworkMapping)
            {
                (bool isFoundTopology, TopologyEnum topologyType) = FindFeederTopology(feederNum, netMap.Value.FeederCollection);
                if (isFoundTopology)
                    return (isFoundTopology, topologyType);
            }

            return (false, TopologyEnum.WYE);
        }

        private static (bool, TopologyEnum) FindFeederTopology(uint feederNum, Dictionary<uint, FeederMappingSortedByTime> feederMappings)
        {
            if (feederMappings.TryGetValue(feederNum, out FeederMappingSortedByTime feederMap))
            {
                for (int i = 0; i < feederMap.SortedFeederMapList.Count; i++)
                {
                    MappingWithTimes mappingWithTimes = feederMap.SortedFeederMapList[i];
                    FeederMap feedMap = mappingWithTimes.Mapping as FeederMap;

                    return (true, feedMap.FeederTopology);
                }
            }
            return (false, TopologyEnum.WYE);
        }



        public static bool IsEventInPhase(PQEvent curEvent, TopologyEnum complianceTopologyEnum, List<string> phaseSet)
        {
            bool isEventInPhase = false;
            EventPhases eventVoltPhases = curEvent.VoltagePhases;
            EventPhases eventCurrentPhases = curEvent.CurrentPhases;

            switch (complianceTopologyEnum)
            {
                case TopologyEnum.TRSPLIT_LLN:
                case TopologyEnum.WYE:
                    {
                        //var result = (EventsPhasesEnum)0;
                        HashSet<EventsPhasesEnum> eventPhaseSet = new HashSet<EventsPhasesEnum>();
                        foreach (var phase in phaseSet)
                        {
                            switch (phase)
                            {
                                case "L1":
                                    eventPhaseSet.Add(EventsPhasesEnum.PH1);
                                    break;
                                case "L2":
                                    eventPhaseSet.Add(EventsPhasesEnum.PH2);
                                    break;
                                case "L3":
                                    eventPhaseSet.Add(EventsPhasesEnum.PH3);
                                    break;
                                default:
                                    break;
                            }
                        }

                        if (eventVoltPhases != null)
                        {
                            var intersection = eventVoltPhases.NamePhases.Intersect(eventPhaseSet).ToHashSet();
                            if (intersection.Count > 0)
                                isEventInPhase = true;

                            //foreach (EventsPhasesEnum eventsPhasesEnum in eventVoltPhases.NamePhases)
                            //{
                            //    if (eventsPhasesEnum == EventsPhasesEnum.PH1 || eventsPhasesEnum == EventsPhasesEnum.PH2 || eventsPhasesEnum == EventsPhasesEnum.PH3)
                            //        isEventInPhase = true;
                            //}
                        }
                        if (eventCurrentPhases != null && !isEventInPhase)
                        {
                            var intersection = eventCurrentPhases.NamePhases.Intersect(eventPhaseSet).ToHashSet();
                            if (intersection.Count > 0)
                                isEventInPhase = true;

                            //foreach (EventsPhasesEnum eventsPhasesEnum in eventCurrentPhases.NamePhases)
                            //{
                            //    if (eventsPhasesEnum == EventsPhasesEnum.PH1 || eventsPhasesEnum == EventsPhasesEnum.PH2 || eventsPhasesEnum == EventsPhasesEnum.PH3)
                            //        isEventInPhase = true;
                            //}
                        }
                        return isEventInPhase;
                    }
                case TopologyEnum.DELTA:
                    {
                        HashSet<EventsPhasesEnum> eventPhaseSet = new HashSet<EventsPhasesEnum>();
                        foreach (var phase in phaseSet)
                        {
                            switch (phase)
                            {
                                case "L1":
                                    eventPhaseSet.Add(EventsPhasesEnum.PH12);
                                    break;
                                case "L2":
                                    eventPhaseSet.Add(EventsPhasesEnum.PH23);
                                    break;
                                case "L3":
                                    eventPhaseSet.Add(EventsPhasesEnum.PH31);
                                    break;
                                default:
                                    break;
                            }
                        }

                        if (eventVoltPhases != null)
                        {
                            var intersection = eventVoltPhases.NamePhases.Intersect(eventPhaseSet).ToHashSet();
                            if (intersection.Count > 0)
                                isEventInPhase = true;
                        }
                        if (eventCurrentPhases != null && !isEventInPhase)
                        {
                            var intersection = eventCurrentPhases.NamePhases.Intersect(eventPhaseSet).ToHashSet();
                            if (intersection.Count > 0)
                                isEventInPhase = true;
                        }
                    }
                    return isEventInPhase;
                case TopologyEnum.SINGLE_LN:
                    {
                        HashSet<EventsPhasesEnum> eventPhaseSet = new HashSet<EventsPhasesEnum>();
                        foreach (var phase in phaseSet)
                        {
                            switch (phase)
                            {
                                case "L1":
                                    eventPhaseSet.Add(EventsPhasesEnum.PH1);
                                    break;
                                default:
                                    break;
                            }
                        }
                        if (eventVoltPhases != null)
                        {
                            var intersection = eventVoltPhases.NamePhases.Intersect(eventPhaseSet).ToHashSet();
                            if (intersection.Count > 0)
                                isEventInPhase = true;
                        }
                        if (eventCurrentPhases != null && !isEventInPhase)
                        {
                            var intersection = eventCurrentPhases.NamePhases.Intersect(eventPhaseSet).ToHashSet();
                            if (intersection.Count > 0)
                                isEventInPhase = true;
                        }
                        return isEventInPhase;
                    }
                case TopologyEnum.SINGLE_LL:
                    {
                        HashSet<EventsPhasesEnum> eventPhaseSet = new HashSet<EventsPhasesEnum>();
                        foreach (var phase in phaseSet)
                        {
                            switch (phase)
                            {
                                case "L1":
                                    eventPhaseSet.Add(EventsPhasesEnum.PH12);
                                    break;
                                default:
                                    break;
                            }
                        }
                        if (eventVoltPhases != null)
                        {
                            var intersection = eventVoltPhases.NamePhases.Intersect(eventPhaseSet).ToHashSet();
                            if (intersection.Count > 0)
                                isEventInPhase = true;
                        }
                        if (eventCurrentPhases != null && !isEventInPhase)
                        {
                            var intersection = eventCurrentPhases.NamePhases.Intersect(eventPhaseSet).ToHashSet();
                            if (intersection.Count > 0)
                                isEventInPhase = true;
                        }
                        return isEventInPhase;
                    }
                default:
                    break;
            }
            return false;
        }

        internal static FiltersGroupContainer BuildEventFilter(Dictionary<int, (EventClass, GeneratedByEnum)> eventClassToGeneratedByMap)
        {
            FiltersGroupContainer filtersGroupContainer = new FiltersGroupContainer();
            foreach (KeyValuePair<int, (EventClass, GeneratedByEnum)> eventClassToGeneratedByPair in eventClassToGeneratedByMap)
            {
                int confID = eventClassToGeneratedByPair.Key;
                (EventClass eventClass, GeneratedByEnum generatedByEnum) = eventClassToGeneratedByPair.Value;

                if (generatedByEnum == GeneratedByEnum.PQServer)
                {
                    ClassFilter classFilter = new ClassFilter();
                    classFilter.AddSingleValue(eventClass);
                    GeneratedByFilter generatedByFilter = new GeneratedByFilter();
                    generatedByFilter.AddSingleValue(generatedByEnum.ToString());

                    IsAggregatedFilter isAggregatedFilter = new IsAggregatedFilter();
                    isAggregatedFilter.isAggregated = false;

                    ConfigIDFilter configIDFilter = new ConfigIDFilter();
                    configIDFilter.AddSingleValue((int)confID);

                    FiltersGroup filtersGroup = new FiltersGroup();
                    filtersGroup.AddFilter(classFilter);
                    filtersGroup.AddFilter(generatedByFilter);
                    filtersGroup.AddFilter(configIDFilter);
                    filtersGroup.AddFilter(isAggregatedFilter);
                    filtersGroupContainer.FilterGroups.Add(filtersGroup);
                }
                else if (generatedByEnum == GeneratedByEnum.MeasuringDevice)
                {
                    ClassFilter classFilter = new ClassFilter();
                    classFilter.AddSingleValue(eventClass);

                    IsAggregatedFilter isAggregatedFilter = new IsAggregatedFilter();
                    isAggregatedFilter.isAggregated = false;

                    FiltersGroup filtersGroup = new FiltersGroup();
                    filtersGroup.AddFilter(classFilter);
                    filtersGroup.AddFilter(isAggregatedFilter);
                    filtersGroupContainer.FilterGroups.Add(filtersGroup);
                }
                else   //Not calculated
                {

                }
            }
            return filtersGroupContainer;
        }

        private static bool IsEventInFeederNetwork(PQEvent curEvent, uint feeder, uint network)
        {
            ///If event has same network as measurement point we need to check feeder, if feeder is also same or there is no feeder at all (all feeders are 0) the event belong to the measurement point, if it has same feeder we do not have to check network and it is also belong to the measurement point. 
            if (curEvent.Networks.Contains(network))
            {
                if (!curEvent.Feeders.Contains(feeder))
                {
                    bool isAllFeedersAreZero = true;
                    foreach (uint feederID in curEvent.Feeders)
                    {
                        if (feederID != 0)
                        {
                            isAllFeedersAreZero = false;
                            break;
                        }
                    }
                    if (!isAllFeedersAreZero)
                        return false;
                }
            }
            else if (!curEvent.Feeders.Contains(feeder))
                return false;
            return true;
        }

        private static bool IsEventPhase(PQEvent curEvent, List<string> phaseList)
        {
            bool isEventInPhase = false;
            EventPhases eventVoltPhases = curEvent.VoltagePhases;
            EventPhases eventCurrentPhases = curEvent.CurrentPhases;
            if (eventVoltPhases != null)
            {
                isEventInPhase = IsEventPhase(phaseList, eventVoltPhases);
            }

            if (isEventInPhase)
                return true;

            if (eventCurrentPhases != null)
            {
                isEventInPhase = IsEventPhase(phaseList, eventCurrentPhases);
            }

            return isEventInPhase;
        }

        private static bool IsEventPhase(IEnumerable<string> phaseList, EventPhases eventVoltPhases)
        {
            foreach (EventsPhasesEnum eventsPhasesEnum in eventVoltPhases.NamePhases)
            {
                if (eventsPhasesEnum == EventsPhasesEnum.PH1 || eventsPhasesEnum == EventsPhasesEnum.PH12)
                {
                    if (phaseList.Contains("L1"))
                        return true;
                }
                else if (eventsPhasesEnum == EventsPhasesEnum.PH2 || eventsPhasesEnum == EventsPhasesEnum.PH23)
                {
                    if (phaseList.Contains("L2"))
                        return true;
                }
                else if (eventsPhasesEnum == EventsPhasesEnum.PH3 || eventsPhasesEnum == EventsPhasesEnum.PH31)
                {
                    if (phaseList.Contains("L3"))
                        return true;
                }
            }
            return false;
        }

        private static Dictionary<uint, (EventClass, GeneratedByEnum)> GetEventGeneratedBy(Dictionary<uint, (EventClass, bool)> eventTypeMap, List<uint> runningEvents)
        {
            Dictionary<uint, (EventClass, GeneratedByEnum)> confIDToGeneratedBy = new Dictionary<uint, (EventClass, GeneratedByEnum)>();
            foreach (var item in eventTypeMap)
            {
                uint confID = item.Key;
                (EventClass eventClass, bool isShared) = item.Value;
                if (runningEvents.Contains(confID))
                    confIDToGeneratedBy.Add(confID, (eventClass, GeneratedByEnum.PQServer));
                else
                {
                    if (isShared)
                        confIDToGeneratedBy.Add(confID, (eventClass, GeneratedByEnum.MeasuringDevice));
                    else
                        confIDToGeneratedBy.Add(confID, (eventClass, GeneratedByEnum.NotCalculated));
                }
            }
            return confIDToGeneratedBy;
        }

        private static FiltersGroupContainer GetPQEventsFilter(IEnumerable<EventClass> eventClassContainer)
        {
            FiltersGroupContainer filtersGroupContainer = new FiltersGroupContainer();

            ClassFilter classFilter = new ClassFilter();
            foreach (var item in eventClassContainer)
            {
                classFilter.AddSingleValue(item);
            }

            IsAggregatedFilter isAggregatedFilter = new IsAggregatedFilter();
            isAggregatedFilter.isAggregated = false;

            FiltersGroup filtersGroup = new FiltersGroup();
            filtersGroup.AddFilter(classFilter);
            filtersGroup.AddFilter(isAggregatedFilter);
            filtersGroupContainer.FilterGroups.Add(filtersGroup);

            return filtersGroupContainer;
        }

        private static void AggregateEvents(List<PQEvent> PQEventListForParam, PQZTimeSpan eventsAggDuration)
        {
            List<EventDataSource> eventDataSourceList = new List<EventDataSource>();
            foreach (PQEvent item in PQEventListForParam)
            {
                EventDataSource evDataSource = new EventDataSource(item);
                eventDataSourceList.Add(evDataSource);
            }

            IEnumerable<EventDataSource> eventDataSources = InvestigationUtils.AggregatePQEvents(eventDataSourceList, eventsAggDuration);
            PQEventListForParam.Clear();
            foreach (var item in eventDataSources)
            {
                PQEventListForParam.Add((PQEvent)item.Event);
            }
        }

        private List<TableWidgetResponseItem> ArrangingForTable(IEnumerable<GraphParametersComponentDtoV3> graphes, string quantity, string parameterName, bool isFeedersAreShared, string? TagName = null, string? TagValue = null)
        {
            var result = new List<TableWidgetResponseItem>();
            foreach (var graph in graphes)
            {
                var item = ArrangingForTable(graph, parameterName, quantity, isFeedersAreShared, TagName, TagValue);
                result.Add(item);
            }

            return result;
        }

        private List<BarItem> ArrangingForBarChart(IEnumerable<GraphParametersComponentDtoV3> graphs, string? componentId, int? feederId)
        {
            List<BarItem> barItemList = new List<BarItem>();
            foreach (GraphParametersComponentDtoV3 graph in graphs)
            {
                AxisValue axisVal = graph.FirstAxis();
                if (axisVal != null)
                {
                    BasicValue bVal = axisVal.ToBasicValue();
                    var result = new BarItem(seriesName: graph.CustomParameterName,
                                value: bVal.Value,
                                dataUnitType: graph.DataUnitType,
                                status: bVal.DataValueStatus);

                    barItemList.Add(result);
                }
                else
                {
                    PqbiDataValueStatus erStatus = PqbiDataValueStatus.Hole;
                    var result = new BarItem(seriesName: graph.CustomParameterName,
                               value: 0,
                               dataUnitType: graph.DataUnitType,
                               status: erStatus);

                    barItemList.Add(result);
                }
            }

            return barItemList;
        }


        private string GetMsrPointName(FeederComponentInfo feeder)
        {
            if (!string.IsNullOrEmpty(feeder.CompName))
                return $"{feeder.CompName}, {feeder.Name}";
            else
                return feeder.Name;
        }

        private async Task<int> LoadPrmToCache(string url, string session, DateTime startDate, DateTime endDate, Infrastructure.TimeZone timezoneinfo, Dictionary<Guid, Dictionary<FiltersGroup, HashSet<BaseParameterComponent>>> baseParametersHashSet, bool isRealTime)
        {
            int resInSec = 0;
            if (baseParametersHashSet.IsCollectionExists())
            {
                foreach (var keyAndValue in baseParametersHashSet)
                {
                    var filterAndParameterComponents = keyAndValue.Value;
                    foreach (var item in filterAndParameterComponents)
                    {
                        FiltersGroup filterGroup = item.Key;
                        HashSet<BaseParameterComponent> prmCompSet = item.Value;
                        resInSec = (int)prmCompSet.First().Parameter.Resolution;
                        await SendingAndStoringDataAsync(url, session, startDate, endDate, timezoneinfo, (false, null), prmCompSet, filterGroup, isRealTime, resInSec);
                    }
                }
            }
            return resInSec;
        }

        void PreparePrmMapForTrendReq(DateTime startDate, DateTime endDate, List<FeederComponentInfo> feeders, int resolutionInSec, Dictionary<Guid, Dictionary<FiltersGroup, HashSet<BaseParameterComponent>>> baseParametersHashSet, TrendParameter parameter)
        {
            try
            {
                TableWidgetParameterType widgetTableType = CalculationStaticTypes.GetTableWidgetParameterType(parameter.Type);
                if (widgetTableType == TableWidgetParameterType.BaseParameter)
                {
                    BaseParameter basePrm = parameter.BaseData.ToBaseParameter();

                    basePrm.Resolution = resolutionInSec;

                    foreach (var feeder in feeders)
                    {
                        var parameterComponents = basePrm.CreateBaseParameterComponents([feeder]);

                        FiltersGroup filterGroup = new FiltersGroup();
                        Dictionary<FiltersGroup, HashSet<BaseParameterComponent>> filterGroupToMsrPrmSetMap = null;
                        if (baseParametersHashSet.TryGetValue(feeder.ComponentId, out filterGroupToMsrPrmSetMap))
                        {
                            if (!filterGroupToMsrPrmSetMap.TryGetValue(filterGroup, out HashSet<BaseParameterComponent> prmComp))
                            {
                                prmComp = new HashSet<BaseParameterComponent>();
                                filterGroupToMsrPrmSetMap.Add(filterGroup, prmComp);
                            }
                            prmComp.AddRange(parameterComponents);
                        }
                        else
                        {
                            filterGroupToMsrPrmSetMap = new Dictionary<FiltersGroup, HashSet<BaseParameterComponent>>(new FiltersGroupComparer());
                            baseParametersHashSet.Add(feeder.ComponentId, filterGroupToMsrPrmSetMap);

                            filterGroupToMsrPrmSetMap[filterGroup] = new HashSet<BaseParameterComponent>(parameterComponents);
                        }
                    }

                }
            }
            catch (SessionExpiredException sessionExpiredException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
                throw new UserFriendlyException($"{parameter} - Failed [{ex.Message}] please rerun without it.");
            }
        }

        int PreparePrmMapForReq<T>(DateTime startDate, DateTime endDate, bool shouldSyncChanged, List<FeederComponentInfo> feeders, List<T> eventColWidgetTableList, List<T> otherColWidgetTableList, Dictionary<Guid, Dictionary<FiltersGroup, HashSet<BaseParameterComponent>>> baseParametersHashSet, T parameter, int refreshRate, GaugeMarkerDto? marker = null) where T : IWidgetParameter
        {
            int numOfPrms = 0;
            int resInSec = 0;
            try
            {
                TableWidgetParameterType widgetTableType = CalculationStaticTypes.GetTableWidgetParameterType(parameter.ParameterType);
                switch (widgetTableType)
                {
                    case TableWidgetParameterType.Event:
                        {
                            eventColWidgetTableList.Add(parameter);
                            numOfPrms += feeders.Count;
                        }
                        break;
                    default:
                        if (widgetTableType == TableWidgetParameterType.BaseParameter)
                        {
                            var baseParameter = JsonConvert.DeserializeObject<BaseParameter>(parameter.BaseData);
                            {
                                if (marker != null)
                                    baseParameter.Quantity = marker.Operation?.ToString();

                                otherColWidgetTableList.Add(parameter);
                                if (baseParameter.Resolution.HasValue)
                                    resInSec = baseParameter.Resolution.Value;
                                if (shouldSyncChanged)
                                {
                                    bool isNeedFilterByEvents = AdvancedSettings.IsNeedToExcludeFlagged(parameter.IsExcludeFlaggedData, parameter.ExcludeFlagged);

                                    if (refreshRate == 0)
                                    {
                                        if (!isNeedFilterByEvents)
                                            resInSec = (int)((endDate - startDate).TotalSeconds);
                                        else
                                            resInSec = (int)(new SyncInterval(IntervalSynchronized.IS1MIN).TimeIntervalInSec);
                                    }
                                    else
                                    {
                                        double refreshInSeconds = refreshRate;
                                        if (!isNeedFilterByEvents)
                                            resInSec = (int)(refreshInSeconds);
                                        else
                                        {
                                            resInSec = (int)(refreshInSeconds > 60 ?
                                                60 : refreshInSeconds);

                                            //baseParameter.Resolution = (int)(new SyncInterval(IntervalSynchronized.IS1MIN).TimeIntervalInSec);
                                        }
                                    }

                                }
                                baseParameter.Resolution = resInSec;

                                foreach (var feeder in feeders)
                                {
                                    var parameterComponents = baseParameter.CreateBaseParameterComponents([feeder]);
                                    numOfPrms += parameterComponents.Count();

                                    //AdvancedSettings advancedSettings = new AdvancedSettings(parameter.NormalValue, parameter.Normalize, parameter.IsExcludeFlaggedData, parameter.ExcludeFlagged, parameter.IgnoreAligningFunction, parameter.ReplaceAggregationWith);
                                    FiltersGroup filterGroup = AdvancedSettings.GetFilterGroup(parameter.ExcludeFlagged);
                                    Dictionary<FiltersGroup, HashSet<BaseParameterComponent>> filterGroupToMsrPrmSetMap = null;
                                    if (baseParametersHashSet.TryGetValue(feeder.ComponentId, out filterGroupToMsrPrmSetMap))
                                    {
                                        if (!filterGroupToMsrPrmSetMap.TryGetValue(filterGroup, out HashSet<BaseParameterComponent> prmComp))
                                        {
                                            prmComp = new HashSet<BaseParameterComponent>();
                                            filterGroupToMsrPrmSetMap.Add(filterGroup, prmComp);
                                        }
                                        prmComp.AddRange(parameterComponents);
                                    }
                                    else
                                    {
                                        filterGroupToMsrPrmSetMap = new Dictionary<FiltersGroup, HashSet<BaseParameterComponent>>(new FiltersGroupComparer());
                                        baseParametersHashSet.Add(feeder.ComponentId, filterGroupToMsrPrmSetMap);

                                        filterGroupToMsrPrmSetMap[filterGroup] = new HashSet<BaseParameterComponent>(parameterComponents);
                                    }
                                    //await SendingAndStoreingDataAsync(url, session, input.StartDate, input.EndDate, (false, null), parameterComponents);
                                }


                                //foreach (var feeder in input.Rows.Feeders)
                                //{
                                //    var parameterComponents = baseParameter.CreateBaseParameterComponents([feeder]);
                                //    await SendingAndStoreingDataAsync(url, session, input.StartDate, input.EndDate, (false, null), parameterComponents);
                                //}
                            }
                        }
                        else
                        {
                            otherColWidgetTableList.Add(parameter);
                            var customParameterId = parameter.CustomData.Id;
                            var customParameter = GetCustomParameter(customParameterId);
                            CustomParameterType customParameterType = CalculationStaticTypes.GetCustomParameterType(customParameter.Type);
                            if (customParameterType == CustomParameterType.SPMC)
                                numOfPrms++;
                            else
                                numOfPrms += feeders.Count;
                        }
                        break;
                }


            }
            catch (SessionExpiredException sessionExpiredException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
                throw new UserFriendlyException($"{parameter.ParameterName} - Failed [{ex.Message}] please rerun without it.");
            }

            return numOfPrms;
        }

        private async Task SendingAndStoringDataAsync(string url, string session, DateTime startDatetime, DateTime endDatetime, Infrastructure.TimeZone timeZone, (bool isNominalCalculate, double? nominalValue) calculationData, IEnumerable<BaseParameterComponent> paramComponents, FiltersGroup? filterGroup, bool isRealTime, int realTimeRefreshRate)
        {
            //Should be refactored!!!!
            if (paramComponents.IsCollectionEmpty())
            {
                return;
            }

            int refreshRateInSec = realTimeRefreshRate;
            var start = new PQZDateTime(startDatetime);
            var end = new PQZDateTime(endDatetime);
            var userTz = TimeZoneInfo.FindSystemTimeZoneById(TZConvert.IanaToWindows(timeZone.TimeZoneInfo));

            using (var mainLogger = PqbiStopwatch.AnchorAsync(nameof(SendingAndStoringDataAsync), Logger))
            {
                //FeeerID null should be taken underc onsidaration.
                var groups = paramComponents.GroupBy(p => new { p.ComponentID }).ToArray();
                var requests = new List<(PQSGetBaseDataRequest, IEnumerable<BaseParameterComponent>)>();
                var getBaseDataInfoInputs = new List<GetBaseDataInfoInput>();

                var basParameterIndexer = new Dictionary<Guid, List<BaseParameterComponent>>();   //Key = CompId

                int durtionInSec = (int)(endDatetime - startDatetime).TotalSeconds;

                if (!isRealTime)
                {
                    foreach (var group in groups)
                    {
                        var measurementParameters = new List<MeasurementParameterBase>();
                        var queue = new List<BaseParameterComponent>();

                        for (int index = 0; index < group.Count(); index++)
                        //foreach (BaseParameterComponent parameterComponent in group)
                        {
                            var parameterComponent = group.ElementAt(index);
                            var calculationItem = new CalculationCacheItem { ComponentId = parameterComponent.ComponentID, FeederId = parameterComponent.FeederId, Start = start.DateTimeUTC, End = end.DateTimeUTC, Parameter = parameterComponent.MeasurementParameter.ToString(), FiltersGroup = filterGroup };

                            if (calculationItem.TryGetCalculationCache(_cacheManager, out var cache))
                            {
                                parameterComponent.SetRawData(cache.PQBIAxisData.ToUserTime(userTz));

                                //parameterComponent.SetRawData(cache.PQBIAxisData, calculationData.isNominalCalculate, calculationData.nominalValue);
                                mainLogger.LogInformation($"Cache used {parameterComponent.ParameterId}");
                                continue;
                            }

                            measurementParameters.Add(parameterComponent.MeasurementParameter);
                            queue.Insert(0, parameterComponent);
                        }

                        if (queue.Count > 0)
                        {
                            var guid = group.First().ComponentID;
                            if (filterGroup != null && filterGroup.FiltersCount == 0)
                                filterGroup = null;
                            var input = new GetBaseDataInfoInput(guid, start.TicksPQZTimeFormat, end.TicksPQZTimeFormat, measurementParameters, CalculationTypeEnum.AUTOMATIC, filtersGroup: filterGroup);
                            basParameterIndexer.Add(guid, queue);
                            getBaseDataInfoInputs.Add(input);
                        }
                    }
                }
                else
                {
                    //var perCompFullParams = new Dictionary<Guid, HashSet<MeasurementParameterBase>>();
                    //var perCompRightParams = new Dictionary<Guid, HashSet<MeasurementParameterBase>>();
                    //var perParamCoverage = new Dictionary<(Guid comp, int? feeder, string paramName), (DateTime covStart, DateTime covEnd)>();

                    // Classify each requested (component, parameter)
                    var rtCache = _cacheManager.GetCache<string, RealtimeCalculationCacheRecord>(CalculationCacheNames.Realtime);

                    foreach (var compGroup in groups)
                    {
                        var measurementParameters = new List<MeasurementParameterBase>();
                        var queue = new List<BaseParameterComponent>();
                        DateTime newStart = DateTime.MinValue;

                        foreach (var pc in compGroup)
                        {
                            var key = RealtimeCacheKey.For(pc.ComponentID, pc.FeederId, durtionInSec, refreshRateInSec, pc.MeasurementParameter.ToString(), filterGroup);

                            if (!rtCache.TryGetValue(key, out var rec))
                            {
                                newStart = startDatetime;
                            }
                            else
                            {
                                var covStart = rec.CoveredStartUtc;
                                var covEnd = rec.CoveredEndUtc;

                                if (covStart <= startDatetime && endDatetime <= covEnd)
                                {
                                    var slicedAxisData = rec.PqbAxis.Slice(startDatetime, endDatetime);
                                    var slicedAxisDataLocal = slicedAxisData.ToUserTime(userTz);
                                    pc.SetRawData(slicedAxisDataLocal);
                                    continue;
                                }

                                if (covStart > startDatetime)
                                {
                                    newStart = startDatetime;

                                    rtCache.Remove(key);
                                    // Left gap exists => FULL FETCH (your rule)

                                }
                                else if (covEnd <= endDatetime)
                                {
                                    if (covEnd > startDatetime)
                                    {
                                        newStart = covEnd - TimeSpan.FromSeconds(realTimeRefreshRate);

                                        if (newStart < startDatetime)
                                        {
                                            newStart = startDatetime;
                                        }
                                        //else
                                        //{
                                        //    isFullFetch = false;
                                        //    isRightFetch = true;
                                        //}
                                    }
                                    else
                                    {
                                        newStart = startDatetime;
                                        rtCache.Remove(key);
                                    }

                                    // Only right gap => RIGHT FETCH
                                }
                                else
                                {
                                    var slicedAxisData = rec.PqbAxis.Slice(startDatetime, endDatetime);
                                    pc.SetRawData(slicedAxisData.ToUserTime(userTz));
                                    continue;
                                }
                            }

                            measurementParameters.Add(pc.MeasurementParameter);
                            queue.Insert(0, pc);
                        }

                        if (queue.Count > 0)
                        {
                            var pqzStart = new PQZDateTime(newStart);

                            var guid = compGroup.First().ComponentID;
                            if (filterGroup != null && filterGroup.FiltersCount == 0)
                                filterGroup = null;
                            var input = new GetBaseDataInfoInput(guid, pqzStart.TicksPQZTimeFormat, end.TicksPQZTimeFormat, measurementParameters, CalculationTypeEnum.AUTOMATIC, filtersGroup: filterGroup);

                            basParameterIndexer.Add(guid, queue);
                            getBaseDataInfoInputs.Add(input);
                        }

                    }
                }

                if (getBaseDataInfoInputs.SafeAny())
                {
                    var request = new PQSGetBaseDataRequest(session, timeZone.TimeZoneID, getBaseDataInfoInputs.ToArray());
                    request.ID = Guid.NewGuid();

                    using (var sendingLogger = mainLogger.CreateSubLogger($"SendingToScada)"))
                    {

                        sendingLogger.LogInformation($"xxx Sending {request.ID} url={url}");
                        var response = await SendRecordsContainerPostBinaryRequestAndException(url, request);
                        sendingLogger.LogInformation($"xxx receiving {request.ID}");

                        string res = null;
#if DEBUG

                        var ptr = PQZxmlWriter.WriteMessage(request, true);

#endif

#if DEBUG

                        res = PQZxmlWriter.WriteMessage(response, true);

#endif

                        var getBaseResponse = new PQSGetBaseDataResponse(request, response, timeZone.TimeZoneInfo);



                        getBaseResponse.ExtractGetParametersOrError(out IEnumerable<PQBIAxisData> axisses, convertToUserTime: false);

                        if (!isRealTime)
                        {
                            foreach (var axise in axisses.ToArray())
                            {
                                sendingLogger.LogInformation($"Send {axise}");

                                if (basParameterIndexer.TryGetValue(axise.ComponentId, out var baseParameterComponents))
                                {
                                    var baseParameter = baseParameterComponents.FirstOrDefault(x => x.MeasurementParameter.ToString() == axise.ParameterName);
                                    if (baseParameter is not null)
                                    {
                                        //if (axise is null)
                                        //{

                                        //}
                                        var axisLocal = axise.ToUserTime(userTz);
                                        baseParameter.SetRawData(axisLocal);
                                        if (axise.PQZStatus != PQZStatus.OK)
                                        {
                                            sendingLogger.LogError($"ComponentId = {axise.ComponentId} with parameter = {axise.ParameterName} failed with Status ={axise.PQZStatus.ToString()}");
                                            continue;
                                        }

                                        //TODO: should be removed!!!!!!!!!!!!!!!
                                        //axise.FeederID = baseParameter.FeederId;

                                        var calculationItem = new CalculationCacheItem
                                        {
                                            ComponentId = baseParameter.ComponentID,
                                            FeederId = baseParameter.FeederId,
                                            Start = start.DateTimeUTC,
                                            End = end.DateTimeUTC,
                                            Parameter = baseParameter.MeasurementParameter.ToString(),
                                            FiltersGroup = filterGroup,
                                            PQBIAxisData = axise
                                        };

                                        await calculationItem.SetCalculationCacheAsync(_cacheManager);
                                        mainLogger.LogInformation($"Cache insrated {baseParameter.ParameterId}");
                                    }
                                    else
                                    {
                                        throw new UserFriendlyException("xyz");
                                    }
                                }
                            }
                        }
                        else
                        {
                            var rtCache = _cacheManager.GetCache<string, RealtimeCalculationCacheRecord>(CalculationCacheNames.Realtime);
                            foreach (var axise in axisses.ToArray())
                            {
                                sendingLogger.LogInformation($"Send {axise}");

                                if (basParameterIndexer.TryGetValue(axise.ComponentId, out var baseParameterComponents))
                                {
                                    var baseParameter = baseParameterComponents.FirstOrDefault(x => x.MeasurementParameter.ToString() == axise.ParameterName);
                                    if (baseParameter is not null)
                                    {
                                        var rtKey = RealtimeCacheKey.For(baseParameter.ComponentID, baseParameter.FeederId, durtionInSec, refreshRateInSec, baseParameter.MeasurementParameter.ToString(), filterGroup);
                                        bool isExistInCache = rtCache.TryGetValue(rtKey, out var oldrec);

                                        if (!isExistInCache)
                                        {

                                            baseParameter.SetRawData(axise.ToUserTime(userTz));
                                            if (axise.PQZStatus != PQZStatus.OK)
                                            {
                                                sendingLogger.LogError($"ComponentId = {axise.ComponentId} with parameter = {axise.ParameterName} failed with Status ={axise.PQZStatus.ToString()}");
                                                continue;
                                            }

                                            // Overwrite rolling record
                                            var recNew = new RealtimeCalculationCacheRecord
                                            {
                                                ComponentId = baseParameter.ComponentID,
                                                FeederId = baseParameter.FeederId,
                                                Parameter = baseParameter.MeasurementParameter.ToString(),
                                                CoveredStartUtc = startDatetime,
                                                CoveredEndUtc = endDatetime,
                                                PqbAxis = axise
                                            };
                                            await rtCache.SetAsync(rtKey, recNew);
                                        }
                                        else
                                        {
                                            var slicedAxisData = oldrec.PqbAxis.Slice(startDatetime, endDatetime);
                                            var mergedAxisData = slicedAxisData.MergeAppendWithTailOverride(axise);
                                            baseParameter.SetRawData(mergedAxisData.ToUserTime(userTz));

                                            // Overwrite rolling record
                                            var recNew = new RealtimeCalculationCacheRecord
                                            {
                                                ComponentId = baseParameter.ComponentID,
                                                FeederId = baseParameter.FeederId,
                                                Parameter = baseParameter.MeasurementParameter.ToString(),
                                                CoveredStartUtc = startDatetime,
                                                CoveredEndUtc = endDatetime,
                                                PqbAxis = mergedAxisData
                                            };
                                            await rtCache.SetAsync(rtKey, recNew);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }


    }
}