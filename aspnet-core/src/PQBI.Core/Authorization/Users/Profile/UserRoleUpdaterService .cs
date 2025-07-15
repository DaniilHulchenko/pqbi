using System.Threading.Tasks;
using Abp.Dependency;
using PQBI.Authorization.Users;

public class UserRoleUpdaterService : ITransientDependency
{
    private readonly UserManager _userManager;

    public UserRoleUpdaterService(UserManager userManager)
    {
        _userManager = userManager;
    }

    /// <summary>
    /// Updates the user's roles to match the external role.
    /// </summary>
    /// <param name="user">The user to update.</param>
    /// <param name="externalRole">The role fetched from the external service (e.g., "Viewer", "Editor", "Administrator").</param>
    public async Task UpdateUserRolesAsync(User user, string externalRole)
    {
        // Get current roles from the user
        var currentRoles = await _userManager.GetRolesAsync(user);

        // If the user already has the correct role, nothing to do.
        if (currentRoles.Contains(externalRole))
        {
            return;
        }

        // Option 1: Remove all roles and assign the external role.
        // (Adjust this logic if you want to merge roles or handle multiple roles.)
        foreach (var role in currentRoles)
        {
            await _userManager.RemoveFromRoleAsync(user, role);
        }

        await _userManager.AddToRoleAsync(user, externalRole);
    }
}
