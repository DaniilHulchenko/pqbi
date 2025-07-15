using System.Threading.Tasks;
using PQBI.Views;
using Xamarin.Forms;

namespace PQBI.Services.Modal
{
    public interface IModalService
    {
        Task ShowModalAsync(Page page);

        Task ShowModalAsync<TView>(object navigationParameter) where TView : IXamarinView;

        Task<Page> CloseModalAsync();
    }
}
