using Abp.Authorization.Users;
using Abp.Dependency;
using PQBI.Authorization.Users;
using PQBI.Authorization.Users.Dto;
using PQBI.Caching;
using PQBI.Configuration;
using PQBI.Infrastructure.Extensions;
using PQBI.Logs;
using PQBI.MultiTenancy;
using PQBI.Network;
using PQBI.Network.Base;
using PQBI.Requests;
using PQS.Data.Common;
using PQS.Data.Permissions.Enums;

namespace PQBI.Web.Startup;


public class PQSServiceExternalAuthSource : DefaultExternalAuthenticationSource<Tenant, User>, ITransientDependency
{
    private readonly ILogger<PQSServiceExternalAuthSource> _logger;
    private readonly IPQSServiceProxy _pQSServiceProxy;
    private readonly IUserSessionCacheRepository _userSessionCacheRepository;
    private readonly UserManager _userManager;
    private readonly IUserAppService _userAppService;

    public PQSServiceExternalAuthSource(
        ILogger<PQSServiceExternalAuthSource> logger,
        IPQSServiceProxy pQSServiceProxy,
        IUserSessionCacheRepository userSessionCacheRepository,
        UserManager userManager,
        IUserAppService userAppService)
    {
        _logger = logger;
        _pQSServiceProxy = pQSServiceProxy;
        _userSessionCacheRepository = userSessionCacheRepository;
        _userManager = userManager;
        _userAppService = userAppService;
    }

    public override string Name
    {
        get { return nameof(PQSServiceExternalAuthSource); }
    }

    /// <summary>
    /// All users which dont have tenant will be evaluated in the PQBI DB.
    /// </summary>
    /// <param name="userNameOrEmailAddress"></param>
    /// <param name="plainPassword"></param>
    /// <param name="tenant"></param>
    /// <returns></returns>
    public override async Task<bool> TryAuthenticateAsync(string userNameOrEmailAddress, string plainPassword, Tenant tenant)
    {
        if (tenant == null)
        {
            return false;
        }

        long userId = 0;
        var result = false;

        var session = await _pQSServiceProxy.OpenAuthenticateAsync(tenant.Id, tenant.PQSServiceUrl, userNameOrEmailAddress, plainPassword);

        if (session.IsOk())
        {
            var user = await _userManager.FindByNameAsync(userNameOrEmailAddress);
            if (user is null)
            {
                user = await _userManager.AddToScadaUser(userNameOrEmailAddress, "Qqaasx100!", tenant.Id);
            }

            _logger.LogInformation($"PQSServiceExternalAuthSource |  TryAuthenticateAsync session={session}");
            if (session.IsOk())
            {
                await _userSessionCacheRepository.SetCacheSessionAsync(user.Id, tenant.Id, session.SessionId);
               
                //string userRole = await _pQSServiceProxy.GetUserRole(session.SessionId, tenant.PQSServiceUrl, userNameOrEmailAddress);
                //if (userRole != null)
                //    await _userManager.UpdateUserRolesAsync(user, userRole);

                userId = user.Id;
                result = true;
            }
        }

        if (result)
        {
            _logger.LogSession(tenant.Id, userId, "Successfully authenticated");
        }
        else
        {
            _logger.LogError($"Tenant authentication failed {tenant.Id} ");
        }

        return result;
    }
}
