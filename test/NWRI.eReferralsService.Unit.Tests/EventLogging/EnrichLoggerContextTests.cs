using FluentAssertions;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moq;
using NWRI.eReferralsService.API.Constants;
using NWRI.eReferralsService.API.EventLogging;

namespace NWRI.eReferralsService.Unit.Tests.EventLogging;

public class EnrichLoggerContextTests
{
    private readonly ITelemetry _telemetry;
    private readonly TelemetryContext _telemetryContext;

    public EnrichLoggerContextTests()
    {
        _telemetryContext = new TelemetryContext();

        var mock = new Mock<ITelemetry>();
        mock.SetupGet(x => x.Context).Returns(_telemetryContext);
        _telemetry = mock.Object;
    }

    [Fact]
    public void InitializeShouldSetCorrelationIdGlobalPropertyWhenHeaderIsPresent()
    {
        var context = new DefaultHttpContext();
        var expectedCorrelationId = "corr-123";
        context.Request.Headers[RequestHeaderKeys.CorrelationId] = expectedCorrelationId;

        var httpContextAccessor = new HttpContextAccessor { HttpContext = context };
        var sut = new EnrichLoggerContext(httpContextAccessor);

        sut.Initialize(_telemetry);

        _telemetryContext.GlobalProperties["CorrelationId"].Should().Be(expectedCorrelationId);
    }

    [Fact]
    public void InitializeShouldSetCorrelationIdGlobalPropertyToEmptyWhenHeaderIsMissing()
    {
        var context = new DefaultHttpContext();
        var httpContextAccessor = new HttpContextAccessor { HttpContext = context };
        var sut = new EnrichLoggerContext(httpContextAccessor);

        sut.Initialize(_telemetry);

        _telemetryContext.GlobalProperties.ContainsKey("CorrelationId").Should().BeFalse();
    }

    [Fact]
    public void InitializeShouldSetCorrelationIdGlobalPropertyToEmptyWhenHttpContextIsNull()
    {
        var httpContextAccessor = new HttpContextAccessor { HttpContext = null };
        var sut = new EnrichLoggerContext(httpContextAccessor);

        sut.Initialize(_telemetry);

        _telemetryContext.GlobalProperties.ContainsKey("CorrelationId").Should().BeFalse();
    }

    [Fact]
    public void InitializeShouldStoreCompleteHeaderValueWhenHeaderHasMultipleValues()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[RequestHeaderKeys.CorrelationId] = new StringValues(["corr-1", "corr-2"]);

        var httpContextAccessor = new HttpContextAccessor { HttpContext = context };
        var sut = new EnrichLoggerContext(httpContextAccessor);

        sut.Initialize(_telemetry);

        _telemetryContext.GlobalProperties["CorrelationId"].Should().Be("corr-1,corr-2");
    }
}
