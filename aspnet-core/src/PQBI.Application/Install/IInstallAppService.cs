using System.Threading.Tasks;
using Abp.Application.Services;
using PQBI.Install.Dto;

namespace PQBI.Install
{
    public interface IInstallAppService : IApplicationService
    {
        Task Setup(InstallDto input);

        AppSettingsJsonDto GetAppSettingsJson();

        CheckDatabaseOutput CheckDatabase();
    }
}