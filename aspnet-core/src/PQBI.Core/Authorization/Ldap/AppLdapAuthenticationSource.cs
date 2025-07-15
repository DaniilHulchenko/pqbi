using Abp.Zero.Ldap.Authentication;
using Abp.Zero.Ldap.Configuration;
using PQBI.Authorization.Users;
using PQBI.MultiTenancy;

namespace PQBI.Authorization.Ldap
{
    public class AppLdapAuthenticationSource : LdapAuthenticationSource<Tenant, User>
    {
        public AppLdapAuthenticationSource(ILdapSettings settings, IAbpZeroLdapModuleConfig ldapModuleConfig)
            : base(settings, ldapModuleConfig)
        {
        }
    }
}