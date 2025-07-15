using System;
using System.Collections.ObjectModel;
using System.Linq;
using Abp;
using Abp.Authorization.Roles;
using Abp.Authorization.Users;
using Abp.Notifications;
using Abp.Runtime.Session;
using AutoMapper.Internal.Mappers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PQBI.Authorization;
using PQBI.Authorization.Roles;
using PQBI.Authorization.Users;
using PQBI.Configuration;
using PQBI.EntityFrameworkCore;
using PQBI.Notifications;

namespace PQBI.Migrations.Seed.Tenants
{
    public class TenantRoleAndUserBuilder
    {
        private const string Demo_User_Name = "demo";
        private const string Demo_User_Password = "Demo";

        private readonly PQBIDbContext _context;
        private readonly TenantSeedConfig _tenantConfig;
        private readonly int _tenantId;

        public TenantRoleAndUserBuilder(PQBIDbContext context, int tenantId, TenantSeedConfig tenantConfig)
        {
            _context = context;
            _tenantId = tenantId;
            _tenantConfig = tenantConfig;
        }

        public void Create()
        {
            CreateRolesAndUsers();
            //CrateDemoUser();

        }

        //private void CrateDemoUser()
        //{
        //    var ptr = _context.Users.ToArray();
        //    var demoUser = _context.Users.IgnoreQueryFilters().FirstOrDefault(u => u.UserName == Demo_User_Name);
            
        //    if (demoUser is null)
        //    {

        //        var email = $"demo@gmail.com";
        //        var tenantId = 1;
        //        var user = new User
        //        {
        //            EmailAddress = email,
        //            IsActive = true,
        //            IsLockoutEnabled = true,
        //            IsTwoFactorEnabled = false,
        //            Name = Demo_User_Name,
        //            Password = Demo_User_Password,
        //            PhoneNumber = "123456",
        //            ShouldChangePasswordOnNextLogin = false,
        //            Surname = "DemoSureName",
        //            UserName = Demo_User_Name,
        //            TenantId = tenantId,
        //            Roles = new Collection<UserRole>(),
        //
        //
        //            zedUserName = Demo_User_Name.ToUpper(),
        //            NormalizedEmailAddress = email
        //        };

        //        var roles = _context.Roles.ToArray();
        //        var viewerRole = roles.FirstOrDefault(x=>x.Id == 1); // For Admin
        //        //var viewerRole = roles.FirstOrDefault(x => x.Id == (int)PQBIRoleEnum.Admin);
        //        var userRole = new UserRole(tenantId,user.Id, viewerRole.Id);
        //        user.Roles.Add(userRole);


        //        _context.Users.Add(user);

        //        _context.SaveChanges();
        //    }
        //}


