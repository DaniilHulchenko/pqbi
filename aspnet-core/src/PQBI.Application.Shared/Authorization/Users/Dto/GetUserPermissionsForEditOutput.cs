using System.Collections.Generic;
using PQBI.Authorization.Permissions.Dto;

namespace PQBI.Authorization.Users.Dto
{
    public class GetUserPermissionsForEditOutput
    {
        public List<FlatPermissionDto> Permissions { get; set; }

        public List<string> GrantedPermissionNames { get; set; }
    }
}