using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NWRI.eReferralsService.API.Constants;
using NWRI.eReferralsService.API.EventLogging;
using NWRI.eReferralsService.API.Middleware;
using NWRI.eReferralsService.Unit.Tests.EventLogging;

namespace NWRI.eReferralsService.Unit.Tests.Middleware;

public class AuditLoggingMiddlewareAuthTests
{
    [Fact]
    public async Task InvokeAsyncEmitsErrAuthFailedWhenResponseIs401()
    {
        // Arrange
        var spyLogger = new SpyEventLogger();

        var context = new DefaultHttpContext();
        context.Request.Path = "/$process-message";
        context.Request.Headers[RequestHeaderKeys.CorrelationId] = "corr-1";

        RequestDelegate next = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };

        var middleware = new AuditLoggingMiddleware(next, spyLogger);
        var auditContextAccessor = new AuditContextAccessor();

        // Act
        await middleware.InvokeAsync(context, auditContextAccessor);

        // Assert
        _ = spyLogger.ErrorEvents.Should().ContainSingle();
        _ = spyLogger.ErrorEvents[0].ErrorEvent.Should().BeOfType<EventCatalogue.ErrAuthFailed>();

        var err = (EventCatalogue.ErrAuthFailed)spyLogger.ErrorEvents[0].ErrorEvent;
        _ = err.Path.Should().Be("/$process-message");
    }
}
