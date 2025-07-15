using Abp.UI;
using Microsoft.Extensions.Logging;
using PQBI.Network.Base;
using PQBI.PQS;
using PQBI.Requests;
using PQS.Data.Measurements;
using System.Text.RegularExpressions;
using PQBI.Sapphire.Options;
using PQS.Data.Measurements.StandardParameter;
using PQS.Translator;
using Twilio.TwiML.Voice;
using PQBI.Sapphire;
using PQZTimeFormat;
using PQBI.Infrastructure.Sapphire;
using PQS.Data.Configurations.Enums;
using PQS.PQZxml;
using PQS.Data.RecordsContainer;
using System.Linq.Expressions;
using PayPalCheckoutSdk.Orders;
using Abp.Runtime.Caching;
using PQBI.Infrastructure.Extensions;
using PQBI.PQS.Cache.Tags;
//using PQS.Data.Measurements.StandardParameter;

namespace PQBI.Network.RestApi;

public interface IPQSTreeBuilderService
{
    static string Alias = IPQSServiceBase.Alias;

    Task<TagTreeRootDto> GetTagOmnibusTreeAsync(string url, string session);
    Task<DynamicTreeNode> GetLogicalOrChannelTreeAsync(string url, string session, string componentId);
    //Task<TagTreeRootDto> GetTreeTableAsync(string url, string session, GetEventstRequest input);
    Task<StaticTreeNode> CheckGetBaseDataTree(string url, string session, string componentId);
    IEnumerable<TagWithComponents> GetComponentByTags(GetComponentByTagsRequest request);
}


public class ParameterPartDto2
{
    public string ParameterName { get; set; }


    public string Group { get; set; }
    public string GroupDescription { get; set; }


    public string CalculationBase { get; set; }
    public string CalculationBaseDescription { get; set; }


    public string QuantityEnum { get; set; }



}


public class PQSTreeBuilderService : PQSRestApiServiceBase, IPQSTreeBuilderService
{
    private const string Feeder_Name = "FEEDER";
    private const string Channel_Name = "CH";
    private const string STD_Name = "STD";
    private const string MULTI_STD_NAME = "MULTI_STD";
    private const string MULTISTD_NAME = "MULTISTD";

    private readonly ITaskOrchestrator _taskOrchestrator;
    private readonly IPQSComponentOperationService _pQSComponentOperationService;
    private readonly IFeederChannelTredBuilder _feederChanelTreBuilder;
    private readonly ICacheManager _cacheManager;

    public PQSTreeBuilderService(ILogger<PQSTreeBuilderService> logger,
        IHttpClientFactory httpClientFactory,
        IPQZBinaryWriterWrapper pQZBinaryWriterCore,
        IPQSenderHelper pQSenderHelper,
        ITaskOrchestrator taskOrchestrator,
        IPQSComponentOperationService pQSComponentOperationService,
        IFeederChannelTredBuilder feederChanelTreBuilder,
            ICacheManager cacheManager
        ) : base(httpClientFactory, pQZBinaryWriterCore, pQSenderHelper, logger)
    {
        _taskOrchestrator = taskOrchestrator;
        _pQSComponentOperationService = pQSComponentOperationService;
        _feederChanelTreBuilder = feederChanelTreBuilder;
        _cacheManager = cacheManager;
    }

    protected override string ClientAlias => IPQSComponentOperationService.Alias;


