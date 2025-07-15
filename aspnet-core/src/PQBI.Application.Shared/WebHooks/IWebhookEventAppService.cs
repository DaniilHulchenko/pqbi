using System.Threading.Tasks;
using Abp.Webhooks;

namespace PQBI.WebHooks
{
    public interface IWebhookEventAppService
    {
        Task<WebhookEvent> Get(string id);
    }
}
