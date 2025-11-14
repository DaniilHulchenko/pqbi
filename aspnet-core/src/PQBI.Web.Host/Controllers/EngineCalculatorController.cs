using Microsoft.AspNetCore.Mvc;
using PQBI.Caching;
using Abp.Runtime.Session;
using PQBI.Network.RestApi;
using PQBI.PQS;
using PQBI.CalculationEngine;
using PQBI.PQS.CalcEngine;

namespace PQBI.Web.Controllers;


//public record PQSCalculationRequestTest(string UserNameOrEmailAddress, string Password, CalcRequestDto Request);
public record PQSCalculationRequestTest(string UserNameOrEmailAddress, string Password, TrendCalcRequest Request);



[Route("[controller]")]
public class EngineCalculatorController : PQBIControllerBase
{
    //public const string PostCalculationIntegrationTestUrl = $"PostCalculationIntegrationTestUrl";
    public const string PostTrendCalculationIntegrationTestUrl = $"PostTrendCalculationIntegrationTestUrl";
    public const string GetAggregationCalculationUrl = $"AggregationCalculation";

    private readonly IPQSTreeBuilderService _pQSTreeBuilderService;
    private readonly ILogger<EngineCalculatorController> _logger;
    private readonly IPQSRestApiService _PQSRestApiService;
    private readonly IPQSComponentOperationService _pQSComponentOperation;
    private readonly ITenantCacheRepository _tenantCacheRepository;
    private readonly IUserSessionCacheRepository _userSessionCacheRepository;
    private readonly ICustomParameterCalculationService _customParameterCalculationService;

    public EngineCalculatorController(IPQSTreeBuilderService pQSTreeBuilderService,
        ITenantCacheRepository tenantCacheRepository,
        IPQSRestApiService pQSRestApiService,
        IPQSComponentOperationService pQSComponentOperation,
        IUserSessionCacheRepository userSessionCacheRepository,
        ICustomParameterCalculationService customParameterCalculationService)
    {
        _pQSTreeBuilderService = pQSTreeBuilderService;
        _tenantCacheRepository = tenantCacheRepository;
        _PQSRestApiService = pQSRestApiService;
        _pQSComponentOperation = pQSComponentOperation;
        _userSessionCacheRepository = userSessionCacheRepository;
        _customParameterCalculationService = customParameterCalculationService;
    }

    //[HttpPost]
    //[Route(PostCalculationIntegrationTestUrl)]

    //public async Task<PQSCalculationResponse> OmnibusTagTreeIntegrationTests([FromBody] PQSCalculationRequestTest request)
    //{
    //    var tenantId = AbpSession.GetTenantId();
    //    var userId = AbpSession.UserId;

    //    var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantId);

    //    var session = await _PQSRestApiService.OpenSessionForUserAsync(tenant.PQSServiceUrl, request.UserNameOrEmailAddress, request.Password);
    //    //var response = await _pQSTreeBuilderService.GetTagOmnibusTreeAsync(tenant.PQSServiceUrl, session);

    //    var response = await _customParameterCalculationService.CalculateTrendChartAsync(tenant.PQSServiceUrl, session, request.Request);


    //    return new PQSCalculationResponse(response);
    //}



    [HttpPost]
    [Route(PostTrendCalculationIntegrationTestUrl)]

    public async Task<PQSCalculationResponse> OmnibusTagTreeIntegrationTests2222([FromBody] PQSCalculationRequestTest request)
    {
        var tenantId = AbpSession.GetTenantId();
        var userId = AbpSession.UserId;

        var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantId);

        var session = await _PQSRestApiService.OpenSessionForUserAsync(tenant.PQSServiceUrl, request.UserNameOrEmailAddress, request.Password);
        //var response = await _pQSTreeBuilderService.GetTagOmnibusTreeAsync(tenant.PQSServiceUrl, session);

        //var response = await _customParameterCalculationService.CalculateTrendChartAsync(tenant.PQSServiceUrl, session, request.Request);

        return null; ;
        //return new PQSCalculationResponse(response);
    }


    [HttpGet]
    [Route(GetAggregationCalculationUrl)]

    public async Task<string[]> GewGetAggregationCalculation()
    {
        var aggregationFunctions = IFunctionEngine.GetAllAggregationFunctions().Select(x => x.Alias);

        return aggregationFunctions.ToArray();
    }

    //[HttpPost]
    //[Route(PostCalculationUrl)]

    //public async Task<PQSCalculationResponse> CalculateParameters([FromBody] CalcRequestDto request)
    //{
    //    var response = default(CalculationDto);

    //    if (AbpSession.TenantId.HasValue && AbpSession.UserId.HasValue)
    //    {
    //        var tenantId = AbpSession.GetTenantId();
    //        var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantId);

    //        var session = await _userSessionCacheRepository.GetCacheSessionAsync(AbpSession.UserId.Value);
    //        response = await _pQSComponentOperation.CalculateCustomParameter(tenant.PQSServiceUrl, session, request);

    //    }

    //    return new PQSCalculationResponse(response);
    //}
}
