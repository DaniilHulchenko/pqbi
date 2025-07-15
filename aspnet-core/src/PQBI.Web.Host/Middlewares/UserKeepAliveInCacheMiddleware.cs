using Abp.Dependency;
using Abp.Runtime.Session;
using PQBI.Caching;

namespace PQBI.Web.Middlewares
{
    public class UserKeepAliveInCacheMiddleware : IMiddleware, ITransientDependency
    {
        private readonly IAbpSession _abpSession;
        private readonly IUserSessionCacheRepository _userSessionCacheRepository;

        public UserKeepAliveInCacheMiddleware(IAbpSession abpSession, IUserSessionCacheRepository userSessionCacheRepository)
        {
            _abpSession = abpSession;
            _userSessionCacheRepository = userSessionCacheRepository;
        }
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (_abpSession.TenantId != null && _abpSession.UserId != null)
            {
                await _userSessionCacheRepository.KeepAliveInCacheAsync(_abpSession.UserId.Value);
            }

            await next(context);
        }
    }
}
