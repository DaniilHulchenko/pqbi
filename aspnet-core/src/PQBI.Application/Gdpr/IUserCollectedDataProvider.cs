using System.Collections.Generic;
using System.Threading.Tasks;
using Abp;
using PQBI.Dto;

namespace PQBI.Gdpr
{
    public interface IUserCollectedDataProvider
    {
        Task<List<FileDto>> GetFiles(UserIdentifier user);
    }
}
