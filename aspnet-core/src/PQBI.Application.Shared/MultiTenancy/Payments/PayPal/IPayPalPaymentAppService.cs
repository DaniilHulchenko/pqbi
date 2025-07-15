using System.Threading.Tasks;
using Abp.Application.Services;
using PQBI.MultiTenancy.Payments.PayPal.Dto;

namespace PQBI.MultiTenancy.Payments.PayPal
{
    public interface IPayPalPaymentAppService : IApplicationService
    {
        Task ConfirmPayment(long paymentId, string paypalOrderId);

        PayPalConfigurationDto GetConfiguration();
    }
}
