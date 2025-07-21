using Abp.UI;
using PQBI.Network.RestApi.EngineCalculation;
using PQBI.PQS.CalcEngine;
using PQBI.PQS;
using PQBI.Requests;
using PQBI.Tenants.Dashboard.Dto;
using PQS.Data.Configurations.Enums;
using PQS.Data.Measurements;
using PQZTimeFormat;
using Microsoft.Extensions.Logging;
using PQBI.CalculationEngine;
using PQBI.Network.Base;
using Newtonsoft.Json;
using PQS.Data.Common;
using Abp.Domain.Repositories;
using PQBI.CalculationEngine.Functions;
using Abp.Runtime.Caching;
using PQBI.Infrastructure;
using PQBI.Infrastructure.Extensions;
using Microsoft.Extensions.Options;
using PQBI.PQS.Cache.Calculation;
using PQS.PQZxml;
using PQBI.CalculationEngine.Matrix;
using PQBI.Infrastructure.Lockers;


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
        Task<TrendResponse> CalculateTrendChartAsync222(string url, string session, TrendCalcRequest222 input);
        Task<TableWidgetResponse> CalculateTableAsync(string url, string session, TableWidgetRequest222 input);
        Task<IEnumerable<BarCharComponentResponse>> CalculateBarChartAsync(string url, string session, BarChartRequest input);
    }

    public class CustomParameterCalculationService : PQSRestApiServiceBase, ICustomParameterCalculationService
    {
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

        public async Task<TrendResponse> CalculateTrendChartAsync222(string url, string session, TrendCalcRequest222 input)
        {
            var response = new TrendResponse();
            var calculationDataItems = new PqbiSafeEntityLockerSlim<List<CalculatedDataItem>>([]);
            var timeStamps = new PqbiSafeEntityLockerSlim<List<long>>([]);

            if (string.IsNullOrEmpty(session))
            {
                throw new UserFriendlyException(nameof(session), "Cant be null");
            }

            using (var mainLogger = PqbiStopwatch.AnchorAsync($"Trender - {input.WidgetName} {nameof(CalculateTrendChartAsync222)}", Logger))
            {
                var list = new List<Task>();
                foreach (TrendParameter parameter in input.Parameters)
                {
                    var task = Task.Run(async () =>
                    {
                        using (var subLogger = mainLogger.CreateSubLogger("Parameter Calculation"))
                        {
                            var graphes = await CalculateTrendChartIntristicAsync(url, session, input, parameter);
                            foreach (var graph in graphes)
                            {
                                var data = new CalculatedDataItem
                                {
                                    ParameterType = graph.RequestType,
                                    Feeders = graph.Feeders.ToList()
                                };

                                if(graph.MissingInformation.IsCollectionExists())
                                {
                                    data.MissingInformation.AddRange(graph.MissingInformation);
                                }


                                if (graph.CustomParameterName.IsStringExists())
                                {
                                    data.ParameterName = graph.CustomParameterName;
                                }
                                else
                                {
                                    data.ParameterName = graph.ParameterNames.FirstOrDefault() ?? "xxx";
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
                    );

                    list.Add(task);
                }

                await Task.WhenAll(list);

                response.Data = calculationDataItems.Value;
                response.TimeStamps = timeStamps.Value;
            }

            return response;
            //return new CalculationDto(result, true, string.Empty);
        }

        private async Task<IEnumerable<GraphParametersComponentDtoV3>> CalculateTrendChartIntristicAsync(string url, string session, TrendCalcRequest222 input, TrendParameter parameter)
        {
            var result = new List<GraphParametersComponentDtoV3>();

            TrendWidgetParameterType customParameterType = CalculationStaticTypes.GetCustomParameterType222(parameter.Type);

            switch (customParameterType)
            {
                case TrendWidgetParameterType.CustomParameter:

                    TrendCustomWidgetData customWidgetData = parameter.CustomData;
                    var customParameterId = customWidgetData.Id;
                    //var customParameterId = int.Parse(parameter.Data);
                    var nodes = await AssembleCustomParameterTree(customParameterId, url, session, input.StartDate, input.EndDate, input.ResolutionInSeconds, input.IsAutoResolution, parameter.CustomData.Quantity, parameter.Feeders, null);
                    foreach (var node in nodes)
                    {
                        var graph = _engineControllerService.FullCalculation(node);
                        result.Add(graph);
                    }

                    break;

                case TrendWidgetParameterType.BaseParameter:
                    var baseData = parameter.BaseData;
                    //TrendBaseData baseData = parameter.BaseData;
                    var baseParameter = baseData.ToBaseParameter();
                    SetAutoResolution(baseParameter, input);// input.StartDate, input.EndDate, input.Resolution,input.ResolutionInSeconds, input.IsAutoResolution);


                    var root = new CustomParameterNodeCalculator(CustomParameterType.BPCP, -1, input.IsAutoResolution, string.Empty, input.StartDate, input.EndDate, input.ResolutionInSeconds, baseParameter.Quantity);
                    var baseParameterRequests = baseParameter.CreateBaseParameterComponents(parameter.Feeders);

                    root.PopulateWithBaseParameterComponents(baseParameterRequests);

                    //SelectAssemble(root, parameter.Feeders);

                    await SendingAndStoreingDataAsync(url, session, input.StartDate, input.EndDate, (false, null), root.BaseParameterComponents);
                    var baseParameterGraph = _engineControllerService.FullCalculation(root);

                    result.Add(baseParameterGraph);
                    return result;


                case TrendWidgetParameterType.Exception:

                    TrendCustomWidgetData exceptionCustomWidgetData = parameter.CustomData;
                    var exceptionCustomParameterId = exceptionCustomWidgetData.Id;

                    var exceptionNodes = await AssembleCustomParameterTree(exceptionCustomParameterId, url, session, input.StartDate, input.EndDate, input.ResolutionInSeconds, input.IsAutoResolution, parameter.CustomData.Quantity, [], null);
                    foreach (var exceptionnode in exceptionNodes)
                    {
                        var exceptionGraph = _engineControllerService.FullCalculation(exceptionnode);
                        result.Add(exceptionGraph);
                    }

                    break;

                default:
                    break;
            }


            return result;
        }

        private void SetAutoResolution(BaseParameter baseParameter, TrendCalcRequest222 input)
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

        private async Task<IEnumerable<CustomParameterNodeCalculator>> AssembleCustomParameterTree(int customParameterId, string url, string session, DateTime start, DateTime end, int widgetResolutionInSeconds, bool isAutoResolution, string parameterQuantity, IEnumerable<FeederComponentInfo> feeders, InnerCustomParameter currentInnerCustomParameters)
        {
            var result = new List<CustomParameterNodeCalculator>();

            var nodeMap = new Dictionary<int, (CustomParameters.CustomParameter CustomParameter, CustomParameterNodeCalculator Node)>();
            var parentMap = new Dictionary<int, int?>();

            var stack = new Stack<int>();
            stack.Push(customParameterId);

            //var firstCustoParameterId = customParameterId;
            var lastCustoParameterId = customParameterId;

            parentMap[customParameterId] = null;

            CustomParameters.CustomParameter customParameter = null;
            while (stack.Count > 0)
            {
                var currentId = lastCustoParameterId = stack.Pop();
                customParameter = GetCustomParameter(currentId);

                if (customParameter == null)
                {
                    continue;
                }

                var customParameterType = CalculationStaticTypes.GetCustomParameterType(customParameter.Type);
                var nextInnerCustomParameters = JsonConvert.DeserializeObject<InnerCustomParameter[]>(customParameter.InnerCustomParameters ?? string.Empty) ?? [];

                var node = new CustomParameterNodeCalculator(customParameterType, customParameter.ResolutionInSeconds, isAutoResolution, customParameter.AggregationFunction, start, end, widgetResolutionInSeconds, parameterQuantity, customParameter.Name, currentInnerCustomParameters);

                nodeMap[currentId] = (customParameter, node);

                foreach (var child in nextInnerCustomParameters)
                {
                    stack.Push(child.CustomParameterId);
                    parentMap[child.CustomParameterId] = currentId;
                }
            }

            int? currentCosutmParametertId = lastCustoParameterId;

            var buffer = new List<CustomParameterNodeCalculator>();
            while (currentCosutmParametertId is not null)
            {
                var (tmpCustomParameter, node) = nodeMap[currentCosutmParametertId.Value];

                var parameterList = JsonConvert.DeserializeObject<BaseParameter[]>(tmpCustomParameter.STDPQSParametersList ?? string.Empty) ?? [];

                IEnumerable<BaseParameterComponent> baseParameterRequests = null;
                if (node.CustomParameterType == CustomParameterType.Exception)
                {
                    baseParameterRequests = parameterList.CreateExceptionBaseParameterComponents();
                }
                else
                {
                    baseParameterRequests = parameterList.CreateBaseParameterComponents(feeders);
                }

                var hasAddedToFather = false;
                if (node.CustomParameterType == CustomParameterType.MPSC)
                {
                    var groups = baseParameterRequests.GroupBy(x => new { x.ComponentID });
                    //var groups = baseParameterRequests.GroupBy(x => new { x.ComponentID, x.FeederId });
                    if (groups.IsCollectionExists())
                    {
                        foreach (var ptr in groups)
                        {
                            node = new CustomParameterNodeCalculator(node.CustomParameterType, tmpCustomParameter.ResolutionInSeconds, isAutoResolution, tmpCustomParameter.AggregationFunction, node.StartDate, node.EndDate, widgetResolutionInSeconds, parameterQuantity, tmpCustomParameter.Name);
                            await SetAndCalculateNode(url, session, start, end, node, ptr);
                            buffer.Add(node);

                            hasAddedToFather = SetChildToFather(currentCosutmParametertId.Value, node);
                        }
                    }
                    else
                    {
                        await SetAndCalculateNode(url, session, start, end, node, baseParameterRequests);
                        buffer.Add(node);
                        hasAddedToFather = SetChildToFather(currentCosutmParametertId.Value, node);
                    }
                }
                else
                {
                    await SetAndCalculateNode(url, session, start, end, node, baseParameterRequests);
                    buffer.Add(node);
                    hasAddedToFather = SetChildToFather(currentCosutmParametertId.Value, node);
                }

                if (hasAddedToFather == false)
                {
                    // Means Root!!!!!!
                    foreach (var item in buffer)
                    {
                        item.Feeders = feeders;
                    }

                    result.AddRange(buffer);
                }

                buffer.Clear();
                parentMap.TryGetValue(currentCosutmParametertId.Value, out currentCosutmParametertId);
            }

            return result;

            bool SetChildToFather(int currentCustomParameter, CustomParameterNodeCalculator node)
            {
                if (parentMap.TryGetValue(currentCustomParameter, out var parentId))
                {
                    if (parentId is not null)
                    {
                        if (nodeMap.TryGetValue(parentId.Value, out var paranetNode))
                        {
                            var (fatherId, father) = paranetNode;
                            father.Children.Add(node);
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        private async Task SetAndCalculateNode(string url, string session, DateTime start, DateTime end, CustomParameterNodeCalculator node, IEnumerable<BaseParameterComponent> ptr)
        {
            node.PopulateWithBaseParameterComponents(ptr);

            await SendingAndStoreingDataAsync(url, session, start, end, (false, null), ptr);
            CalculatedInnerAndOuterAggregation2222(node);
        }

        private void CalculatedInnerAndOuterAggregation2222(CustomParameterNodeCalculator node)
        {
            //var matrix = node.ParameterMatrix;
            //foreach (var parameterComponent in node.BaseParameterComponents)
            //{
            //    var calculated = _engineControllerService.CalculatedInnerAlignment(node, parameterComponent);
            //    matrix.AddSeries(parameterComponent, calculated, parameterComponent.PQZStatus);
            //}

            node.CalculatedInnerAlignment();

            _engineControllerService.CalculateOutterAggregation(node);
        }


        //private void CalculatedInnerAndOuterAggregation(CustomParameterNodeCalculator node)
        //{
        //    var matrix = node.ParameterMatrix;
        //    foreach (var parameterComponent in node.BaseParameterComponents)
        //    {
        //        var calculated = _engineControllerService.CalculatedInnerAlignment(node, parameterComponent);
        //        matrix.AddSeries(parameterComponent, calculated, parameterComponent.PQZStatus);
        //    }

        //    _engineControllerService.CalculateOutterAggregation(node);
        //}

        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------


        public async Task<TableWidgetResponse> CalculateTableAsync(string url, string session, TableWidgetRequest222 input)
        {
            var responseItems = new List<TableWidgetResponseItem>();
            var paramComponents = new List<BaseParameterComponent>();

            using (var mainLogger = PqbiStopwatch.AnchorAsync($"Tablo - {input.WidgetName} {nameof(CalculateTableAsync)}", Logger))
            {
                foreach (var parameter in input.ColumnWidgetTables)
                {
                    try
                    {
                        using (var sub = mainLogger.CreateSubLogger(parameter.ParameterName))
                        {
                            TableWidgetParameterType widgetTableType = CalculationStaticTypes.GetTableWidgetParameterType(parameter.ParameterType);
                            switch (widgetTableType)
                            {
                                case TableWidgetParameterType.CustomParameter:

                                    var items = await CustomParameterCreateTableNodeAsync(url, session, input, parameter);
                                    responseItems.AddRange(items);

                                    break;

                                case TableWidgetParameterType.BaseParameter:

                                    var baseParamaterItems = await BaseParameterCreateTableNodeAsync(url, session, input, parameter);
                                    responseItems.AddRange(baseParamaterItems);
                                    break;

                                case TableWidgetParameterType.Event:
                                    //In Event only count can be Nadav H.

                                    //var tmp = await WidgetTableEventCalculation(url, session, input, input.StartDate, input.EndDate, null);
                                    //var tmp = await WidgetTableEventCalculation(url, session, input, input.StartDate, input.EndDate, parameter);
                                    //responseItems.AddRange(tmp);
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

        private void CustomParameterTableValidate(TableWidgetRequest222 input, CustomParameters.CustomParameter customParameter)
        {
            var customParameterType = CalculationStaticTypes.GetCustomParameterType(customParameter.Type);

            if (customParameterType == CustomParameterType.Exception)
            {

                throw new UserFriendlyException("In table exception mode is not allowrd.");
            }
        }


        private async Task<IEnumerable<TableWidgetResponseItem>> CustomParameterCreateTableNodeAsync(string url, string session, TableWidgetRequest222 input, ColumnWidgetTable parameter)
        {
            var customParameterId = parameter.CustomData.Id;

            var customParameter = GetCustomParameter(customParameterId);
            CustomParameterTableValidate(input, customParameter);
            Func<IEnumerable<FeederComponentInfo>, Task<IEnumerable<CustomParameterNodeCalculator>>> selector = async (feeders) =>
            {
                var nodes = await AssembleCustomParameterTree(customParameterId, url, session, input.StartDate, input.EndDate, -1, false, parameter.CustomData.Quantity, feeders, null);
                return nodes;
            };

            var responseItems = await RealCalculateTableAsync(url, session, input, parameter, selector, parameter.CustomData.Quantity);
            return responseItems;
        }

        private CustomParameters.CustomParameter GetCustomParameter(int customParameterId)
        {
            lock (_customerParameterLocker)
            {
                return _customParameterRepository.Get(customParameterId);
            }
        }

        private async Task<IEnumerable<TableWidgetResponseItem>> BaseParameterCreateTableNodeAsync(string url, string session, TableWidgetRequest222 input, ColumnWidgetTable parameter)
        {
            var baseParameter = JsonConvert.DeserializeObject<BaseParameter>(parameter.BaseData);
            //baseParameter.SetISXResolution(input.StartDate, input.EndDate);
            //    int totalSeconds = (int)(endDate - startDate).TotalSeconds;
            baseParameter.Resolution = (int)((input.EndDate - input.StartDate).TotalSeconds);
            var node = new CustomParameterNodeCalculator(CustomParameterType.BPCP, -1, false, string.Empty, input.StartDate, input.EndDate, -1, baseParameter.Quantity);

            Func<IEnumerable<FeederComponentInfo>, Task<IEnumerable<CustomParameterNodeCalculator>>> selector = async (feeders) =>
            {
                var parameterComponents = baseParameter.CreateBaseParameterComponents(feeders);
                node.PopulateWithBaseParameterComponents(parameterComponents);

                //SelectAssemble(node, feeders);

                await SendingAndStoreingDataAsync(url, session, input.StartDate, input.EndDate, (false, null), parameterComponents);
                return [node];
            };

            var responseItems = await RealCalculateTableAsync(url, session, input, parameter, selector, baseParameter.Quantity);
            return responseItems;
        }

        private async Task<IEnumerable<TableWidgetResponseItem>> RealCalculateTableAsync(string url, string session, TableWidgetRequest222 input, ColumnWidgetTable parameter,
            Func<IEnumerable<FeederComponentInfo>, Task<IEnumerable<CustomParameterNodeCalculator>>> calculationSelector, string quantity)
        {
            var responseItems = new List<TableWidgetResponseItem>();

            using (var mainLogger = PqbiStopwatch.AnchorAsync(nameof(CalculateTableAsync), Logger))
            {
                var componentMap = new Dictionary<Guid, List<FeederComponentInfo>>();
                var feederMap = new Dictionary<FeederComponentInfo, GraphParametersComponentDtoV3?>();
                var customParameterType = CustomParameterType.BPCP;

                try
                {
                    foreach (var feeder in input.Rows.Feeders)
                    {
                        var nodes = await calculationSelector([feeder]);
                        var node = nodes.First();
                        customParameterType = node.CustomParameterType;

                        var graph = _engineControllerService.FullCalculation(node);

                        if (graph.TryGetMissingParameterInfo(out var invalidParameter))
                        {
                            mainLogger.LogError($"{invalidParameter.PropertyName} failed with PQZStatus = {invalidParameter.Status}");
                        }

                        responseItems.AddRange(ArrangingForTable([graph], quantity, parameter.ParameterName));
                        feederMap[feeder] = graph;

                        if (feeder.Id != null)
                        {
                            if (componentMap.TryGetValue(feeder.ComponentId, out var feederList))
                            {
                                feederList.Add(feeder);
                            }
                            else
                            {
                                componentMap[feeder.ComponentId] = [feeder];
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                }


                foreach (var (key, feeders) in componentMap)
                {
                    var feederList = feeders.ToArray();
                    try
                    {
                        if (customParameterType == CustomParameterType.SPMC)
                        {
                            var nodes = await calculationSelector(feederList);
                            var node = nodes.First();
                            var graph = _engineControllerService.FullCalculation(node);
                            var feeder = graph.Feeders.First();

                            var responseItem = ArrangingForTable(graph.FirstValue(), feeder.ComponentId.ToString(), null, parameter.ParameterName, quantity, graph.DataUnitType);

                            //var responseItem = ArrangingForTable(graph, parameter.ParameterName, quantity);
                            responseItems.Add(responseItem);
                        }
                        else
                        {

                            CalculateForMltiAndBaseParameter(feederMap, feederList, out var calculated, out var missingBaseParameterInfo);
                            var responseItem = ArrangingForTable(calculated, key.ToString(), null, parameter.ParameterName, quantity, new EmptyDataUnitType(), missingBaseParameterInfo: missingBaseParameterInfo);
                            responseItems.Add(responseItem);
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }


                try
                {
                    foreach (var tag in input.Rows.Tags)
                    {
                        if (customParameterType == CustomParameterType.SPMC)
                        {
                            var nodes = await calculationSelector(tag.Feeders);
                            var node = nodes.First();
                            var graph = _engineControllerService.FullCalculation(node);

                            responseItems.AddRange(ArrangingForTable([graph], quantity, parameter.ParameterName, tag.Id, tag.Name));
                        }
                        else
                        {
                            CalculateForMltiAndBaseParameter(feederMap, tag.Feeders, out var calculated, out var missingBaseParameterInfo);
                            var responseItem = ArrangingForTable(calculated, null, null, parameter.ParameterName, quantity, new EmptyDataUnitType(), tag.Id, tag.Name, missingBaseParameterInfo: missingBaseParameterInfo);
                            responseItems.Add(responseItem);
                        }
                    }
                }
                catch (Exception ex)
                {
                }
            }

            return responseItems;

            bool CalculateForMltiAndBaseParameter(Dictionary<FeederComponentInfo, GraphParametersComponentDtoV3?> fMap, IEnumerable<FeederComponentInfo> list, out BasicValue calculated, out MissingBaseParameterInfo missingBaseParameterInfo)
            {
                missingBaseParameterInfo = null;
                var values = new List<BasicValue>();
                foreach (var feeder in list)
                {
                    if (fMap[feeder].TryGetMissingParameterInfo(out missingBaseParameterInfo) == false)
                    {
                        //Valid 
                        var axisValue = fMap[feeder].FirstAxis();
                        values.Add(axisValue.ToBasicValue());
                    }
                }

                calculated = new BasicValue();
                if (values.IsCollectionEmpty() == false)
                {
                    calculated = _engineControllerService.AggregationFunctionsAsync(quantity, values);
                }

                return missingBaseParameterInfo == null;
            }
        }


        //private async Task<List<TableWidgetResponseItem>> WidgetTableEventCalculation(string url, string session, TableWidgetRequest222 input, DateTime start, DateTime end, TableWidgetParameter parameter)
        //{
        //    var responseItems = new List<TableWidgetResponseItem>();
        //    var phaseMapper = new Dictionary<string, string> { { "PH1", "L1" }, { "PH2", "L2" }, { "PH3", "L3" }, };
        //    //var phaseMapper = new Dictionary<string, string> { { "L1", "PH1" }, { "L2", "PH2" }, { "L3", "PH3" }, };


        //    var @event = JsonConvert.DeserializeObject<TableWidgetEvent>(parameter.Data);
        //    @event.AggregationInSeconds = @event.AggregationInSeconds ?? 0;
        //    var allowedPhases = @event.Phases.ToHashSet();


        //    var eventClass = (EventClass)@event.EventId;

        //    var componentIds = input.Rows.Feeders.Select(x => x.Parent.ToString()).ToHashSet();
        //    var events = new List<EventComponent>();

        //    foreach (var componentId in componentIds)
        //    {
        //        var request = new PQSGetEventRequest(session, start.ToPqzDateTime(), end.ToPqzDateTime(), [eventClass], componentId);
        //        var pqsResponse = await SendRecordsContainerPostBinaryRequestAndException(url, request);
        //        var respose = new PQSAddEventResponse(request, pqsResponse);

        //        var componentEvents = respose.Events;
        //        if (componentEvents.Count() == 0)
        //        {
        //            responseItems.Add(new TableWidgetResponseItem { Calculated = 0, ComponentId = componentId, ParameterName = parameter.ParameterName });
        //            continue;
        //        }

        //        foreach (var evntList in respose.Events)
        //        {
        //            foreach (var evnt in evntList)
        //            {
        //                foreach (var feeder in evnt.Feeders)
        //                {
        //                    if (evnt.Phases is not null && evnt.Phases.Count > 0)
        //                    {
        //                        foreach (var phase in evnt.Phases)
        //                        {
        //                            if (phaseMapper.TryGetValue(phase, out var originalPhase))
        //                            {
        //                                //if (allowedPhases.Contains(phase))
        //                                //if (phase.Equals(originalPhase, StringComparison.OrdinalIgnoreCase))
        //                                //{
        //                                var eventComponent = new EventComponent
        //                                {
        //                                    EventId = evnt.EventClass.ToString(),
        //                                    Phase = phase,
        //                                    Feeder = feeder.ToString(),
        //                                    ComponentId = componentId,
        //                                    StartTime = evnt.StartTime,
        //                                    DurationMilliSeconds = evnt.DurationMilliSecond,
        //                                    Daviation = evnt.Deviation,
        //                                    Value = evnt.Value,
        //                                };

        //                                events.Add(eventComponent);
        //                                //}
        //                            }
        //                            else
        //                            {
        //                                throw new UserFriendlyException($"phaseMapper falied with [{phase}]");
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }


        //    var shrinkList = new ShrinkList<EventComponent>();
        //    IDictionary<string, List<EventComponent>> phaseDictionary = null;

        //    if (@event.IsPolyphase)
        //    {
        //        phaseDictionary = shrinkList.Shrink(events, x => $"{x.ComponentId}__{x.EventId}__{x.Feeder}__{x.Phase}", @event.AggregationInSeconds.Value);
        //    }
        //    else
        //    {
        //        phaseDictionary = shrinkList.Shrink(events, x => $"{x.ComponentId}__{x.EventId}__{x.Feeder}", @event.AggregationInSeconds.Value);
        //    }

        //    var compDic = new Dictionary<string, List<EventComponent>>(); //Dictoinary<ComponentKId,List<>>
        //    foreach (var phaseDic in phaseDictionary)
        //    {
        //        var compID = phaseDic.Key.Split("__").First();
        //        if (compDic.TryGetValue(compID, out var phasesList))
        //        {
        //            phasesList.AddRange(phaseDic.Value);
        //        }
        //        else
        //        {
        //            compDic[compID] = phaseDic.Value;
        //        }
        //    }

        //    foreach (var compDickeyAndValue in compDic)
        //    {
        //        var points = compDickeyAndValue.Value.Select(x => SelectorProperty(@event.Parameter, x)).ToArray();

        //        //var calculated = await _engineControllerService.AggregationFunctionsAsync(parameter.Quantity, []);
        //        var calculated = _engineControllerService.AggregationFunctionsAsync(parameter.Quantity, points);

        //        var eventTableWidgetResponseItem = new TableWidgetResponseItem
        //        {
        //            ComponentId = compDickeyAndValue.Key,
        //            ParameterName = parameter.ParameterName,
        //            Calculated = calculated,
        //        };

        //        responseItems.Add(eventTableWidgetResponseItem);
        //    }


        //    return responseItems;

        //    double? SelectorProperty(WidgetTableParameterType widgetTableParameterType, EventComponent eventComponent)
        //    {
        //        switch (widgetTableParameterType)
        //        {
        //            case WidgetTableParameterType.Deviation:
        //                return eventComponent.Daviation;

        //            case WidgetTableParameterType.Duration:
        //                return eventComponent.DurationMilliSeconds;

        //            case WidgetTableParameterType.Value:
        //                return eventComponent.Value;

        //            default:
        //                throw new NotImplementedException($"In Event the options can be only of type {nameof(WidgetTableParameterType)}");
        //        }
        //    }
        //}

        public async Task<IEnumerable<BarCharComponentResponse>> CalculateBarChartAsync(string url, string session, BarChartRequest input) => null;

        private IEnumerable<TableWidgetResponseItem> ArrangingForTable(IEnumerable<GraphParametersComponentDtoV3> graphes, string quantity, string parameterName, string? TagName = null, string? TagValue = null)
        {
            var result = new List<TableWidgetResponseItem>();
            foreach (var graph in graphes)
            {
                var item = ArrangingForTable(graph, parameterName, quantity, TagName, TagValue);
                result.Add(item);
            }

            return result;
        }

        private TableWidgetResponseItem ArrangingForTable(BasicValue calculated, string? componentId, string? feederId, string parameterName, string quantity, DataUnitType dataType, string? TagName = null, string? TagValue = null, MissingBaseParameterInfo missingBaseParameterInfo = null)
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

        private TableWidgetResponseItem ArrangingForTable(GraphParametersComponentDtoV3 graph, string parameterName, string quantity, string? TagName = null, string? TagValue = null)
        {
            //double?  value = 
            var componentId = graph.Feeders.FirstOrDefault()?.ComponentId;
            var feederId = graph.Feeders.FirstOrDefault()?.Id;
            return ArrangingForTable(graph.FirstValue(), componentId?.ToString(), feederId?.ToString(), parameterName, quantity, graph.DataUnitType, TagName, TagValue, graph.MissingInformation?.FirstOrDefault());
        }

        private async Task SendingAndStoreingDataAsync(string url, string session, DateTime startDatetime, DateTime endDatetime, (bool isNominalCalculate, double? nominalValue) calculationData, IEnumerable<BaseParameterComponent> paramComponents)
        {
            //Should be refactored!!!!
            if (paramComponents.IsCollectionEmpty())
            {
                return;
            }

            var start = new PQZDateTime(startDatetime);
            var end = new PQZDateTime(endDatetime);


            using (var mainLogger = PqbiStopwatch.AnchorAsync(nameof(SendingAndStoreingDataAsync), Logger))
            {
                //FeeerID null should be taken underc onsidaration.
                var groups = paramComponents.GroupBy(p => new { p.ComponentID }).ToArray();
                var requests = new List<(PQSGetBaseDataRequest, IEnumerable<BaseParameterComponent>)>();
                var getBaseDataInfoInputs = new List<GetBaseDataInfoInput>();

                var basParameterIndexer = new Dictionary<Guid, List<BaseParameterComponent>>();   //Key = CompId

                foreach (var group in groups)
                {
                    var measurementParameters = new List<MeasurementParameterBase>();
                    var queue = new List<BaseParameterComponent>();

                    for (int index = 0; index < group.Count(); index++)
                    //foreach (BaseParameterComponent parameterComponent in group)
                    {
                        var parameterComponent = group.ElementAt(index);
                        var calculationItem = new CalculationCacheItem { ComponentId = parameterComponent.ComponentID, FeederId = parameterComponent.FeederId, Start = start.DateTimeUTC, End = end.DateTimeUTC, Parameter = parameterComponent.MeasurementParameter.ToString() };

                        if (calculationItem.TryGetCalculationCache(_cacheManager, out var cache))
                        {
                            parameterComponent.SetRawData(cache.PQBIAxisData, calculationData.isNominalCalculate, calculationData.nominalValue);
                            mainLogger.LogInformation($"Cache used {parameterComponent.ParameterId}");
                            continue;
                        }

                        measurementParameters.Add(parameterComponent.MeasurementParameter);
                        queue.Insert(0, parameterComponent);
                    }

                    if (queue.Count > 0)
                    {
                        var guid = group.First().ComponentID;
                        var input = new GetBaseDataInfoInput(guid, start.TicksPQZTimeFormat, end.TicksPQZTimeFormat, measurementParameters, CalculationTypeEnum.AUTOMATIC);
                        basParameterIndexer.Add(guid, queue);
                        getBaseDataInfoInputs.Add(input);
                    }
                }

                if (getBaseDataInfoInputs.SafeAny())
                {
                    var request = new PQSGetBaseDataRequest(session, getBaseDataInfoInputs.ToArray());
                    request.ID = Guid.NewGuid();

                    using (var sendingLogger = mainLogger.CreateSubLogger($"SendingToScada)"))
                    {

                        sendingLogger.LogInformation($"xxx Sending {request.ID} url={url}");
                        var response = await SendRecordsContainerPostBinaryRequestAndException(url, request);
                        sendingLogger.LogInformation($"xxx receiving {request.ID}");

                        var ptr = PQZxmlWriter.WriteMessage(request, true);
                        var getBaseResponse = new PQSGetBaseDataResponse(request, response);

                        getBaseResponse.ExtractGetParametersOrError(out IEnumerable<PQBIAxisData> axisses);

                        foreach (var axise in axisses.ToArray())
                        {
                            sendingLogger.LogInformation($"Send {axise}");

                            if (basParameterIndexer.TryGetValue(axise.ComponentId, out var baseParameterComponents))
                            {
                                var baseParameter = baseParameterComponents.FirstOrDefault(x => x.MeasurementParameter.ToString() == axise.ParameterName);
                                if (baseParameter is not null)
                                {
                                    if (axise is null)
                                    {

                                    }
                                    baseParameter.SetRawData(axise, calculationData.isNominalCalculate, calculationData.nominalValue);
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
                }
            }
        }


    }
}

