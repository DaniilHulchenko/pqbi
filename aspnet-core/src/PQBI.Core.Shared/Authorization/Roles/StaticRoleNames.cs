using System.Collections.Generic;

namespace PQBI.Authorization.Roles
{
    public enum RoleTypes : ulong
    {
        None,
        Admin,
        DashboardBuilder,
        Viewer,
    }

    public static class RoleTypesUtilities
    {
        public static IEnumerable<RoleTypes> GetRolesExceptAdmin()
        {
            foreach (var roleTypeObject in System.Enum.GetValues(typeof(RoleTypes)))
            {
                var roleType = (RoleTypes)roleTypeObject;
                if (roleType == RoleTypes.None || roleType == RoleTypes.Admin)
                {
                    continue;
                }

                yield return roleType;
            }
        }

        public static IEnumerable<RoleTypes> GetAllRoles()
        {
            foreach (var roleTypeObject in System.Enum.GetValues(typeof(RoleTypes)))
            {
                var roleType = (RoleTypes)roleTypeObject;
                if (roleType == RoleTypes.None )
                {
                    continue;
                }

                yield return roleType;
            }
        }
    }

    public static class StaticRoleNames
    {
        public static class Host
        {
            public const string Admin = nameof(RoleTypes.Admin);
        }

        public static class Tenants
        {
            public const string Admin = nameof(RoleTypes.Admin);

            public const string DashboardBuilder = nameof(RoleTypes.DashboardBuilder);

            public const string Viewer = nameof(RoleTypes.Viewer);
        }
    }
}