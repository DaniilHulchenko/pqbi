using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PQBI.Trace;
using System.Threading.Tasks;

namespace PQBI.Web.Controllers
{
    [Route("[controller]")]
    public class LogWatcherController : PQBIControllerBase
    {
        private readonly ILogger<LogWatcherController> _logger;
        private readonly ILogWatcherService _logWatcher;

        public LogWatcherController(ILogger<LogWatcherController> logger, ILogWatcherService logWatcher)
        {
            _logger = logger;
            _logWatcher = logWatcher;
        }

        [HttpGet]
        public async Task<IActionResult> Watch (int amountLogs = 10)
        {
            var logs = _logWatcher.Peek(amountLogs);


            return Ok(logs);
        }

    }
}