using System.Threading.Tasks;
using Abp.Domain.Policies;

namespace PQBI.Authorization.Users
{
    public interface IUserPolicy : IPolicy
    {
        Task CheckMaxUserCountAsync(int tenantId);
    }
}
