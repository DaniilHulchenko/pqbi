using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Authorization.Roles;
using Abp.Authorization.Users;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.IdentityFramework;
using Abp.Localization;
using Abp.Organizations;
using Abp.Runtime.Caching;
using Abp.UI;
using Abp.Zero.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using PQBI.Authorization.Users;
using PQBI.Authorization;

namespace PQBI.Authorization.Roles
{
    /// <summary>
    /// Role manager.
    /// Used to implement domain logic for roles.
    /// </summary>
    public class RoleManager : AbpRoleManager<Role, User>
    {
        private readonly ILocalizationManager _localizationManager;
        private readonly IPermissionManager _permissionManager;

        public RoleManager(
            RoleStore store,
            IEnumerable<IRoleValidator<Role>> roleValidators,
            ILookupNormalizer keyNormalizer,
            IdentityErrorDescriber errors,
            ILogger<RoleManager> logger,
            IPermissionManager permissionManager,
            IRoleManagementConfig roleManagementConfig,
            ICacheManager cacheManager,
            IUnitOfWorkManager unitOfWorkManager,
            ILocalizationManager localizationManager,
            IRepository<OrganizationUnit, long> organizationUnitRepository,
            IRepository<OrganizationUnitRole, long> organizationUnitRoleRepository)
            : base(
                store,
                roleValidators,
                keyNormalizer,
                errors,
                logger,
                permissionManager,
                cacheManager,
                unitOfWorkManager,
                roleManagementConfig,
                organizationUnitRepository,
                organizationUnitRoleRepository)
        {
            _localizationManager = localizationManager;
            _permissionManager = permissionManager;
        }

        public override Task SetGrantedPermissionsAsync(Role role, IEnumerable<Permission> permissions)
        {
            CheckPermissionsToUpdate(role, permissions);

            return base.SetGrantedPermissionsAsync(role, permissions);
        }

        public virtual async Task<Role> GetRoleByIdAsync(long roleId)
        {
            var role = await FindByIdAsync(roleId.ToString());
            if (role == null)
            {
                throw new ApplicationException("There is no role with id: " + roleId);
            }

            return role;
        }

        public async Task CreatePredefinedRoles()
        {
            foreach (var roleTmp in RoleTypesUtilities.GetAllRoles())
            {
                var userRole = Roles.Single(r => r.Name == roleTmp.ToString());
                PopulatePredefiinedRoles(userRole);

                var identityResult = await UpdateAutoRoleAsync(userRole);
                identityResult.CheckErrors(_localizationManager);
            }
        }

        public async Task<IdentityResult> UpdateAutoRoleAsync(Role role)
        {
            var result = await UpdateAsync(role);

            if (role.Name.Equals(RoleTypes.DashboardBuilder.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                await SetGrantedPermissionsToDashboardBuilderAsync(role);
            }

            if (role.Name.Equals(RoleTypes.Viewer.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                await SetGrantedPermissionsToViewerrAsync(role);
            }

            return result;
        }

        private void PopulatePredefiinedRoles(Role role)
        {
            if (role.Name.Equals(RoleTypes.Viewer.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                role.IsDefault = true;
            }
        }

        private async Task SetGrantedPermissionsToDashboardBuilderAsync(Role userRole)
        {
            var pagesPermission = _permissionManager.GetPermission(AppPermissions.Pages);
            var dashboardPermission = _permissionManager.GetPermission(AppPermissions.Pages_Tenant_Dashboard);
            var dashboardEditPermission = _permissionManager.GetPermission(AppPermissions.Pages_Tenant_Dashboard_Edit);
            await SetGrantedPermissionsAsync(userRole, new[] { pagesPermission, dashboardPermission, dashboardEditPermission });
        }

        private async Task SetGrantedPermissionsToViewerrAsync(Role userRole)
        {
            var pagesPermission = _permissionManager.GetPermission(AppPermissions.Pages);
            var dashboardPermission = _permissionManager.GetPermission(AppPermissions.Pages_Tenant_Dashboard);
            await SetGrantedPermissionsAsync(userRole, new[] { pagesPermission, dashboardPermission });
        }

        private void CheckPermissionsToUpdate(Role role, IEnumerable<Permission> permissions)
        {
            if (role.Name == StaticRoleNames.Host.Admin &&
                (!permissions.Any(p => p.Name == AppPermissions.Pages_Administration_Roles_Edit) ||
                 !permissions.Any(p => p.Name == AppPermissions.Pages_Administration_Users_ChangePermissions)))
            {
                throw new UserFriendlyException(L("YouCannotRemoveUserRolePermissionsFromAdminRole"));
            }
        }

        private new string L(string name)
        {
            return _localizationManager.GetString(PQBIConsts.LocalizationSourceName, name);
        }
    }
}
