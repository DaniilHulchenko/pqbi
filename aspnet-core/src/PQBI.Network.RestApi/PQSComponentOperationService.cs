using Abp.Events.Bus.Handlers;
using Abp.UI;
using Castle.Core.Logging;
using Microsoft.Extensions.Logging;
using PQBI.CalculationEngine;
using PQBI.Friendships.Dto;
using PQBI.Infrastructure.Extensions;
using PQBI.Infrastructure.Sapphire;
using PQBI.IntegrationTests.Scenarios.PopulatingParameters;
using PQBI.Network.Base;
using PQBI.Network.RestApi.EngineCalculation;
using PQBI.Network.RestApi.Validations;
using PQBI.PQS;
using PQBI.PQS.CalcEngine;
using PQBI.Requests;
using PQBI.Sapphire;
using PQBI.Tenants.Dashboard.Dto;
using PQS.CommonUI.Data;
using PQS.CommonUI.Enums;
using PQS.Data.Common.Extensions;
using PQS.Data.Common.Values;
using PQS.Data.Configurations;
using PQS.Data.Configurations.Enums;
using PQS.Data.Events.Enums;
using PQS.Data.Measurements;
using PQS.Data.Measurements.CustomParameter;
using PQS.Data.Measurements.Enums;
using PQS.Data.Measurements.StandardParameter;
using PQS.Data.Networks;
using PQS.Data.RecordsContainer;
using PQS.Data.RecordsContainer.Records;
using PQS.PQZxml;
using PQS.Translator;
using PQZTimeFormat;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using Twilio.TwiML.Voice;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace PQBI.Network.RestApi;

public interface IPQSComponentOperationService
{
    static string Alias = IPQSServiceBase.Alias;
    Task<string> GetBaseParameterName(BaseParameterNameSlim request);

    //string GetLegendName();

    Task<ComponentsWithTagsDto> GetAllComponentsWithTagsAsync(string url, string sessionId);
    Task<GetTagsConfigurationResponse> GetAllTagsAsync(string url, string sessionId);
    Task<ComponentsWithTagsDto> GetObjectAsync(string url, string sessionId);
    //Task<PQSOutput> GetBaseDataAsync(string url, string session);
    Task<PQSOutput> GetBaseDataConcurrentAsync(string url, string session, PQSInput request);
    Task<PQSOutput> GetBaseDataAsync_Original(string url, string session, PQSInput request);
    Task<ComponentWithTagsResponse> GetAllTags(string url, string session);
    Task<IEnumerable<EventClassDescription>> GetEventsTypeAsync(string url, string session);
    Task<IEnumerable<PQSEventDto[]>> GetEventss(string url, string session, GetEventstRequest request);
    Task<IEnumerable<ComponentSlimDto>> GetAllComponentSlimsAsync(string url, string session);
    Task<StaticDataInfo> GetStaticTree(string url, string session);
}

public class ComponentsWithTagsDto
{
    public IEnumerable<PQS.ComponentDto> Components { get; init; }

    public GetTagsConfigurationResponse GetTagsConfigurationResponse { get; init; }
}

public class PQSComponentOperationService : PQSRestApiServiceBase, IPQSComponentOperationService
{
    private readonly ITaskOrchestrator _taskOrchestrator;
    private readonly IFunctionEngine _functionEngine;
    private readonly IEngineCalculationService _engineControllerService;

    public PQSComponentOperationService(ILogger<PQSComponentOperationService> logger,
        IHttpClientFactory httpClientFactory,
        IPQZBinaryWriterWrapper pQZBinaryWriterCore,
        IPQSenderHelper pQSenderHelper,
        ITaskOrchestrator taskOrchestrator,
        IFunctionEngine functionEngine,
        IEngineCalculationService engineControllerService) : base(httpClientFactory, pQZBinaryWriterCore, pQSenderHelper, logger)
    {
        _taskOrchestrator = taskOrchestrator;
        _functionEngine = functionEngine;
        _engineControllerService = engineControllerService;
    }


    protected override string ClientAlias => IPQSComponentOperationService.Alias;

