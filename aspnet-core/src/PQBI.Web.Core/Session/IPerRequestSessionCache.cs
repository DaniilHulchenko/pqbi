using System.Threading.Tasks;
using PQBI.Sessions.Dto;

namespace PQBI.Web.Session
{
    public interface IPerRequestSessionCache
    {
        Task<GetCurrentLoginInformationsOutput> GetCurrentLoginInformationsAsync();
    }
}
