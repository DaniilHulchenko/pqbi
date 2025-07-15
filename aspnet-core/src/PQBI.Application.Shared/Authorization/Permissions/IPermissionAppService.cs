using Abp.Application.Services;
using Abp.Application.Services.Dto;
using PQBI.Authorization.Permissions.Dto;

namespace PQBI.Authorization.Permissions
{
    public interface IPermissionAppService : IApplicationService
    {
        ListResultDto<FlatPermissionWithLevelDto> GetAllPermissions();
    }
}