    public async Task<string> GetBaseParameterName(BaseParameterNameSlim request)
    {
        MeasurementParameterBase parameter = null;

        var baseParameter = request.ToBaseParameter();

        // Since Dima sends without Resolution....
        if (request.Resolution is null)
        {
            baseParameter.Resolution = 1;
            //baseParameter.Resolution = "IS1SEC";
        }

        if (request.IsLogical)
        {

            parameter = BaseParameterComponentHelper.GetFeederParameter(baseParameter, request.FeederId ?? "1");
        }
        else
        {
            parameter = BaseParameterComponentHelper.GetChannelParameter(baseParameter);
            //var @params = ConstructParameterChannel(parameter.Group, parameter.ResolutionInSeconds.ToString(), parameter.Base, parameter.ScadaQuantityName, parameter.Phase);

            //msrParm = MeasurementParameterFactory.GenerateNewMesurmentParameterWithoutSplit(@params);
        }

        QuantityType quantityType = QuantityType.Avg;
        var quantity = baseParameter.Quantity.ToLower();
        if (quantity.StartsWith("max"))
        {
            quantityType = QuantityType.Max;
        }
        else
        {
            if (quantity.StartsWith("avg"))
            {
                quantityType = QuantityType.Avg;

            }
            else
            {
                if (quantity.StartsWith("sum"))
                {
                    quantityType = QuantityType.Sum;
                }
                else
                {
                    if (quantity.StartsWith("min"))
                    {
                        quantityType = QuantityType.MinMax;
                    }
                }
            }
        }

        var name = PqbiMeasurementParameterHelper.GetLegendName(parameter, quantityType);
        if (name.EndsWith("/Max"))
        {
            name = name.Substring(0, name.Length - 4);
        }

        return name;
    }

    public async Task<IEnumerable<EventClassDescription>> GetEventsTypeAsync(string url, string session)    
    {       
        ConfigurationParameterBase eventsIdAndNameConf = StandardConfigurationMapping.Instance.GetParameterBase(StandardConfigurationEnum.STD_SUPPORTED_EVENTS_ID_AND_NAME);
        GetInstantConfigurationRecord getInstantConfigurationRecord = new GetInstantConfigurationRecord(Guid.Empty, [eventsIdAndNameConf]);

        var request = new PQSRequest(Guid.NewGuid(), new Guid(session));
        request.AddRecord(getInstantConfigurationRecord);       
        var pqsResponse = await SendRecordsContainerPostBinaryRequestAndException(url, request);

        PQSRecordBase rec = pqsResponse.GetRecord(0);

        if (rec is InstantConfigurationRecord instRec)
        {
            instRec.Configuration.TryGetConfigurationValue<ListValuesContainer<string>>(eventsIdAndNameConf, out ListValuesContainer<string> eventTypeList);
            IEnumerable<EventClassDescription> eventClassDescriptionList = ParseEventsCodeAndNames(eventTypeList);          

            return eventClassDescriptionList;
        }

        return null;
    }

    public static IEnumerable<EventClassDescription> ParseEventsCodeAndNames(ListValuesContainer<string> codesAndValues)
    {
        List<EventClassDescription> eventClassDescriptionList = new List<EventClassDescription>();
        //List<Tuple<int, string, EventClass, AggregationEnum, bool>> values = new List<Tuple<int, string, EventClass, AggregationEnum, bool>>();
        foreach (string cAndV in codesAndValues)
        {
            if (!string.IsNullOrEmpty(cAndV))
            {
                string[] splittedArray = cAndV.Split('&');
                if (splittedArray.Length > 1)
                {
                    int eventID;
                    if (int.TryParse(splittedArray[0], out eventID))
                    {
                        EventClassDescription eventClassDescription = new EventClassDescription() { ConfID = (uint)eventID };
                        EventClass ec = EventClass.None;
                        AggregationEnum aggregationEnum = AggregationEnum.NotAggregated;
                        bool isSharedEvent = false;
                        if (splittedArray.Length == 4)
                        {
                            Enum.TryParse<EventClass>(splittedArray[2], out ec);
                            aggregationEnum = (AggregationEnum)byte.Parse(splittedArray[3]);                           
                        }
                        else if (splittedArray.Length == 5)
                        {
                            Enum.TryParse<EventClass>(splittedArray[2], out ec);
                            aggregationEnum = (AggregationEnum)byte.Parse(splittedArray[3]);
                            if (!bool.TryParse(splittedArray[4], out isSharedEvent))
                            {
                                isSharedEvent = false;
                            }                         
                        }
                        else if (splittedArray.Length == 3)
                        {
                            Enum.TryParse<EventClass>(splittedArray[2], out ec);                           
                        }

                        eventClassDescription.EventClass = ec;
                        eventClassDescription.IsShared = isSharedEvent;
                        eventClassDescription.AggregationEnum = aggregationEnum;
                        eventClassDescription.Name = splittedArray[1];
                        eventClassDescription.Description = ec.Description();

                        eventClassDescriptionList.Add(eventClassDescription);                       
                    }
                }
            }
        }
        return eventClassDescriptionList;
    }

