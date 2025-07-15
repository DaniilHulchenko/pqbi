using Microsoft.AspNetCore.Mvc;
using PQBI.Caching;
using Abp.Runtime.Session;
using PQBI.Network.RestApi;
using PQBI.PQS;
using PQBI.Tenants.Dashboard.Dto;
using PQBI.MultiTenancy.Dto;
using PQBI.IntegrationTests.Scenarios.PopulatingParameters;
using PQBI.Sapphire.Options;
using k8s.Models;
using PQBI.Sapphire;
using PQS.Data.Events.Enums;
using Newtonsoft.Json;
using System.Data;
using PQBI.PQS.CalcEngine;
using PQBI.Infrastructure.Sapphire;
using PQBI.DashboardCustomization;
using Humanizer;
using System.Text.Json;
using PQBI.Network.RestApi.EngineCalculation;
using PQS.CommonUI.Enums;
using PQS.CommonUI.Data;
using PQS.Data.Measurements;
using PQS.Data.RecordsContainer.Records;
using PQS.Data.Configurations;
using PQS.Data.Configurations.Enums;
using PQS.Data.RecordsContainer;


namespace PQBI.Web.Controllers;

public record PQSGetSessionRequestTest(string UserNameOrEmailAddress, string Password);
public record PQSGetSessionResponseTest(ComponentDto[] Components);
public record GetAllComponentsResponse(ComponentDto[] Components);
public record GetAllEventsResponse(IEnumerable<PQSEventDto[]> Events);

public record PQSCalculationRequestTest222(string UserNameOrEmailAddress, string Password, PQBI.PQS.CalcEngine.TableWidgetRequest Request);
public record PQSCalculationChartBarRequestTest(string UserNameOrEmailAddress, string Password, PQBI.PQS.CalcEngine.BarChartRequest Request);
public record GetBaseParameterNameIntegrationTest(string UserNameOrEmailAddress, string Password, BaseParameterNameSlim Request);






public record PostEventstRequestTest(string UserNameOrEmailAddress, string Password, GetEventstRequest Request)
    : PQSGetSessionRequestTest(UserNameOrEmailAddress, Password);

public record GetPqsWidgetRequestTest(string UserNameOrEmailAddress, string Password, PQSInput GetPqsWidgetRequest)
    : PQSGetSessionRequestTest(UserNameOrEmailAddress, Password);

//[Route($"{PQSRestApiController.ControllerPrefixUrl}/[controller]")]
[Route("api/services/app/[controller]")]
public class PQSRestApiController : PQBIControllerBase
{
    public const string ControllerPrefixUrl = "api/services/app/";

    //public const string GetPQSComponentUrl = $"GetAllComponents";
    public const string PostEventsIntegrationTestsUrl = $"EventsIntegrationTests";
    public const string PostCustomerParametersIntegrationTestsUrl = $"CustomerParametersIntegrationTests";
    public const string PostChartBarIntegrationTestsUrl = $"ChartBarIntegrationTests";
    public const string GetTags = $"Tags";
    public const string GetPqsEvents = $"PQSEvents";
    public const string GetPQSBaseDataUrl = $"BaseDataIntegrationTests";
    public const string GetPQSBaseDataXXXXUrl = $"PQSData";
    public const string GetAllComponentConcurentlyUrl = $"GetAllComponentConcurent";
    public const string BaseParameterNameUrl = $"BaseParameterName";
    public const string BaseParameterNameIntegrationTestUrl = $"{BaseParameterNameUrl}IntegrationTests";
    public const string GetAllComponentsUrl = $"components";




    public const string GetStaticDataUrl_Old = $"GetStaticDataUrl_Old";
    //public const string GetStaticDataUrl = $"GetStaticData";
    public const string GetStaticDataUrl = $"GetStaticData";

    
    public const string GetMeasurementsGroupsUrl = $"MeasurementsGroups";
    public const string GetMeasurementsPhasesUrl = $"MeasurementsPhases";
    public const string GetMeasurementsBasesUrl = $"MeasurementsBases";
    public const string GetMeasurementsQunatitiesUrl = $"MeasurementsQunatities";

    public const string PostIntegrationTestsStaticDataUrl = $"PostStaticDataUrl";
    public const string PostIntegrationTestsStaticDataIntegrationTestUrl = $"PostStaticDataUrl222";

