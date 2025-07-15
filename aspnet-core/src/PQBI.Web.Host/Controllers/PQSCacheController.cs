using Abp.Runtime.Session;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PQBI.Caching;
using PQBI.Logs;

namespace PQBI.Web.Controllers
{
    [Route("[controller]")]
    public class PQSCacheController : PQBIControllerBase
    {
        private readonly ILogger<PQSCacheController> _logger;
        private readonly IUserSessionCacheRepository _userSessionCacheRepository;

        public PQSCacheController(ILogger<PQSCacheController> logger, IUserSessionCacheRepository userSessionCacheRepository
            )
        {
            _logger = logger;
            _userSessionCacheRepository = userSessionCacheRepository;
        }


        [HttpGet("AllActiveUsers")]
        public async Task<IEnumerable<UserTenant>> ActiveUsers()
        {
            try
            {
                var activeUsers = _userSessionCacheRepository.PeekUserInfos();

                var serialized = JsonConvert.SerializeObject(activeUsers);
                _logger.LogInformation($"{nameof(PQSCacheController)} | {nameof(ActiveUsers)} |  activeusers= {serialized}");
                
                //_logger.LogSession(AbpSession.TenantId ?? 0, AbpSession.GetUserId(), message: JsonConvert.SerializeObject(activeUsers));

                return activeUsers;
            }
            catch (System.Exception ex)
            {
                throw;
                //return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

    }
}
