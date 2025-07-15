using System.Collections.Generic;
using MvvmHelpers;
using PQBI.Models.NavigationMenu;

namespace PQBI.Services.Navigation
{
    public interface IMenuProvider
    {
        ObservableRangeCollection<NavigationMenuItem> GetAuthorizedMenuItems(Dictionary<string, string> grantedPermissions);
    }
}