    //public const string GetComponentScenarioTestUrl = $"{GetPQSComponentUrl}";
    public const string GetBaeDataScenarioTestUrl = $"{GetPQSBaseDataUrl}IntegrationTests";
    public const string GetBaeDataUrl = GetPQSBaseDataXXXXUrl;

    private readonly ILogger<PQSRestApiController> _logger;
    private readonly IPQSRestApiService _PQSRestApiService;
    private readonly ITenantCacheRepository _tenantCacheRepository;
    private readonly IPQSComponentOperationService _pQSComponentOperation;
    private readonly IUserSessionCacheRepository _userSessionCacheRepository;
    private readonly IPQSTreeBuilderService _pQSTreeBuilderService;
    private readonly ICustomParameterCalculationService _customParameterCalculationService;

    public PQSRestApiController(ILogger<PQSRestApiController> logger,
         IPQSRestApiService pQSRestApiService,
         ITenantCacheRepository tenantCacheRepository,
         IPQSComponentOperationService pQSComponentOperation,
         IUserSessionCacheRepository userSessionCacheRepository,
         IPQSTreeBuilderService pQSTreeBuilderService,
          ICustomParameterCalculationService customParameterCalculationService
        )
    {
        this._logger = logger;
        this._PQSRestApiService = pQSRestApiService;
        _tenantCacheRepository = tenantCacheRepository;
        _pQSComponentOperation = pQSComponentOperation;
        _userSessionCacheRepository = userSessionCacheRepository;
        _pQSTreeBuilderService = pQSTreeBuilderService;
        _customParameterCalculationService = customParameterCalculationService;
    }


    [HttpGet]
    [Route(GetAllComponentsUrl)]

    public async Task<GetAllComponentsResponse> GetAllComponentsAsync()
    {
        var response = default(GetAllComponentsResponse);

        if (AbpSession.TenantId.HasValue && AbpSession.UserId.HasValue)
        {
            var tenantId = AbpSession.GetTenantId();
            var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantId);

            var session = await _userSessionCacheRepository.GetCacheSessionAsync(AbpSession.UserId.Value);
            var componentWithTagss = await _pQSComponentOperation.GetObjectAsync(tenant.PQSServiceUrl, session);

            response = new GetAllComponentsResponse(componentWithTagss.Components.ToArray());
        }

