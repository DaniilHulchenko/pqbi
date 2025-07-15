//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Logging;
//using PQBI.Network.Grpc;
//using System.Threading.Tasks;
//using PQBI.Caching;
//using Abp.Runtime.Session;
//using PQBI.Network.Grpc.Models;

//namespace PQBI.Web.Controllers;

//public class PQSGrpcController : PQBIControllerBase
//{
//    private readonly ILogger<PQSGrpcController> _logger;
//    private readonly IPQSGrpcService _PQSGrpcService;
//    private readonly IUserSessionCacheRepository _userSessionCacheRepository;
//    private readonly ITenantCacheRepository _tenantCacheRepository;


//    public PQSGrpcController(ILogger<PQSGrpcController> logger,
//         IPQSGrpcService pQSGrpcService,
//         IUserSessionCacheRepository userSessionCacheRepository,
//         ITenantCacheRepository tenantCacheRepository)
//    {
//        this._logger = logger;
//        this._PQSGrpcService = pQSGrpcService;
//        _userSessionCacheRepository = userSessionCacheRepository;
//        _tenantCacheRepository = tenantCacheRepository;
//    }

//    [HttpGet("identify")]
//    public async Task<IActionResult> GetIndentify()
//    {
//        try
//        {
//            var tenantId = AbpSession.GetTenantId();
//            var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantId);
//            var response = await _PQSGrpcService.IndentifyAsync(tenant.PQSServiceUrl);

//            return Ok("Ok");
//        }
//        catch (System.Exception ex)
//        {
//            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
//        }
//    }


//    [HttpPost("xml")]
//    public async Task<ActionResult<string>> RequestXML([FromBody] PQSServiceInputDto reqBody)
//    {
//        try
//        {
//            var tenantId = AbpSession.GetTenantId();
//            var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantId);

//            var response = await _PQSGrpcService.RequestXmlAsync(tenant.PQSServiceUrl, reqBody.RequestBody);

//            return Ok(response);
//        }
//        catch (System.Exception ex)
//        {
//            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
//        }
//    }




//    [HttpPut("closeSession")]
//    public async Task<ActionResult<string>> CloseSession()
//    {
//        try
//        {
//            var tenantId = AbpSession.GetTenantId();
//            var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantId);

//            var session = await _userSessionCacheRepository.GetCacheSessionAsync(AbpSession.GetUserId());
//            if (!string.IsNullOrEmpty(session))
//            {
//                var isClosed = await _PQSGrpcService.CloseSessionForUserAsync(tenant.PQSServiceUrl, session);
//            }

//            return Ok("Ok");
//        }
//        catch (System.Exception ex)
//        {
//            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
//        }
//    }
//}