    public async Task<IEnumerable<PQSEventDto[]>> GetEventss(string url, string session, GetEventstRequest input)
    {
        var start = new PQZDateTime(input.Start);
        var end = new PQZDateTime(input.End);


        var request = new PQSGetEventRequest(session, start, end, [EventClass.EVENT_CLASSIFICATION_DIP, EventClass.EVENT_CLASSIFICATION_SWELL], "08c3912f-0275-4278-bf86-917168d88eef");

        var pqsResponse = await SendRecordsContainerPostBinaryRequestAndException(url, request);

        var respose = new PQSAddEventResponse(request, pqsResponse);
        var events = respose.Events;

        var tmp = input.ComponentIds.First();
        var componentId = Guid.Parse(tmp);

        var flickerRequest = new FlickerPQSGetBaseDataRequest(session, new GetBaseDataInfoInput(componentId, start.TicksPQZTimeFormat, end.TicksPQZTimeFormat, null, CalculationTypeEnum.FORCE_DB_DATA, 1));
        var flickerResponse = await SendRecordsContainerPostBinaryRequestAndException(url, flickerRequest);
        var getBaseDataResponse = new FlickerPQSGetBaseDataResponse(flickerRequest, flickerResponse);


        getBaseDataResponse.ExtractFlickersOrError(out var axisses, out var errors);
        //var debug = PQZxmlWriter.WriteMessage(flickerRequest, true);

        return events;
    }

    public async Task<ComponentsWithTagsDto> GetAllComponentsWithTagsAsync(string url, string session)
    {
        GetTagsConfigurationResponse getInstanceResponse = null;
        using (var mainLogger = PqbiStopwatch.AnchorAsync(nameof(GetAllComponentsWithTagsAsync), Logger))
        {
            getInstanceResponse = await GetAllTagsAsync(url, session);
            getInstanceResponse.TryGetMap(out var map, out _);

            PQSGetComponentsRequest request = null;
            PQSResponse pqsResponse = null;
            using (var subLogger = mainLogger.CreateSubLogger(nameof(GetAllTagsAsync)))
            {
                request = new PQSGetComponentsRequest(session);
                pqsResponse = await SendRecordsContainerPostBinaryRequestAndException(url, request);
            }

            List<PQS.ComponentDto> result = null;
            if (pqsResponse != null)
            {
                var response = new PQSGetComponentsResponse(request, pqsResponse);
                var tmppp = response.Status;
                result = response.Components?.Select(x => new ComponentDto(x.ComponentId, x.ComponentName, x.Feeders, x.Channels, x.AdditionalDatas)).ToSafeList<ComponentDto>();
            }

            foreach (var item in result.SafeArray())
            {
                if (map.TryGetValue(item.ComponentId, out var componentDto))
                {
                    item.Tags = componentDto.Tags.ToList();
                }
            }

            return new ComponentsWithTagsDto { Components = result, GetTagsConfigurationResponse = getInstanceResponse };
        }

    }

    public async Task<StaticDataInfo> GetStaticTree(string url, string session)
    {
        var tree = new StaticTreeNode { Value = StaticTreeNode.RootLabel, Description = StaticTreeNode.RootLabel };

        var logicalDataGenerator = new CreationLogicalOptions();
        var channelDataGenerator = new CreationChannelOptions();

        var logicalData = logicalDataGenerator.CreateDataAsync();
        tree.Children.Add(logicalData);


        var channelData = channelDataGenerator.CreateDataAsync();
        tree.Children.Add(channelData);


        var hashSet = new HashSet<AdditionalData>();
        var components = await GetAllComponentSlimsAsync(url, session);

        foreach (var component in components)
        {
            foreach (var additionalData in component.AdditionalDatas)
            {
                hashSet.Add(additionalData);
            }
        }


        return new StaticDataInfo { StaticTreeNode = tree, AdditionalDatas = hashSet };
    }

    public async Task<IEnumerable<ComponentSlimDto>> GetAllComponentSlimsAsync(string url, string session)
    {
        using (var mainLogger = PqbiStopwatch.AnchorAsync(nameof(GetAllComponentSlimsAsync), Logger))
        {
            var request = new PQSGetComponentsRequest(session); ;
            PQSResponse pqsResponse = await SendRecordsContainerPostBinaryRequestAndException(url, request);

            IEnumerable<PQS.ComponentSlimDto> result = null;
            if (pqsResponse != null)
            {
                var response = new PQSGetComponentsResponse(request, pqsResponse);
                result = response.Components;
            }

            return result;
        }
    }


