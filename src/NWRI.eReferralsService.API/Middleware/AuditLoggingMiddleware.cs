using System.Diagnostics;
using NWRI.eReferralsService.API.Constants;
using NWRI.eReferralsService.API.EventLogging;
using NWRI.eReferralsService.API.EventLogging.Interfaces;

namespace NWRI.eReferralsService.API.Middleware
{
    public sealed class AuditLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IEventLogger _eventLogger;

        public AuditLoggingMiddleware(RequestDelegate next, IEventLogger eventLogger)
        {
            _next = next;
            _eventLogger = eventLogger;
        }

        public async Task InvokeAsync(HttpContext context, IAuditContextAccessor auditContextAccessor)
        {
            auditContextAccessor.Current = CreateAuditContext(context);

            var stopwatch = Stopwatch.StartNew();

            _eventLogger.Audit(new EventCatalogue.RequestReceived(
                Method: context.Request.Method,
                Path: context.Request.Path.Value ?? string.Empty,
                RequestSize: context.Request.ContentLength));

            try
            {
                await _next(context);
            }
            finally
            {
                if (context.Response.StatusCode is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden)
                {
                    _eventLogger.Error(
                        new EventCatalogue.ErrAuthFailed(context.Request.Path.Value ?? string.Empty),
                        new UnauthorizedAccessException($"HTTP {context.Response.StatusCode}"));
                }

                _eventLogger.Audit(new EventCatalogue.ResponseSent(
                    StatusCode: context.Response.StatusCode,
                    LatencyMs: stopwatch.ElapsedMilliseconds));

                auditContextAccessor.Current = null;
            }
        }

        private static AuditContext CreateAuditContext(HttpContext context)
        {
            var correlationId = context.Request.Headers.TryGetValue(RequestHeaderKeys.CorrelationId, out var correlation)
                && !string.IsNullOrWhiteSpace(correlation)
                ? correlation.ToString()
                : context.TraceIdentifier;

            return new AuditContext(correlationId);
        }
    }
}
