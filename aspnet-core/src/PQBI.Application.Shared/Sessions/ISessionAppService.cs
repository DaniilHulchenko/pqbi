using System.Threading.Tasks;
using Abp.Application.Services;
using PQBI.Sessions.Dto;

namespace PQBI.Sessions
{
    public interface ISessionAppService : IApplicationService
    {
        Task<GetCurrentLoginInformationsOutput> GetCurrentLoginInformations();

        Task<UpdateUserSignInTokenOutput> UpdateUserSignInToken();
    }
}