    public async Task<GetTagsConfigurationResponse> GetAllTagsAsync(string url, string session)
    {
        using (var subLogger = PqbiStopwatch.AnchorAsync($"{nameof(GetAllTagsAsync)}", Logger))
        {
            var getInstanceRequest = new GetTagsConfigurationRequest(session);
            var getInstanceRawResponse = await SendRecordsContainerPostBinaryRequestAndException(url, getInstanceRequest);

            var getInstanceResponse = new GetTagsConfigurationResponse(getInstanceRequest, getInstanceRawResponse);

            return getInstanceResponse;
        }
    }

    public async Task<ComponentsWithTagsDto> GetObjectAsync(string url, string session)
    {
        string[] ids = null;
        ComponentsWithTagsDto componentsWithTags = null;

        using (var mainLogger = PqbiStopwatch.AnchorAsync(nameof(GetObjectAsync), Logger))
        {
            componentsWithTags = await GetAllComponentsWithTagsAsync(url, session);
            ids = componentsWithTags.Components.Select(x => x.ComponentId).ToArray();
        }


        var request = new PQSGetAllObjectsRequest(session, ids);
        var pqsResponse = await SendRecordsContainerPostBinaryRequestAndException(url, request);
        var response = new PQSGetAllObjectsResponse(request, pqsResponse);

        response.ExtractGetParametersOrError(out var parameters, out var errors);

        if (errors != null)
        {
            throw new UserFriendlyException(@"PQSGetAllObjectsResponse Faild");
        }

        foreach (var component in componentsWithTags.Components)
        {
            if (parameters.TryGetValue(component.ComponentId, out var paramList))
            {
                component.ParameterInfos = paramList.ToList();
            }
        }

        return componentsWithTags;
    }


    void SwitchPlaces(List<string> list, int start, int end)
    {
        var tmp = list[start];
        var index = 0;

        if (end < start)
        {
            list.RemoveAt(start);
            list.Insert(end, tmp);
        }
        else
        {
            //start < end		
            list.Insert(end, tmp);
            list.RemoveAt(start);
        }


    }



    private void CreateTreeFromParameter_Origin(string parameterName, StaticTreeNode tree)
    {
        if (string.IsNullOrEmpty(parameterName))
        {
            return;
        }

        var pattern = @"FEEDER_(\d+)";
        parameterName = Regex.Replace(parameterName, pattern, "FEEDER$1", RegexOptions.IgnoreCase);

        var @params = parameterName.Split('_');
        var currentNode = tree;

        foreach (var param in @params)
        {
            var childNode = currentNode.Children.FirstOrDefault(n => n.Value == param);

            if (childNode == null)
            {
                childNode = new StaticTreeNode { Value = param };
                currentNode.Children.Add(childNode);
            }

            currentNode = childNode;
        }
    }


    string GetSubstringUpToFirstHash(string input)
    {
        int index = input.IndexOf('#');
        if (index == -1)
        {
            return input;
        }
        return input.Substring(0, index);
    }



    public async Task<ComponentWithTagsResponse> GetAllTags(string url, string session)
    {

        var getInstanceRequest = new GetTagsConfigurationRequest(session);
        var getInstanceRawResponse = await SendRecordsContainerPostBinaryRequestAndException(url, getInstanceRequest);

        var getInstanceResponse = new GetTagsConfigurationResponse(getInstanceRequest, getInstanceRawResponse);
        getInstanceResponse.TryGetMap(out _, out var list);


        var result = new ComponentWithTagsResponse
        {
            Data = list.Select(x => new ComponentWithTagDtos { TagName = x.TagName, TagValue = x.TagValue, ComponentIds = x.ComponentIds.ToArray() }).ToArray()
        };

        return result;
    }

