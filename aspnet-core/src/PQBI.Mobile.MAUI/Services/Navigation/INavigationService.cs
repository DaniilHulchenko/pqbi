using Microsoft.AspNetCore.Components;

namespace PQBI.Services.Navigation
{
    public interface INavigationService
    {
        void Initialize(NavigationManager navigationManager);

        void NavigateTo(string uri, bool forceLoad = false, bool replace = false);
    }
}