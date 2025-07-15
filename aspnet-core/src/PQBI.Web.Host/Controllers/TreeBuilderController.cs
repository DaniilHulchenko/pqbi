using Microsoft.AspNetCore.Mvc;
using PQBI.Caching;
using Abp.Runtime.Session;
using PQBI.Network.RestApi;
using PQBI.PQS;
using GraphQL;

namespace PQBI.Web.Controllers;

public record PostGetLogicalTreeRequestTest(string UserNameOrEmailAddress, string Password, string ComponentId) : PQSGetSessionRequestTest(UserNameOrEmailAddress, Password);
public record CheckGetBaseDataTreeRequestTest(string UserNameOrEmailAddress, string Password, params string[] ComponentId) : PQSGetSessionRequestTest(UserNameOrEmailAddress, Password);

[Route("api/services/app/[controller]")]
public class TreeBuilderController : PQBIControllerBase
{
    public const string GetTreetUrl = $"TagsTree";
    public const string GetComponentUrl = $"ComponentTree";
    public const string GetComponentSlimUrl = $"ComponentSlimsInfo";

    public const string ComponensByTagstUrl = $"ComponentByTags";

    public const string PostTreetIntegrationTestUrl = $"GetTreeIntegrationTest";
    public const string PostTreeTabletIntegrationTestUrl = $"GetTreeTableIntegrationTest";

    public const string PostLogicalChannelTreeUrl = $"PostLogicalOrChannelTreeUrl";
    public const string CheckGetBaseDataTreeUrl = $"CheckGetBaseDataTreeUrl";
    public const string GetLogicalChannelTreeUrl = @"LogicalOrChannelTreeUrl/{componentId}";

    private readonly IPQSTreeBuilderService _pQSTreeBuilderService;
    private readonly ILogger<TreeBuilderController> _logger;
    private readonly IPQSRestApiService _PQSRestApiService;
    private readonly IPQSComponentOperationService _pQSComponentOperationService;
    private readonly ITenantCacheRepository _tenantCacheRepository;
    private readonly IUserSessionCacheRepository _userSessionCacheRepository;


    public TreeBuilderController(
        ILogger<TreeBuilderController> logger,
        IPQSTreeBuilderService pQSTreeBuilderService,
        ITenantCacheRepository tenantCacheRepository,
        IPQSRestApiService pQSRestApiService,
        IPQSComponentOperationService pQSComponentOperationService,
        IUserSessionCacheRepository userSessionCacheRepository)
    {
        _logger = logger;
        _pQSTreeBuilderService = pQSTreeBuilderService;
        _tenantCacheRepository = tenantCacheRepository;
        _PQSRestApiService = pQSRestApiService;
        this._pQSComponentOperationService = pQSComponentOperationService;
        _userSessionCacheRepository = userSessionCacheRepository;
    }

    [HttpPost]
    [Route(PostTreetIntegrationTestUrl)]

    public async Task<TagTreeRootDto> OmnibusTagTreeIntegrationTests([FromBody] PQSGetSessionRequestTest request)
    {
        var tenantId = AbpSession.GetTenantId();
        var userId = AbpSession.UserId;

        var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantId);

        var session = await _PQSRestApiService.OpenSessionForUserAsync(tenant.PQSServiceUrl, request.UserNameOrEmailAddress, request.Password);
        var response = await _pQSTreeBuilderService.GetTagOmnibusTreeAsync(tenant.PQSServiceUrl, session.SessionId);