        private void CreateRolesAndUsers()
        {
            var adminUser = _context.Users.IgnoreQueryFilters().FirstOrDefault(u => u.TenantId == _tenantId && u.UserName == AbpUserBase.AdminUserName);
            if (adminUser != null)
                return;

            //Admin role

            var adminRoleName = RoleTypes.Admin.ToString();
            var adminRole = _context.Roles.IgnoreQueryFilters().FirstOrDefault(r => r.TenantId == _tenantId && r.Name == adminRoleName);
            if (adminRole == null)
            {
                adminRole = _context.Roles.Add(new Role(_tenantId, StaticRoleNames.Tenants.Admin, StaticRoleNames.Tenants.Admin) { IsStatic = true }).Entity;
                _context.SaveChanges();
            }

            //DashboardBuilder role
            var dashboardBuilderRoleName = RoleTypes.DashboardBuilder.ToString();
            var dashboardBuilderRole = _context.Roles.IgnoreQueryFilters().FirstOrDefault(r => r.TenantId == _tenantId && r.Name == dashboardBuilderRoleName);
            if (dashboardBuilderRole == null)
            {
                dashboardBuilderRole = _context.Roles.Add(new Role(_tenantId, StaticRoleNames.Tenants.DashboardBuilder, StaticRoleNames.Tenants.DashboardBuilder) { IsStatic = true }).Entity;
                _context.SaveChanges();

                var permDashbEdit = _context.RolePermissions.Add(new RolePermissionSetting()
                {
                    TenantId = _tenantId,
                    Name = AppPermissions.Pages_Tenant_Dashboard_Edit,
                    IsGranted = true,
                    RoleId = dashboardBuilderRole.Id,
                    CreationTime = System.DateTime.UtcNow,
                    CreatorUserId = 1
                }).Entity;
                _context.SaveChanges();

                var permDashb = _context.RolePermissions.Add(new RolePermissionSetting()
                {
                    TenantId = _tenantId,
                    Name = AppPermissions.Pages_Tenant_Dashboard,
                    IsGranted = true,
                    RoleId = dashboardBuilderRole.Id,
                    CreationTime = System.DateTime.UtcNow,
                    CreatorUserId = 1
                }).Entity;
                _context.SaveChanges();

                var permPages = _context.RolePermissions.Add(new RolePermissionSetting()
                {
                    TenantId = _tenantId,
                    Name = AppPermissions.Pages,
                    IsGranted = true,
                    RoleId = dashboardBuilderRole.Id,
                    CreationTime = System.DateTime.UtcNow,
                    CreatorUserId = 1
                }).Entity;
                _context.SaveChanges();
            }

            //Viewer role
            var viewerRoleName = RoleTypes.Viewer.ToString();
            var viewerRole = _context.Roles.IgnoreQueryFilters().FirstOrDefault(r => r.TenantId == _tenantId && r.Name == viewerRoleName);
            if (viewerRole == null)
            {
                viewerRole = _context.Roles.Add(new Role(_tenantId, StaticRoleNames.Tenants.Viewer, StaticRoleNames.Tenants.Viewer) { IsStatic = true, IsDefault = true }).Entity;
                _context.SaveChanges();

                var permDashb = _context.RolePermissions.Add(new RolePermissionSetting()
                {
                    TenantId = _tenantId,
                    Name = AppPermissions.Pages_Tenant_Dashboard,
                    IsGranted = true,
                    RoleId = viewerRole.Id,
                    CreationTime = System.DateTime.UtcNow,
                    CreatorUserId = 1
                }).Entity;

                _context.SaveChanges();

                var permPages = _context.RolePermissions.Add(new RolePermissionSetting()
                {
                    TenantId = _tenantId,
                    Name = AppPermissions.Pages,
                    IsGranted = true,
                    RoleId = viewerRole.Id,
                    CreationTime = System.DateTime.UtcNow,
                    CreatorUserId = 1
                }).Entity;

                _context.SaveChanges();
            }

            //admin user

            //var adminUser = _context.Users.IgnoreQueryFilters().FirstOrDefault(u => u.TenantId == _tenantId && u.UserName == AbpUserBase.AdminUserName);
            if (adminUser == null)
            {
                adminUser = User.CreateTenantAdminUser(_tenantId, _tenantConfig.AdminEmailAddress, _tenantConfig.AdminName, _tenantConfig.AdminSurname);
                adminUser.UserName = _tenantConfig.AdminName;
                adminUser.Password = new PasswordHasher<User>(new OptionsWrapper<PasswordHasherOptions>(new PasswordHasherOptions())).HashPassword(adminUser, _tenantConfig.AdminPassword);
                adminUser.IsEmailConfirmed = true;
                adminUser.ShouldChangePasswordOnNextLogin = false;
                adminUser.IsActive = true;

                adminUser.SetNormalizedNames();
                _context.Users.Add(adminUser);
                _context.SaveChanges();

                //Assign Admin role to admin user
                _context.UserRoles.Add(new UserRole(_tenantId, adminUser.Id, adminRole.Id));
                _context.SaveChanges();

                //User account of admin user
                if (_tenantId == 1)
                {
                    _context.UserAccounts.Add(new UserAccount
                    {
                        TenantId = _tenantId,
                        UserId = adminUser.Id,
                        UserName = AbpUserBase.AdminUserName,
                        EmailAddress = adminUser.EmailAddress
                    });
                    _context.SaveChanges();
                }

                //Notification subscription
                _context.NotificationSubscriptions.Add(new NotificationSubscriptionInfo(SequentialGuidGenerator.Instance.Create(), _tenantId, adminUser.Id, AppNotificationNames.NewUserRegistered));
                _context.SaveChanges();
            }
        }
    }
}
