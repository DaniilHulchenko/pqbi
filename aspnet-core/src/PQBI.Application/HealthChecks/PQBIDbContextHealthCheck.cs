using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PQBI.EntityFrameworkCore;

namespace PQBI.HealthChecks
{
    public class PQBIDbContextHealthCheck : IHealthCheck
    {
        private readonly DatabaseCheckHelper _checkHelper;

        public PQBIDbContextHealthCheck(DatabaseCheckHelper checkHelper)
        {
            _checkHelper = checkHelper;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
        {
            if (_checkHelper.Exist("db"))
            {
                return Task.FromResult(HealthCheckResult.Healthy("PQBIDbContext connected to database."));
            }

            return Task.FromResult(HealthCheckResult.Unhealthy("PQBIDbContext could not connect to database"));
        }
    }
}
