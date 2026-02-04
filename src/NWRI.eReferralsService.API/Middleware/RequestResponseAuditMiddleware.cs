using System.Diagnostics;
using NWRI.eReferralsService.API.EventLogging;
using NWRI.eReferralsService.API.EventLogging.Interfaces;

namespace NWRI.eReferralsService.API.Middleware
{
    public sealed class RequestResponseAuditMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IEventLogger _eventLogger;

        public RequestResponseAuditMiddleware(RequestDelegate next, IEventLogger eventLogger)
        {
            _next = next;
            _eventLogger = eventLogger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            if (endpoint?.Metadata.GetMetadata<AuditLogRequestAttribute>() is null)
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();

            _eventLogger.Audit(new EventCatalogue.RequestReceived(
                context.Request.Method,
                context.Request.Path.Value ?? string.Empty,
                context.Request.ContentLength));

            try
            {
                await _next(context);
            }
            finally
            {
                _eventLogger.Audit(new EventCatalogue.ResponseSent(context.Response.StatusCode, stopwatch.ElapsedMilliseconds));

                if (context.Response.StatusCode is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden)
                {
                    _eventLogger.LogError(new EventCatalogue.AuthFailedError());
                }
            }
        }
    }
}
