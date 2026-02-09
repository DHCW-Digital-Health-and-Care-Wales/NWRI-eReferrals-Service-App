using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using NWRI.eReferralsService.API.Constants;
using NWRI.eReferralsService.API.EventLogging;
using NWRI.eReferralsService.API.Middleware;
using NWRI.eReferralsService.Unit.Tests.EventLogging;

namespace NWRI.eReferralsService.Unit.Tests.Middleware;

public class RequestResponseAuditMiddlewareTests
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
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new ControllerActionDescriptor(), new AuditLogRequestAttribute()),
            "Test controller endpoint"));

        RequestDelegate next = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        };

        var middleware = new RequestResponseAuditMiddleware(next, spyLogger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        spyLogger.AuditEvents.Should().HaveCount(2);
        spyLogger.AuditEvents[0].Should().BeOfType<EventCatalogue.RequestReceived>();
        spyLogger.AuditEvents[1].Should().BeOfType<EventCatalogue.ResponseSent>();

        var req = (EventCatalogue.RequestReceived)spyLogger.AuditEvents[0];
        req.Method.Should().Be(HttpMethods.Post);
        req.Path.Should().Be("/$process-message");
        req.RequestSize.Should().Be(123);

        var resp = (EventCatalogue.ResponseSent)spyLogger.AuditEvents[1];
        resp.StatusCode.Should().Be(StatusCodes.Status200OK);
        resp.LatencyMs.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task InvokeAsyncDoesNotLogForSwaggerRequest()
    {
        // Arrange
        var spyLogger = new SpyEventLogger();

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/swagger/index.html";

        RequestDelegate next = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        };

        var middleware = new RequestResponseAuditMiddleware(next, spyLogger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        spyLogger.AuditEvents.Should().BeEmpty();
        spyLogger.LogErrorEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task InvokeAsyncLogsForServiceRequestEndpoint()
    {
        // Arrange
        var spyLogger = new SpyEventLogger();

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/ServiceRequest/123";
        context.Request.Headers[RequestHeaderKeys.CorrelationId] = "corr-1";
        context.Request.Headers[RequestHeaderKeys.RequestId] = "req-1";

        context.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new ControllerActionDescriptor(), new AuditLogRequestAttribute()),
            "Test controller endpoint"));

        RequestDelegate next = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        };

        var middleware = new RequestResponseAuditMiddleware(next, spyLogger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        spyLogger.AuditEvents.Should().HaveCount(2);
        spyLogger.AuditEvents[0].Should().BeOfType<EventCatalogue.RequestReceived>();
        spyLogger.AuditEvents[1].Should().BeOfType<EventCatalogue.ResponseSent>();
    }
}
