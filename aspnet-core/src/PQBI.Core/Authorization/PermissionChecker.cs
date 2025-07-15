using Abp.Authorization;
using PQBI.Authorization.Roles;
using PQBI.Authorization.Users;

namespace PQBI.Authorization
{
    public class PermissionChecker : PermissionChecker<Role, User>
    {
        public PermissionChecker(UserManager userManager)
            : base(userManager)
        {

        }
    }
}
