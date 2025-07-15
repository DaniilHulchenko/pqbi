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
using PQS.Data.Events.Enums;
using PQS.CommonUI.Data;
using PQS.CommonUI.Utils;
using PQS.Data.Events;
using PQS.Data.Events.Filters;
using System.Collections.Generic;
using PQS.Data.RecordsContainer.Records;
using PQS.Data.RecordsContainer;
using System.ComponentModel;
using Twilio.TwiML.Voice;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PQS.Data.Networks;
using PQS.Data.Events.Utility;
using Stripe;
using Task = System.Threading.Tasks.Task;
using PQS.Data.Configurations;
using PQS.Data.Common.Values;
using PQS.Data.Configurations.SystemElectricalMapping;
using PQS.Data.Configurations.Utilities;
using PayPalCheckoutSdk.Orders;
using PQBI.Infrastructure.Lockers;
using PQS.CommonUI.Enums;
using Abp.Events.Bus;
using System.Collections.ObjectModel;
using Castle.MicroKernel.Registration;
using Newtonsoft.Json.Linq;
using MimeKit;
using PQS.Data.Configurations.Tag;
using PQS.Data.Measurements.Utils;
using PQS.Data.Common.Units;
using PQBI.CustomParameters;
using System.Collections;
using Twilio.Rest.Api.V2010.Account;
using PQS.Data.Common.Extensions;
using Microsoft.VisualBasic;
//using System.Diagnostics.Eventing.Reader;


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

        public async Task<TrendResponse> CalculateTrendChartAsync(string url, string session, TrendCalcRequest input)
        {
            var response = new TrendResponse();
            var calculationDataItems = new PqbiSafeEntityLockerSlim<List<CalculatedDataItem>>([]);
            var timeStamps = new PqbiSafeEntityLockerSlim<List<long>>([]);

            if (string.IsNullOrEmpty(session))
            {
                throw new UserFriendlyException(nameof(session), "Cant be null");
            }

            IEnumerable<TrendParameter> parameters = GetParameterBundle(input);

            using (var mainLogger = PqbiStopwatch.AnchorAsync($"Trender - {input.WidgetName} {nameof(CalculateTrendChartAsync)}", Logger))
            {
                //var list = new List<Task>();
                foreach (TrendParameter parameter in parameters)
                {
                    //var task = Task.Run(async () =>
                    {
                        using (var subLogger = mainLogger.CreateSubLogger("Parameter Calculation"))
                        {

                            var graphes = await CalculateTrendChartIntristicAsync(url, session, input, parameter);

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

        private async Task<IEnumerable<GraphParametersComponentDtoV3>> CalculateTrendChartIntristicAsync(string url, string session, TrendCalcRequest input, TrendParameter parameter)
        {
            var result = new List<GraphParametersComponentDtoV3>();

            TrendWidgetParameterType customParameterType = CalculationStaticTypes.GetCustomParameterTrendType(parameter.Type);

            switch (customParameterType)
            {
                case TrendWidgetParameterType.CustomParameter:

                    TrendCustomWidgetData customWidgetData = parameter.CustomData;
                    var customParameterId = customWidgetData.Id;

                    var calculationNode = await AssembleCustomParameterTree(customParameterId, url, session, input.StartDate, input.EndDate, input.ResolutionInSeconds, input.IsAutoResolution, parameter.CustomData.Quantity, parameter.Feeders, false);
                    var results = _engineControllerService.RootCalculation(calculationNode);
                    result.AddRange(results);

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

                    await SendingAndStoringDataAsync(url, session, input.StartDate, input.EndDate, (false, null), root.BaseParameterComponents, null);
                    //var baseParameterGraph = _engineControllerService.FullCalculation(root);
                    //result.Add(baseParameterGraph);


                    var baseParameterGraphes = _engineControllerService.RootCalculation(root);
                    result.AddRange(baseParameterGraphes);

                    return result;


                case TrendWidgetParameterType.Exception:

                    TrendCustomWidgetData exceptionCustomWidgetData = parameter.CustomData;
                    var exceptionCustomParameterId = exceptionCustomWidgetData.Id;

                    var exceptionnode = await AssembleCustomParameterTree(exceptionCustomParameterId, url, session, input.StartDate, input.EndDate, input.ResolutionInSeconds, input.IsAutoResolution, parameter.CustomData.Quantity, [], false);
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
                        if (multiCustomParameter.TryGetValue(param.CustomData.Id, out var val) == true)
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
              int widgetResolutionInSeconds,
              bool isAutoResolution,
              string parameterQuantity,
              IEnumerable<FeederComponentInfo> feeders,
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

                // construct this node
                var node = new CustomParameterNodeCalculator(
                    CalculationStaticTypes.GetCustomParameterType(customParameter.Type),
                    customParameter.ResolutionInSeconds,
                    isAutoResolution,
                    outerAggregationFunction,
                    start,
                    end,
                    widgetResolutionInSeconds,
                    parameterQuantity,
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

                var reqs = baseParams.CreateBaseParameterComponents(feeders);
                await SetAndCalculateInnerAggregation(url, session, start, end, node, reqs, advancedSettings);

                // 2b) outer-aggregation
                node.CalculatedParameterOuterMatrixAndAggregation(isIgnoreAligningFunction);
            

                node.AddFinalMatrixCalculation();

                //// 2c) final leaf vs. internal call
                //if (innerSpecs.Any())
                //    _engineControllerService.AddFinalMaxtrixCalculationWithChildren(node);
                //else
                //    _engineControllerService.CalculateFinalMatrixChildless(node);

                return node;
            }

            var customParameter = GetCustomParameter(customParameterId);
            CustomParameterType customParameterType = CalculationStaticTypes.GetCustomParameterType(customParameter.Type);
            if (customParameterType == CustomParameterType.MPSC)
            {
                List<CustomParameterNodeCalculator> customParameterNodeCalculatorList = new List<CustomParameterNodeCalculator>();
                foreach (var feeder in feeders)
                {
                    CustomParameterNodeCalculator nCalculator = await BuildAsync(customParameterId, [feeder], null);
                    customParameterNodeCalculatorList.Add(nCalculator);
                }
                CustomParameterNodeCalculator nodeCalculator = customParameterNodeCalculatorList.First();
                for (int i = 1; i < customParameterNodeCalculatorList.Count; i++)
                {
                    nodeCalculator.FinalAggregationMatrixes.AddRange(customParameterNodeCalculatorList[i].FinalAggregationMatrixes);
                    nodeCalculator.ParameterMatrixes.AddRange(customParameterNodeCalculatorList[i].ParameterMatrixes);
                }
                return nodeCalculator;
            }
            else
                return await BuildAsync(customParameterId, feeders, null);
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


        private async Task SetAndCalculateInnerAggregation(string url, string session, DateTime start, DateTime end, CustomParameterNodeCalculator node, IEnumerable<BaseParameterComponent> ptr, AdvancedSettings? advancedSettings)
        {
            node.PopulateWithBaseParameterComponents(ptr);
            await SendingAndStoringDataAsync(url, session, start, end, (false, null), ptr, advancedSettings?.FiltersGroup);
            node.CalculatedInnerAlignment(ptr, advancedSettings);
        }


        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------


        public async Task<TableWidgetResponse> CalculateTableAsync(string url, string session, TableWidgetRequest input)
        {
            var responseItems = new List<TableWidgetResponseItem>();
            var paramComponents = new List<BaseParameterComponent>();

            List<ColumnWidgetTable> eventColWidgetTableList = new List<ColumnWidgetTable>();
            List<ColumnWidgetTable> otherColWidgetTableList = new List<ColumnWidgetTable>();
            Dictionary<Guid, Dictionary<FiltersGroup, HashSet<BaseParameterComponent>>> baseParametersHashSet = new Dictionary<Guid, Dictionary<FiltersGroup, HashSet<BaseParameterComponent>>>();

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
                                case TableWidgetParameterType.Event:
                                    //In Event only count can be Nadav H.=
                                    {
                                        eventColWidgetTableList.Add(parameter);
                                    }
                                    break;
                                default:

                                    otherColWidgetTableList.Add(parameter);

                                    if (widgetTableType == TableWidgetParameterType.BaseParameter)
                                    {
                                        var baseParameter = JsonConvert.DeserializeObject<BaseParameter>(parameter.BaseData);
                                        {
                                            foreach (var feeder in input.Rows.Feeders)
                                            {
                                                var parameterComponents = baseParameter.CreateBaseParameterComponents([feeder]);

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
                                                    filterGroupToMsrPrmSetMap = new Dictionary<FiltersGroup, HashSet<BaseParameterComponent>>();
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

                                    break;
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

                List<TableWidgetResponseItem> tableEventWidgetResponseItemList = null;
                if (eventColWidgetTableList.IsCollectionExists())
                {
                    tableEventWidgetResponseItemList = await WidgetTableEventCalculation(url, session, input, eventColWidgetTableList, input.StartDate, input.EndDate);
                    responseItems.AddRange(tableEventWidgetResponseItemList);
                }

                if (baseParametersHashSet.IsCollectionExists())
                {
                    foreach (var keyAndValue in baseParametersHashSet)
                    {
                        var filterAndParameterComponents = keyAndValue.Value;
                        foreach (var item in filterAndParameterComponents)
                        {
                            FiltersGroup filterGroup = item.Key;
                            HashSet<BaseParameterComponent> prmCompSet = item.Value;
                            await SendingAndStoringDataAsync(url, session, input.StartDate, input.EndDate, (false, null), prmCompSet, filterGroup);
                        }
                    }
                }

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

                                    var items = await CustomParameterCreateTableNodeAsync(url, session, input, parameter, advancedSettings);
                                    responseItems.AddRange(items);

                                    break;

                                case TableWidgetParameterType.BaseParameter:

                                    var baseParamaterItems = await BaseParameterCreateTableNodeAsync(url, session, input, parameter, advancedSettings);
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

        private void CustomParameterTableValidate(TableWidgetRequest input, CustomParameters.CustomParameter customParameter)
        {
            var customParameterType = CalculationStaticTypes.GetCustomParameterType(customParameter.Type);

            if (customParameterType == CustomParameterType.Exception)
            {

                throw new UserFriendlyException("In table exception mode is not allowrd.");
            }
        }


        private async Task<IEnumerable<TableWidgetResponseItem>> CustomParameterCreateTableNodeAsync(string url, string session, TableWidgetRequest input, ColumnWidgetTable parameter, AdvancedSettings? advancedSettings = null)
        {
            var customParameterId = parameter.CustomData.Id;

            var customParameter = GetCustomParameter(customParameterId);
            CustomParameterTableValidate(input, customParameter);
            Func<IEnumerable<FeederComponentInfo>, bool, Task<IEnumerable<CustomParameterNodeCalculator>>> selector = async (feeders, isTag) =>
            {
                var nodes = await AssembleCustomParameterTree(customParameterId, url, session, input.StartDate, input.EndDate, -1, false, parameter.CustomData.Quantity, feeders, isTag, advancedSettings);
                //var nodes = await AssembleCustomParameterTree(customParameterId, url, session, input.StartDate, input.EndDate, -1, false, parameter.CustomData.Quantity, feeders);
                return [nodes];
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

        private async Task<IEnumerable<TableWidgetResponseItem>> BaseParameterCreateTableNodeAsync(string url, string session, TableWidgetRequest input, ColumnWidgetTable parameter, AdvancedSettings? advancedSettings = null)
        {
            var baseParameter = JsonConvert.DeserializeObject<BaseParameter>(parameter.BaseData);
            //baseParameter.SetISXResolution(input.StartDate, input.EndDate);
            //    int totalSeconds = (int)(endDate - startDate).TotalSeconds;

            bool isNeedFilterByEvents = false;
            if (advancedSettings.IsExcludeFlaggedData)
                isNeedFilterByEvents = true;

            if (!isNeedFilterByEvents)
                baseParameter.Resolution = (int)((input.EndDate - input.StartDate).TotalSeconds);
            var node = new CustomParameterNodeCalculator(CustomParameterType.BPCP, -1, false, string.Empty, input.StartDate, input.EndDate, -1, baseParameter.Quantity, advancedSettingsForTable: advancedSettings);

            Func<IEnumerable<FeederComponentInfo>, bool, Task<IEnumerable<CustomParameterNodeCalculator>>> selector = async (feeders, isTag) =>
            {
                var parameterComponents = baseParameter.CreateBaseParameterComponents(feeders);
                node.PopulateWithBaseParameterComponents(parameterComponents);

                //SelectAssemble(node, feeders);

                await SendingAndStoringDataAsync(url, session, input.StartDate, input.EndDate, (false, null), parameterComponents, advancedSettings?.FiltersGroup);
              
                return [node];
            };

            var responseItems = await RealCalculateTableAsync(url, session, input, parameter, selector, baseParameter.Quantity);
            return responseItems;
        }


        private async Task<IEnumerable<TableWidgetResponseItem>> RealCalculateTableAsync(string url, string session, TableWidgetRequest input, ColumnWidgetTable parameter, Func<IEnumerable<FeederComponentInfo>, bool, Task<IEnumerable<CustomParameterNodeCalculator>>> calculationSelector, string quantity, AdvancedSettings? advancedSettings = null)
        {
            var responseItems = new List<TableWidgetResponseItem>();

            using (var mainLogger = PqbiStopwatch.AnchorAsync(nameof(RealCalculateTableAsync), Logger))
            {
                var feederMap = new Dictionary<FeederComponentInfo, GraphParametersComponentDtoV3?>();
                var customParameterType = CustomParameterType.BPCP;
                string outerAggFunction = null;

                //try
                {
                    foreach (var feeder in input.Rows.Feeders)
                    {
                        var nodes = await calculationSelector([feeder], false);
                        var node = nodes.First();
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

                        var graph = _engineControllerService.RootCalculation(node).First();
                        //var graph = _engineControllerService.FullCalculation(node);


                        if (graph.TryGetMissingParameterInfo(out var invalidParameter))
                        {
                            mainLogger.LogError($"{invalidParameter.PropertyName} failed with PQZStatus = {invalidParameter.Status}");
                        }

                        //responseItems.AddRange(ArrangingForTable([graph], quantity, parameter.ParameterName));

                        var responseItem = ArrangingForTable(graph.FirstValue(), feeder.ComponentId.ToString(), feederId, parameter.ParameterName, quantity, graph.DataUnitType, missingBaseParameterInfo: graph.MissingInformation?.FirstOrDefault());
                        responseItems.Add(responseItem);
                        feederMap[feeder] = graph;

                    }
                }
                //catch (Exception ex)
                {
                }

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

                            responseItems.AddRange(ArrangingForTable(graphes, quantity, parameter.ParameterName, tag.Id, tag.Name));
                        }
                        else
                        {
                            string tagQuantity = quantity;
                            if (customParameterType == CustomParameterType.MPSC)
                                tagQuantity = outerAggregationFunction;

                            CalculateForMltiAndBaseParameter(feederMap, tag.Feeders, out var calculated, tagQuantity, out var missingBaseParameterInfo);
                            var responseItem = ArrangingForTable(calculated, null, null, parameter.ParameterName, quantity, new EmptyDataUnitType(), tag.Id, tag.Name, missingBaseParameterInfo: missingBaseParameterInfo);
                            responseItems.Add(responseItem);
                        }
                    }
                }
                //catch (Exception ex)
                {
                }
            }

            return responseItems;          
        }

        bool CalculateForMltiAndBaseParameter(Dictionary<FeederComponentInfo, GraphParametersComponentDtoV3?> fMap, IEnumerable<FeederComponentInfo> list, out BasicValue calculated, string quantity, out MissingBaseParameterInfo missingBaseParameterInfo)
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
            List<TableWidgetResponseItem> tableWidgetResponseItemList = new List<TableWidgetResponseItem>();

            List<TableWidgetResponseItem> feedersTableWidgetResponseItemList = await PopulateEventsValForFeedersInTable(url, eventColWidgetTableList, sessionID, startDate, endDate, input.Rows.Feeders, columnToFeederEventsMap, columnToFeederEventsResMap, input.Rows.Tags.Count);
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
                        calculatedTagVal = Compute(colEvList, tableWidgetEvent.Parameter, quantityType, normalizeBy);
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
                        DataUnitType = new EmptyDataUnitType()  // Example usage
                    };                    
                    tableWidgetResponseItemList.Add(tableResItem);
                }                             
            }

            return tableWidgetResponseItemList;
        }

        //private async Task<List<TableWidgetResponseItem>> PopulateEventsValForFeedersInTable(string url, List<ColumnWidgetTable> eventColWidgetTableList, Guid sessionID, PQZDateTime startDate, PQZDateTime endDate, List<FeederComponentInfo> feederComponentInfoList, string tagID, string tagValue)
        private async Task<List<TableWidgetResponseItem>> PopulateEventsValForFeedersInTable(string url, List<ColumnWidgetTable> eventColWidgetTableList, Guid sessionID, PQZDateTime startDate, PQZDateTime endDate, List<FeederComponentInfo> feederComponentInfoList, Dictionary<string, Dictionary<FeederComponentInfo, List<PQEvent>>> columnToFeederEventsMap, Dictionary<string, Dictionary<FeederComponentInfo, double>> columnToFeederEventsResMap, int tagCount)
        {
            TableWidgetResponseItem tableResItem = null;
            Dictionary<string, List<PQEvent>> prmNameToEventsMap = new Dictionary<string, List<PQEvent>>();
            //Dictionary<string, Dictionary<string, List<PQEvent>>> compPrmNameToEventsMap = new Dictionary<string, Dictionary<string, List<PQEvent>>>();

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
                    ColumnWidgetTable columnWidgetTable = eventColWidgetTableList[colNum];
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

                double calcValue = 0;
                if (recBase is EventsRecord)
                {
                    EventsRecord evRec = recBase as EventsRecord;

                    EventsContainer eventsContainer = evRec.GetEventsContainer();
                    ICollection<EventBase> eventBaseCollection = eventsContainer.GetAllEvents();

                    List<PQEvent> compPQEventList = eventBaseCollection.Cast<PQEvent>().ToList();

                    for (int colNum = 0; colNum < eventColWidgetTableList.Count; colNum++)
                    {
                        ColumnWidgetTable columnWidgetTable = eventColWidgetTableList[colNum];
                        TableWidgetEvent tableWidgetEvent = columnWidgetTable.TableEvent;

                        uint confID = tableWidgetEvent.EventId;
                        EventClass eventClassEnum = tableWidgetEvent.EventClass;
                        int eventConfID = (int)confID;
                        (EventClass, GeneratedByEnum) classGeneratedByTuple;
                        confIDToGeneratedByMap.TryGetValue(eventConfID, out classGeneratedByTuple);
                        GeneratedByEnum generatedByEnum = classGeneratedByTuple.Item2;
                        Enum.TryParse<PQBIQuantityType>(tableWidgetEvent.Quantity.ToLower(), out PQBIQuantityType quantityType);

                        if (generatedByEnum == GeneratedByEnum.NotCalculated)
                        {
                            PrepareEventDataForTagCalculation(columnToFeederEventsMap, columnToFeederEventsResMap, tagCount, feederComponentInfo, calcValue, columnWidgetTable, new List<PQEvent>(), quantityType);
                            tableResItem = new TableWidgetResponseItem()
                            {
                                FeederId = feederID,
                                ComponentId = feederCompID.ToString(),
                                ParameterName = columnWidgetTable.ParameterName,
                                DataUnitType = new EmptyDataUnitType(),
                                Calculated = 0
                            };

                            tableWidgetResponseItemList.Add(tableResItem);
                            continue;
                        }

                        List<PQEvent> pqEventList = new List<PQEvent>();
                        foreach (PQEvent pqEvent in compPQEventList)
                        {
                            if (pqEvent.Class != eventClassEnum)
                                continue;

                            if (generatedByEnum == GeneratedByEnum.PQServer)
                            {
                                if (pqEvent.GeneratedBy != GeneratedByEnum.PQServer.ToString())
                                    continue;
                                else if (pqEvent.ConfigurationID != eventConfID)
                                    continue;
                            }
                            else if (pqEvent.GeneratedBy == GeneratedByEnum.PQServer.ToString())
                                continue;

                            bool isEventInFeederNetwork = IsEventInFeederNetwork(pqEvent, (uint)feederID, 600);
                            if (!isEventInFeederNetwork)
                                continue;

                            if (!IsEventInPhase(pqEvent, topologyType, tableWidgetEvent.Phases))
                                continue;

                            pqEventList.Add(pqEvent);
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

                        calcValue = Compute(pqEventList, tableWidgetEvent.Parameter, quantityType, normalizeBy);
                        PrepareEventDataForTagCalculation(columnToFeederEventsMap, columnToFeederEventsResMap, tagCount, feederComponentInfo, calcValue, columnWidgetTable, pqEventList, tagQuantityType);
                        tableResItem = new TableWidgetResponseItem()
                        {
                            ComponentId = feederCompID.ToString(),
                            FeederId = feederID,
                            ParameterName = columnWidgetTable.ParameterName,
                            Quantity = tableWidgetEvent.Quantity,
                            Calculated = calcValue,
                            DataUnitType = new EmptyDataUnitType()
                        };
                        tableWidgetResponseItemList.Add(tableResItem);
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

        private static void PrepareEventDataForTagCalculation(Dictionary<string, Dictionary<FeederComponentInfo, List<PQEvent>>> columnToFeederEventsMap, Dictionary<string, Dictionary<FeederComponentInfo, double>> columnToFeederEventsResMap, int tagCount, FeederComponentInfo feederComponentInfo, double calcValue, ColumnWidgetTable columnWidgetTable, List<PQEvent> pqEventList, PQBIQuantityType quantityType)
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

        public static double Compute(
                   IReadOnlyCollection<PQEvent> events,
                   WidgetTableParameterType parameter,
                   PQBIQuantityType quantity,
                   double normalizeBy,
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

            return res / normalizeBy;
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

        private TableWidgetResponseItem ArrangingForTable(GraphParametersComponentDtoV3 graph, string parameterName, string quantity, string? TagName = null, string? TagValue = null)
        {
            //double?  value = 
            var componentId = graph.Feeders.FirstOrDefault()?.ComponentId;
            var feederId = graph.Feeders.FirstOrDefault()?.Id;
            return ArrangingForTable(graph.FirstValue(), componentId?.ToString(), feederId, parameterName, quantity, graph.DataUnitType, TagName, TagValue, graph.MissingInformation?.FirstOrDefault());
        }

        private async Task SendingAndStoringDataAsync(string url, string session, DateTime startDatetime, DateTime endDatetime, (bool isNominalCalculate, double? nominalValue) calculationData, IEnumerable<BaseParameterComponent> paramComponents, FiltersGroup? filterGroup)
        {
            //Should be refactored!!!!
            if (paramComponents.IsCollectionEmpty())
            {
                return;
            }

            var start = new PQZDateTime(startDatetime);
            var end = new PQZDateTime(endDatetime);


            using (var mainLogger = PqbiStopwatch.AnchorAsync(nameof(SendingAndStoringDataAsync), Logger))
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
                        var calculationItem = new CalculationCacheItem { ComponentId = parameterComponent.ComponentID, FeederId = parameterComponent.FeederId, Start = start.DateTimeUTC, End = end.DateTimeUTC, Parameter = parameterComponent.MeasurementParameter.ToString(), FiltersGroup = filterGroup };

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
                        var input = new GetBaseDataInfoInput(guid, start.TicksPQZTimeFormat, end.TicksPQZTimeFormat, measurementParameters, CalculationTypeEnum.AUTOMATIC, filtersGroup: filterGroup);
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

#if DEBUG

                        var ptr = PQZxmlWriter.WriteMessage(request, true);

#endif

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
                                    //if (axise is null)
                                    //{

                                    //}
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
                }
            }
        }


    }
}