using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using NWRI.eReferralsService.API.Constants;
using NWRI.eReferralsService.API.EventLogging;
using NWRI.eReferralsService.API.Middleware;
using NWRI.eReferralsService.Unit.Tests.EventLogging;

namespace NWRI.eReferralsService.Unit.Tests.Middleware;

public class AuditLoggingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsyncLogsReqReceivedAndRespSentWithOutcome()
    {
        // Arrange
        var spyLogger = new SpyEventLogger();

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/$process-message";
        context.Request.ContentLength = 123;
        context.Request.Headers[RequestHeaderKeys.CorrelationId] = "corr-1";
        context.Request.Headers[RequestHeaderKeys.RequestId] = "req-1";

        context.SetEndpoint(new Endpoint(
            requestDelegate: _ => Task.CompletedTask,
            metadata: new EndpointMetadataCollection(new ControllerActionDescriptor()),
            displayName: "Test controller endpoint"));

        RequestDelegate next = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        };

        var middleware = new AuditLoggingMiddleware(next, spyLogger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _ = spyLogger.AuditEvents.Should().HaveCount(2);
        _ = spyLogger.AuditEvents[0].Should().BeOfType<EventCatalogue.RequestReceived>();
        _ = spyLogger.AuditEvents[1].Should().BeOfType<EventCatalogue.ResponseSent>();

        var req = (EventCatalogue.RequestReceived)spyLogger.AuditEvents[0];
        _ = req.Method.Should().Be(HttpMethods.Post);
        _ = req.Path.Should().Be("/$process-message");
        _ = req.RequestSize.Should().Be(123);

        var resp = (EventCatalogue.ResponseSent)spyLogger.AuditEvents[1];
        _ = resp.StatusCode.Should().Be(StatusCodes.Status200OK);
        _ = resp.LatencyMs.Should().BeGreaterOrEqualTo(0);
    }
}
