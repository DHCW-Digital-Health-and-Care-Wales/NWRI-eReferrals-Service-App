using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NWRI.eReferralsService.API.HealthChecks;

public class ApplicationLivenessHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(HealthCheckResult.Healthy("Application is running."));
    }
}
