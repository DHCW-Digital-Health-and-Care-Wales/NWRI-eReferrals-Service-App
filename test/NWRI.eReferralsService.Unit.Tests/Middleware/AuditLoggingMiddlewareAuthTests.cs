using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
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

        context.SetEndpoint(new Endpoint(
            requestDelegate: _ => Task.CompletedTask,
            metadata: new EndpointMetadataCollection(new ControllerActionDescriptor()),
            displayName: "Test controller endpoint"));

        RequestDelegate next = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };

        var middleware = new AuditLoggingMiddleware(next, spyLogger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        spyLogger.LogErrorEvents.Should().ContainSingle();
        spyLogger.LogErrorEvents[0].LogErrorEvent.Should().BeOfType<EventCatalogue.AuthFailedError>();
    }
}