        return response;
    }

    [HttpPost]
    [Route(BaseParameterNameIntegrationTestUrl)]

    public async Task<string> BaseParameterName([FromBody] GetBaseParameterNameIntegrationTest baseParameter)
    {
        var name = await _pQSComponentOperation.GetBaseParameterName(baseParameter.Request);

        return name;
    }


    [HttpPost]
    [Route(BaseParameterNameUrl)]

    public async Task<string> BaseParameterName([FromBody] BaseParameterNameSlim baseParameter)
    {
        var name = await _pQSComponentOperation.GetBaseParameterName(baseParameter);

        return name;
    }


    [HttpPost]
    [Route(PostCustomerParametersIntegrationTestsUrl)]

    public async Task<TableWidgetResponse> PQSTableTreeDataIntegrationTests([FromBody] PQSCalculationRequestTest222 request)
    {
        TableWidgetResponse omnibus = null;
        try
        {

            var tenantId = AbpSession.GetTenantId();
            var userId = AbpSession.UserId;

            var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantId);

            var session = await _PQSRestApiService.OpenSessionForUserAsync(tenant.PQSServiceUrl, request.UserNameOrEmailAddress, request.Password);

            //omnibus = await _customParameterCalculationService.CalculateTableAsync(tenant.PQSServiceUrl, session, request.Request);


            //omnibus.Config = request.Request.Config;
        }
        catch (Exception ex)
        {
            //Should be removed lator on.

        }

        return omnibus;
    }



    [HttpPost]
    [Route(PostChartBarIntegrationTestsUrl)]

    public async Task<BarChartResponse> PQPostChartBarIntegrationTests([FromBody] PQSCalculationChartBarRequestTest request)
    {
        BarChartResponse response = null;
        try
        {

            var tenantId = AbpSession.GetTenantId();
            var userId = AbpSession.UserId;

            var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantId);

            var session = await _PQSRestApiService.OpenSessionForUserAsync(tenant.PQSServiceUrl, request.UserNameOrEmailAddress, request.Password);

            var items = await _customParameterCalculationService.CalculateBarChartAsync(tenant.PQSServiceUrl, session.SessionId, request.Request);
            response = new BarChartResponse
            {
                Components = items.ToList(),
                StartDate = request.Request.StartDate,
                EndDate = request.Request.EndDate,
                //Config = request.Request.Config
            };


            //omnibus.Config = request.Request.Config;
        }
        catch (Exception ex)
        {
            //Should be removed lator on.

        }

        return response;
    }



    [HttpPost]
    [Route(PostEventsIntegrationTestsUrl)]
    public async Task<GetAllEventsResponse> PostEvents([FromBody] PostEventstRequestTest request)
    {
        try
        {

            var tenantId = AbpSession.GetTenantId();
            var userId = AbpSession.UserId;

            var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantId);

            var session = await _PQSRestApiService.OpenSessionForUserAsync(tenant.PQSServiceUrl, request.UserNameOrEmailAddress, request.Password);
            var events = await _pQSComponentOperation.GetEventss(tenant.PQSServiceUrl, session.SessionId, request.Request);

            var response = new GetAllEventsResponse(events);
            return response;
        }
        catch (Exception ex)
        {
            //Should be removed lator on.
            throw;
        }
    }


    [HttpGet]
    [Route(GetPqsEvents)]
    public async Task<IEnumerable<EventClassDescription>> GetPqdEvents()
    {
        var tenantId = AbpSession.GetTenantId();
        var userId = AbpSession.UserId;
        var session = await _userSessionCacheRepository.GetCacheSessionAsync(AbpSession.UserId.Value);
        var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantId);

        return await _pQSComponentOperation.GetEventsTypeAsync(tenant.PQSServiceUrl, session);       
    }

    /// <summary>
    /// xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
    /// </summary>
    /// <returns></returns>
    /// 

    [HttpGet]
    [Route(GetStaticDataUrl)]
    public async Task<StaticDataInfo> StaticData()
    {
        var response = default(StaticDataInfo);

        if (AbpSession.TenantId.HasValue && AbpSession.UserId.HasValue)
        {
            var tenantId = AbpSession.GetTenantId();
            var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantId);

            var session = await _userSessionCacheRepository.GetCacheSessionAsync(AbpSession.UserId.Value);
            response = await _pQSComponentOperation.GetStaticTree(tenant.PQSServiceUrl, session);

        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        var txt = System.Text.Json.JsonSerializer.Serialize(response, options);
        return response;
    }




    //[HttpGet]
    //[Route(GetStaticDataUrl)]
    //public async Task<StaticTreeNode> StaticData()
    //{

    //    var response = CreationOptionsDataFactory.CreateOptionDtos();

    //    var options = new JsonSerializerOptions { WriteIndented = true };
    //    var txt = System.Text.Json.JsonSerializer.Serialize(response, options);
    //    return response;
    //}


    [HttpGet]
    [Route(GetMeasurementsGroupsUrl)]
    public async Task<IEnumerable<GroupDataInfo>> MeasurementsGroups()
    {
        var response = GroupDataInfo.GetAllGroupDataInfos();
        return response;
    }

    [HttpGet]
    [Route(GetMeasurementsPhasesUrl)]
    public async Task<IEnumerable<PhaseDataInfo>> MeasurementsPhases()
    {
        var response = PhaseDataInfo.GetAllPhaseDataInfos();
        return response;
    }

    [HttpGet]
    [Route(GetMeasurementsBasesUrl)]
    public async Task<IEnumerable<BaseDataInfo>> MeasurementsBases()
    {
        var response = BaseDataInfo.GetAllBaseDataInfos();
        return response;
    }

    [HttpGet]
    [Route(GetMeasurementsQunatitiesUrl)]
    public async Task<IEnumerable<QuantityDataInfo>> MeasurementsQuantiities()
    {
        var response = QuantityDataInfo.GetAllQuantityDataInfos();
        return response;
    }

    [HttpPost]
    [Route(PostIntegrationTestsStaticDataIntegrationTestUrl)]
    public async Task<StaticTreeNode> PostStaticDataItegrationTest([FromBody] PQSGetSessionRequestTest request)
    {
        try
        {

            var response = CreationOptionsDataFactory.CreateOptionDtos();
            //var responseTmp = CreationOptionsDataFactory.CreateOptionDtosOld();
            return response;
        }
        catch (Exception ex)
        {
            //Should be removed lator on.
            throw;
        }
    }





    //[HttpGet]
    //[Route(GetStaticDataUrl)]
    //public async Task<TreeParameterNode> StaticData2222()
    //{

    //    var response = CreationOptionsDataFactory.CreateOptionDtos2222();
    //    return response;
    //}

    //--------------------------------------------------------------------------------------------------------------------------

    [HttpGet("identify")]
    public async Task<IActionResult> GetIndentify()
    {
        try
        {
            var tenantId = AbpSession.GetTenantId();
            var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantId);
            var response = await _PQSRestApiService.IndentifyAsync(tenant.PQSServiceUrl);

            return Ok(response);
        }
        catch (System.Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }


    [HttpGet]
    [Route(GetTags)]

    public async Task<ComponentWithTagsResponse> GetTagsWithComponents()
    {
        var response = default(ComponentWithTagsResponse);

        if (AbpSession.TenantId.HasValue && AbpSession.UserId.HasValue)
        {
            var tenantId = AbpSession.GetTenantId();
            var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantId);

            var session = await _userSessionCacheRepository.GetCacheSessionAsync(AbpSession.UserId.Value);
            response = await _pQSComponentOperation.GetAllTags(tenant.PQSServiceUrl, session);

        }

        return response;
    }



    [HttpPost]
    [Route(GetTags)]

    public async Task<ComponentWithTagsResponse> GetTagsWithComponentsIntegrationTests([FromBody] PQSGetSessionRequestTest request)
    {
        var tenantId = AbpSession.GetTenantId();
        var userId = AbpSession.UserId;

        var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantId);

        var session = await _PQSRestApiService.OpenSessionForUserAsync(tenant.PQSServiceUrl, request.UserNameOrEmailAddress, request.Password);
        var response = await _pQSComponentOperation.GetAllTags(tenant.PQSServiceUrl, session.SessionId);

        return response;
    }

    //[HttpPost]
    //[Route(GetComponentScenarioTestUrl)]
    //public async Task<PQSGetSessionResponseTest> GetObjectIntegrationTestsAsync([FromBody] PQSGetSessionRequestTest request)
    //{
    //    var tenantId = AbpSession.GetTenantId();
    //    var userId = AbpSession.UserId;

    //    var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantId);

    //    var session = await _PQSRestApiService.OpenSessionForUserAsync(tenant.PQSServiceUrl, request.UserNameOrEmailAddress, request.Password);
    //    var components = await _pQSComponentOperation.GetObjectAsync(tenant.PQSServiceUrl, session);


    //    var response = new PQSGetSessionResponseTest(components.ToArray());


    //    return response;
    //}


    [Route(GetBaeDataUrl)]
    [HttpPost]
    public async Task<PQSOutput> GetPqsTrendWidgetIntegrationTestsAsync([FromBody] GetPqsWidgetRequestTest request)
    {
        var tenantId = AbpSession.GetTenantId();
        var userId = AbpSession.UserId;

        var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantId);

        var session = await _PQSRestApiService.OpenSessionForUserAsync(tenant.PQSServiceUrl, request.UserNameOrEmailAddress, request.Password);
        var result = await _pQSComponentOperation.GetBaseDataAsync_Original(tenant.PQSServiceUrl, session.SessionId, request.GetPqsWidgetRequest);

        return result;
    }

    [Route(GetAllComponentConcurentlyUrl)]
    [HttpPost]
    public async Task<PQSOutput> GetAllComponentConcurentlyUrlAsync([FromBody] GetPqsWidgetRequestTest request)
    {
        var tenantId = AbpSession.GetTenantId();
        var userId = AbpSession.UserId;

        var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantId);

        var session = await _PQSRestApiService.OpenSessionForUserAsync(tenant.PQSServiceUrl, request.UserNameOrEmailAddress, request.Password);
        var result = await _pQSComponentOperation.GetBaseDataConcurrentAsync(tenant.PQSServiceUrl, session.SessionId, request.GetPqsWidgetRequest);

        return result;
    }

}