    public async Task<PQSOutput> GetBaseDataConcurrentAsync(string url, string session, PQSInput widgetRequest)
    {
        var axisDaa = new List<PQBIAxisData>();

        var start = new PQZDateTime(widgetRequest.StartDate);
        var end = new PQZDateTime(widgetRequest.EndDate);


        var dictionary = new Dictionary<string, List<(string ParameterName, string Quantity)>>();

        foreach (var parameter in widgetRequest.CustomParameter.Value.ParameterList)
        {
            var list = default(List<(string ParameterName, string Quantity)>);
            if (dictionary.TryGetValue(parameter.ComponentId, out list) == false)
            {
                list = dictionary[parameter.ComponentId] = new List<(string ParameterName, string Quantity)>();
            }

            list.Add((parameter.ParamName, parameter.Quantity));
        }

        var requests = new List<(Func<Task<PQSResponse>>, PQSGetBaseDataRequest)>();
        foreach (var keyAndVal in dictionary)
        {
            var mesurnmentParameters = new List<MeasurementParameterBase>();
            foreach (var paramName in keyAndVal.Value)
            {
                var prmAndQuantities = ConstructParameter(paramName.ParameterName, widgetRequest.Resolution, paramName.Quantity);

                var msrParm = MeasurementParameterFactory.GenerateRealtimeMesuarmentParameterWithoutSplit(prmAndQuantities);
                mesurnmentParameters.Add(msrParm);
            }

            var request = new PQSGetBaseDataRequest(session, new GetBaseDataInfoInput(Guid.Parse(keyAndVal.Key), start.TicksPQZTimeFormat, end.TicksPQZTimeFormat, mesurnmentParameters, CalculationTypeEnum.FORCE_DB_DATA));
            requests.Add((async () => await SendRecordsContainerPostBinaryRequestAndException(url, request), request));
            //var tmp = PQZxmlWriter.WriteMessage(request, true);
        }

        var pqsResponses = await _taskOrchestrator.DispatchBatch(requests.Select(x => x.Item1).ToArray());

        for (int index = 0; index < requests.Count; index++)
        {
            var response = new PQSGetBaseDataResponse(requests[index].Item2, pqsResponses[index]);
            response.ExtractGetParametersOrError(out var axisses);

            //if (errors is not null)
            //{
            //    throw new UserFriendlyException((int)errors.Status, "to_translate", "From ErrorRecord");
            //}

            axisDaa.AddRange(axisses);
        }

        //var response = new PQSGetBaseDataResponse(request, pqsResponses.First());
        //response.ExtractGetParametersOrError(out var axisses, out var errors);

        //if (errors is not null)
        //{
        //    throw new UserFriendlyException((int)errors.Status, "to_translate", "From ErrorRecord");
        //}

        //axisDaa.AddRange(axisses);

        return new PQSOutput
        {
            Data = axisDaa.ToArray()
        };
    }


    public async Task<PQSOutput> GetBaseDataAsync_Original(string url, string session, PQSInput widgetRequest)
    {
        var axisDaa = new List<PQBIAxisData>();

        var start = new PQZDateTime(widgetRequest.StartDate);
        var end = new PQZDateTime(widgetRequest.EndDate);


        var dictionary = new Dictionary<string, List<(string ParameterName, string Quantity)>>();

        foreach (var parameter in widgetRequest.CustomParameter.Value.ParameterList)
        {
            var list = default(List<(string ParameterName, string Quantity)>);
            if (dictionary.TryGetValue(parameter.ComponentId, out list) == false)
            {
                list = dictionary[parameter.ComponentId] = new List<(string ParameterName, string Quantity)>();
            }

            list.Add((parameter.ParamName, parameter.Quantity));
        }

        foreach (var keyAndVal in dictionary)
        {
            var mesurnmentParameters = new List<MeasurementParameterBase>();
            foreach (var paramName in keyAndVal.Value)
            {
                var prmAndQuantities = ConstructParameter(paramName.ParameterName, widgetRequest.Resolution, paramName.Quantity);

                var msrParm = MeasurementParameterFactory.GenerateRealtimeMesuarmentParameterWithoutSplit(prmAndQuantities);
                mesurnmentParameters.Add(msrParm);
            }

            var request = new PQSGetBaseDataRequest(session, new GetBaseDataInfoInput(Guid.Parse(keyAndVal.Key), start.TicksPQZTimeFormat, end.TicksPQZTimeFormat, mesurnmentParameters, CalculationTypeEnum.FORCE_DB_DATA));
            var tmp = PQZxmlWriter.WriteMessage(request, true);


            var pqsResponses = await _taskOrchestrator.DispatchBatch(async () => await SendRecordsContainerPostBinaryRequestAndException(url, request));
            //var pqsResponse = await SendRecordsContainerPostBinaryRequestAndException(url, request);
            var response = new PQSGetBaseDataResponse(request, pqsResponses.First());
            response.ExtractGetParametersOrError(out var axisses);

            //if (errors is not null)
            //{
            //    throw new UserFriendlyException((int)errors.Status, "to_translate", "From ErrorRecord");
            //}

            axisDaa.AddRange(axisses);
        }

        return new PQSOutput { Data = axisDaa.ToArray() };
    }


    private string ConstructParameter(string template, string param1, string param2)
    {
        var @params = template.Split('_');
        var modifiedParams = new string[@params.Length + 2];
        Array.Copy(@params, 0, modifiedParams, 0, 2);
        modifiedParams[2] = param1;
        Array.Copy(@params, 2, modifiedParams, 3, @params.Length - 2);
        modifiedParams[modifiedParams.Length - 1] = param2;
        var result = string.Join('_', modifiedParams);

        return result;
    }
}