    public IEnumerable<TagWithComponents> GetComponentByTags(GetComponentByTagsRequest request)
    {
        if (request is null || request.Tags is null || request.Tags.Count() == 0)
        {
            return Enumerable.Empty<TagWithComponents>();
        }

        var cacheItem = _cacheManager.GetOrDefault();
        var components = new List<TagWithComponents>();
        if (cacheItem is not null && cacheItem.Components is not null)
        {
            foreach (var tagName in request.Tags)
            {
                foreach (var component in cacheItem.Components)
                {
                    if (tagName.IsCollectionEmpty() == false && tagName.Equals(component.TagName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        components.Add(component);
                    }
                }
            }
        }

        return components;
    }

    public async Task<TagTreeRootDto> GetTagOmnibusTreeAsync(string url, string session)
    {
        IEnumerable<PQS.ComponentDto> components = null;
        GetTagsConfigurationResponse tags = null;
        var no_name = string.Empty;

        var mainLogger = PqbiStopwatch.AnchorAsync(nameof(GetTagOmnibusTreeAsync), Logger);
        {
            var componentWithTags = await _pQSComponentOperationService.GetObjectAsync(url, session);
            components = componentWithTags.Components;
            tags = componentWithTags.GetTagsConfigurationResponse;

            mainLogger.LogInformation("Additional Information");
            var compStr = string.Join(',', components.Select(x => x.ComponentId));
            mainLogger.LogInformation(compStr);

            compStr = string.Join(',', components.Select(x => x.ComponentName));
            mainLogger.LogInformation(compStr);
        }


        tags.TryGetMap(out IDictionary<string, ComponentWithTagsDto> map, out IEnumerable<TagWithComponents> tagWithComponents);
        var cacheItem = new TagWithComponentCacheItem
        {
            Components = tagWithComponents,
        };

        await _cacheManager.GetTagWithComponentCache().SetAsync(TagWithComponentCacheItem.CacheName, cacheItem);

        //var ptr = _cacheManager.GetOrDefault();

        var tagMap = new Dictionary<string, Dictionary<string, List<ComponentDto>>>();

        foreach (var component in components)
        {
            var found = tagWithComponents.FirstOrDefault(tag => tag.ComponentIds.Contains(component.ComponentId) == true);
            if (found is null)
            {
                if (tagMap.TryGetValue(no_name, out var dickCom))
                {
                    if (dickCom.TryGetValue(component.ComponentId, out var comp) == true)
                    {
                        comp.Add(component);
                    }
                    else
                    {
                        dickCom[component.ComponentId] = [component];
                    }
                }
                else
                {
                    var dickComp = new Dictionary<string, List<ComponentDto>>();
                    tagMap[no_name] = dickComp;
                    dickComp[component.ComponentId] = [component];
                }
            }
            else
            {
                foreach (var tag in tagWithComponents)
                {
                    var key = tag.TagName;
                    if (tag.ComponentIds.Contains(component.ComponentId))
                    {
                        var labelsMap = default(Dictionary<string, List<ComponentDto>>);
                        if (tagMap.TryGetValue(key, out labelsMap) == false)
                        {
                            tagMap[key] = labelsMap = new Dictionary<string, List<ComponentDto>>();
                        }

                        var comps = default(List<ComponentDto>);
                        if (labelsMap.TryGetValue(tag.TagValue, out comps) == false)
                        {
                            labelsMap[tag.TagValue] = comps = new List<ComponentDto>();
                        }
                        comps.Add(component);
                    }
                }
            }

        }


        var tagSlims = new List<TagDtoV2>();
        var omnibus = new TagTreeRootDto(tagSlims);

        foreach (var tag in tagMap)
        {
            var labels = new List<LabelDtoV2>();
            var tagRoot = new TagDtoV2(tag.Key, labels);

            foreach (var keyAndValue in tag.Value)
            {
                var comps = new List<ComponentDto>();
                var labelDto = new LabelDtoV2(keyAndValue.Key, comps);

                foreach (var componentDto in keyAndValue.Value)
                {
                    comps.Add(componentDto);
                }
                tagRoot.Labels.Add(labelDto);
            }

            tagSlims.Add(tagRoot);
        }

        return omnibus;
    }

    public async Task<DynamicTreeNode> GetLogicalOrChannelTreeAsync(string url, string session, string componentId)
    {

        var request = new PQSGetAllObjectsRequest(session, [componentId]);
        var pqsResponse = await SendRecordsContainerPostBinaryRequestAndException(url, request);

        var response = new PQSGetAllObjectsResponse(request, pqsResponse);
        response.ExtractGetParametersOrError(out var @params, out var error);

        DynamicTreeNode tree = null;
        var paramDictionary = @params;

        if (paramDictionary.Count > 0)
        {
            if (paramDictionary.TryGetValue(componentId, out var parameters))
            {
                tree = await _feederChanelTreBuilder.GetLogicalOrChannelTreeAsync(parameters);
            }
        }

        return tree;
    }


    public async Task<StaticTreeNode> CheckGetBaseDataTree(string url, string session, string componentId)
    {

        var request = new PQSGetAllObjectsRequest(session, [componentId]);
        var pqsResponse = await SendRecordsContainerPostBinaryRequestAndException(url, request);

        var response = new PQSGetAllObjectsResponse(request, pqsResponse);
        response.ExtractGetParametersOrError(out var @params, out var error);

        var start = new PQZDateTime(DateTime.Now.AddDays(-100));
        var end = new PQZDateTime(DateTime.Now.AddDays(-1));

        var ptr = @params.First().Value;
        foreach (var parameterNameValue in ptr)
        {
            //if (parameterNameValue.Contains("RMS"))
            {

                var parameterName = parameterNameValue.Split('#').FirstOrDefault();
                var msrParm = MeasurementParameterFactory.GenerateNewMesurmentParameterWithoutSplit(parameterName);


                var tmpppp = MeasurementParameterFactory.GenerateNewMesurmentParameterWithoutSplit("STD_HRMSINCYC_3_IS1MIN_BHCYC_QMIN_UV1N_FEEDER_1");
                if (msrParm.IsHarmonicParameter)
                {
                    var getDataRequest = new PQSGetBaseDataRequest(session, new GetBaseDataInfoInput(Guid.Parse(componentId), start.TicksPQZTimeFormat, end.TicksPQZTimeFormat, [msrParm], CalculationTypeEnum.FORCE_DB_DATA));
                    var tmp = PQZxmlWriter.WriteMessage(request, true);
                    var pqsResponses = await SendRecordsContainerPostBinaryRequestAndException(url, getDataRequest);


                    //var rtr = new PQSGetBaseDataResponse(getDataRequest, pqsResponses);
                    //try
                    //{
                    //    rtr.ExtractGetParametersOrError(out var axisses, out var errors);

                    //    if (axisses.Any())
                    //    {


                    //    }
                    //}
                    //catch (Exception ex) { }

                }


                var paramInfo = new ParameterPartDto2
                {
                    ParameterName = parameterName,

                    Group = msrParm.GetGroupName(),
                    GroupDescription = ((StandardMeasurementParameter)msrParm).Group.Description(),

                    CalculationBase = msrParm.CalculationBaseClass.CalculationBaseEnum.ToString(),
                    CalculationBaseDescription = msrParm.CalculationBaseClass.CalculationBaseEnum.Description(),


                    QuantityEnum = msrParm.Quantity.Description(),
                };

            }
        }

        return null;
    }


    //public async Task<TagTreeRootDto> GetTreeTableAsync(string url, string session, GetEventstRequest input)
    //{

    //    var start = new PQZDateTime(input.Start);
    //    var end = new PQZDateTime(input.End);

    //    var evennts = EventFactory.GetAllEvents();

    //    var request = new PQSGetEventRequest(session, start, end, evennts, input.ComponentIds);
    //    var pqsResponse = await SendRecordsContainerPostBinaryRequestAndException(url, request);

    //    var respose = new PQSAddEventResponse(request, pqsResponse);
    //    var events = respose.Events;


    //    return null;
    //}

}
