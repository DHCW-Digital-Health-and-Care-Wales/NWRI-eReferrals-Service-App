using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using NWRI.eReferralsService.API.Constants;

namespace NWRI.eReferralsService.API.EventLogging;

public class EnrichLoggerContext : ITelemetryInitializer
{
    private const string CorrelationIdPropertyName = "CorrelationId";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EnrichLoggerContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Initialize(ITelemetry telemetry)
    {
        if (_httpContextAccessor.HttpContext?.Request.Headers.TryGetValue(RequestHeaderKeys.CorrelationId, out var correlationId) == true)
        {
            telemetry.Context.GlobalProperties.TryAdd(CorrelationIdPropertyName, correlationId);
        }
    }
}
