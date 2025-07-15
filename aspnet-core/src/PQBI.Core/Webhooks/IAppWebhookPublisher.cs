using System.Threading.Tasks;
using PQBI.Authorization.Users;

namespace PQBI.WebHooks
{
    public interface IAppWebhookPublisher
    {
        Task PublishTestWebhook();
    }
}
