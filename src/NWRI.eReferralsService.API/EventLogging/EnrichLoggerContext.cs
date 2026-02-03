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
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return;
        }

        var correlationId = httpContext.Request.Headers[RequestHeaderKeys.CorrelationId].ToString();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            return;
        }

        telemetry.Context.GlobalProperties.TryAdd(CorrelationIdPropertyName, correlationId);
    }
}
