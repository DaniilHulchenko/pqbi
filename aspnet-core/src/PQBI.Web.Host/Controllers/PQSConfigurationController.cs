using Microsoft.AspNetCore.Mvc;
using PQBI.Configuration;

namespace PQBI.Web.Controllers;



[Route("[controller]")]
public class PQSConfigurationController : PQBIControllerBase
{
    private readonly IPQSConfigurationService _pQSConfigurationService;

    public PQSConfigurationController(IPQSConfigurationService pQSConfigurationService)
    {
        _pQSConfigurationService = pQSConfigurationService;
    }


    [HttpGet("configurations")]

    public async Task<ActionResult<string>> GetConfigurationsAsync()
    {
        var configurations = _pQSConfigurationService.GetAllConfiguration();

        return Ok(configurations);
    }

}
