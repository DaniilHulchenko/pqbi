using Abp.Application.Services;
using PQBI.Dto;
using PQBI.Logging.Dto;

namespace PQBI.Logging
{
    public interface IWebLogAppService : IApplicationService
    {
        GetLatestWebLogsOutput GetLatestWebLogs();

        FileDto DownloadWebLogs();
    }
}
