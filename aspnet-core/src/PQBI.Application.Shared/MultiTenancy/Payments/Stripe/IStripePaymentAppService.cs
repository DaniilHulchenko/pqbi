using System.Threading.Tasks;
using Abp.Application.Services;
using PQBI.MultiTenancy.Payments.Dto;
using PQBI.MultiTenancy.Payments.Stripe.Dto;

namespace PQBI.MultiTenancy.Payments.Stripe
{
    public interface IStripePaymentAppService : IApplicationService
    {
        Task ConfirmPayment(StripeConfirmPaymentInput input);

        StripeConfigurationDto GetConfiguration();
        
        Task<string> CreatePaymentSession(StripeCreatePaymentSessionInput input);
    }
}