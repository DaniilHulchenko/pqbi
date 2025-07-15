using Abp.MultiTenancy;
using Abp.Zero.Configuration;

namespace PQBI.Authorization.Roles
{

    public static class AppRoleConfig
    {
        public static void Configure(IRoleManagementConfig roleManagementConfig)
        {
            //Static host roles

            roleManagementConfig.StaticRoles.Add(
                new StaticRoleDefinition(
                    StaticRoleNames.Host.Admin,
                    MultiTenancySides.Host,
                    grantAllPermissionsByDefault: true)
                );

            //Static tenant roles

            foreach (var roleTypeObject in System.Enum.GetValues(typeof(RoleTypes)))
            {
                var roleType = (RoleTypes)roleTypeObject;
                if (roleType == RoleTypes.None)
                {
                    continue;
                }

                if (roleType == RoleTypes.Admin)
                {
                    roleManagementConfig.StaticRoles.Add(new StaticRoleDefinition(StaticRoleNames.Tenants.Admin, MultiTenancySides.Tenant, grantAllPermissionsByDefault: true));
                    continue;
                }

                var roleName = roleType.ToString();
                roleManagementConfig.StaticRoles.Add(new StaticRoleDefinition(roleName, MultiTenancySides.Tenant, grantAllPermissionsByDefault: false));
            }
        }
    }
}
