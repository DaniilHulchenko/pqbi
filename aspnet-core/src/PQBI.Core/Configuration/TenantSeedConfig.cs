using Abp.Authorization.Users;
using PQBI.Infrastructure;

namespace PQBI.Configuration
{
    public class PqbiConfig : PQSConfig<PqbiConfig>
    {
        public bool MultiTenancyEnabled { get; set; } = true;
        public TenantSeedConfig TenantSeedConfig { get; set; }
    }


    public class TenantSeedConfig : PQSConfig<TenantSeedConfig>
    {
        public string AdminEmailAddress { get; set; } = "admin@defaulttenant.com";
        public string AdminName { get; set; } = AbpUserBase.AdminUserName;
        public string AdminPassword { get; set; } = AbpUserBase.AdminUserName;
        public string AdminSurname { get; set; } = AbpUserBase.AdminUserName;
        public string ConnectionString { get; set; }
        public bool SendActivationEmail { get; set; } = true;
        public bool ShouldChangePasswordOnNextLogin { get; set; } = false;
        public PQSComunication PQSComunication { get; set; }

    }
}




