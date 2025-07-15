using System.Threading.Tasks;
using Abp.Domain.Uow;

namespace PQBI.OpenIddict
{
    public interface IOpenIddictDbConcurrencyExceptionHandler
    {
        Task HandleAsync(AbpDbConcurrencyException exception);
    }
}