        return response;
    }


    [HttpGet]
    [Route(GetTreetUrl)]
    public async Task<TagTreeRootDto> GetAllComponentsAsync()
    {
        var response = default(TagTreeRootDto);

        if (AbpSession.TenantId.HasValue && AbpSession.UserId.HasValue)
        {
            var tenantId = AbpSession.GetTenantId();
            var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantId);

            var session = await _userSessionCacheRepository.GetCacheSessionAsync(AbpSession.UserId.Value);

            using (var mainLogger = PqbiStopwatch.AnchorAsync($"{nameof(GetAllComponentsAsync)} - Beggin", _logger))
            {
                response = await _pQSTreeBuilderService.GetTagOmnibusTreeAsync(tenant.PQSServiceUrl, session);
            }
        }

        return response;
    }

    [HttpPut]
    [Route(ComponensByTagstUrl)]
    public async Task<GetComponentByTagsResponse> GetComponentByTagsAsync([FromBody] GetComponentByTagsRequest request)
    {
        var response = default(GetComponentByTagsResponse);

        if (AbpSession.TenantId.HasValue && AbpSession.UserId.HasValue)
        {
            var tenantId = AbpSession.GetTenantId();
            var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantId);

            var session = await _userSessionCacheRepository.GetCacheSessionAsync(AbpSession.UserId.Value);

            response = new GetComponentByTagsResponse { Components = _pQSTreeBuilderService.GetComponentByTags(request).ToArray() };
        }

        return response;
    }


    [HttpPost]
    [Route(PostLogicalChannelTreeUrl)]
    public async Task<DynamicTreeNode> PostGetLogicalTreeTestAsync([FromBody] PostGetLogicalTreeRequestTest request)
    {

        var tenantId = AbpSession.GetTenantId();
        var userId = AbpSession.UserId;

        var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantId);

        var session = await _PQSRestApiService.OpenSessionForUserAsync(tenant.PQSServiceUrl, request.UserNameOrEmailAddress, request.Password);
        var response = await _pQSTreeBuilderService.GetLogicalOrChannelTreeAsync(tenant.PQSServiceUrl, session.SessionId, request.ComponentId);

        return response;
    }


    [HttpPost]
    [Route(CheckGetBaseDataTreeUrl)]
    public async Task<StaticTreeNode> CheckGetBaseDataTree([FromBody] CheckGetBaseDataTreeRequestTest request)
    {

        var tenantId = AbpSession.GetTenantId();
        var userId = AbpSession.UserId;

        var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantId);

        var session = await _PQSRestApiService.OpenSessionForUserAsync(tenant.PQSServiceUrl, request.UserNameOrEmailAddress, request.Password);
        var response = await _pQSTreeBuilderService.CheckGetBaseDataTree(tenant.PQSServiceUrl, session.SessionId, request.ComponentId.FirstOrDefault());

        return response;
    }





    [HttpGet]
    [Route(GetLogicalChannelTreeUrl)]
    public async Task<DynamicTreeNode> GetLogicalOrChannelTreeTestAsync([FromRoute] string componentId)
    {
        var response = default(DynamicTreeNode);

        if (AbpSession.TenantId.HasValue && AbpSession.UserId.HasValue)
        {
            var tenantId = AbpSession.GetTenantId();
            var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantId);

            var session = await _userSessionCacheRepository.GetCacheSessionAsync(AbpSession.UserId.Value);
            response = await _pQSTreeBuilderService.GetLogicalOrChannelTreeAsync(tenant.PQSServiceUrl, session, componentId);
        }

        return response;
    }



    [HttpGet]
    [Route(GetComponentUrl)]

    public async Task<IEnumerable<ComponentDto>> GetAllComponentsWithTagsAsync()
    {
        var response = default(IEnumerable<ComponentDto>);

        if (AbpSession.TenantId.HasValue && AbpSession.UserId.HasValue)
        {
            var tenantId = AbpSession.GetTenantId();
            var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantId);

            var session = await _userSessionCacheRepository.GetCacheSessionAsync(AbpSession.UserId.Value);
            var tmp = await _pQSComponentOperationService.GetAllComponentsWithTagsAsync(tenant.PQSServiceUrl, session);
            response = tmp.Components;
        }

        return response;
    }

    [HttpPut]
    [Route(GetComponentSlimUrl)]

    public async Task<GetComponentSlimInfosResponse> GetComponentSlimAsync([FromBody] GetComponentSlimInfosRequest request)
    {
        var response = default(GetComponentSlimInfosResponse);

        if (AbpSession.TenantId.HasValue && AbpSession.UserId.HasValue)
        {
            var tenantId = AbpSession.GetTenantId();
            var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantId);

            var session = await _userSessionCacheRepository.GetCacheSessionAsync(AbpSession.UserId.Value);
            var components = await _pQSComponentOperationService.GetAllComponentSlimsAsync(tenant.PQSServiceUrl, session);
            response = new GetComponentSlimInfosResponse { Components = components };
        }

        return response;
    }
}