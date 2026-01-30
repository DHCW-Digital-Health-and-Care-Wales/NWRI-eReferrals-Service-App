using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using NWRI.eReferralsService.API.Constants;

namespace NWRI.eReferralsService.API.EventLogging;

public class TelemetryInitializer : ITelemetryInitializer
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TelemetryInitializer(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.GlobalProperties["CorrelationId"] = _httpContextAccessor.HttpContext?.Request.Headers[RequestHeaderKeys.CorrelationId];
    }
}
