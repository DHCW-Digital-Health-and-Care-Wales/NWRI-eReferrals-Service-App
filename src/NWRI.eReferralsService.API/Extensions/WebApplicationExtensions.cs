using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace NWRI.eReferralsService.API.Extensions;

[ExcludeFromCodeCoverage]
public static class WebApplicationExtensions
{
    public static void MapCustomHealthChecks(this WebApplication app)
    {
        // Readiness probe
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        // Liveness probe
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live")
        });

        // General health check endpoint
        app.MapHealthChecks("/health", new HealthCheckOptions());
    }
}
