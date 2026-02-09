using System.Text;
using AutoFixture;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NWRI.eReferralsService.API.Constants;
using NWRI.eReferralsService.API.Controllers;
using NWRI.eReferralsService.API.Exceptions;
using NWRI.eReferralsService.API.Services;
using NWRI.eReferralsService.Unit.Tests.Extensions;

namespace NWRI.eReferralsService.Unit.Tests.Controllers;

public class ReferralsControllerTests
{
    private readonly IFixture _fixture = new Fixture().WithCustomizations();

    private readonly ReferralsController _sut;

    public ReferralsControllerTests()
    {
        _fixture.OmitAutoProperties = true;
        _sut = _fixture.CreateWithFrozen<ReferralsController>();

        _fixture.Register<IHeaderDictionary>(() => new HeaderDictionary { { _fixture.Create<string>(), _fixture.Create<string>() } });
    }

    [Fact]
    public async Task ProcessMessageShouldCallProcessMessageAsync()
    {
        //Arrange
        var body = _fixture.Create<string>();
        var headers = _fixture.Create<IHeaderDictionary>();

        SetRequestDetails(headers, body);

        var headerArgs = new List<IHeaderDictionary>();
        _fixture.Mock<IReferralService>().Setup(x => x.ProcessMessageAsync(Capture.In(headerArgs), It.IsAny<string>(), It.IsAny<CancellationToken>()));
        //Act
        await _sut.ProcessMessage(CancellationToken.None);

        //Assert
        headerArgs[0].Should().ContainKeys(headers.Keys);
        _fixture.Mock<IReferralService>().Verify(x => x.ProcessMessageAsync(It.IsAny<IHeaderDictionary>(), body, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task ProcessMessageShouldReturn200()
    {
        //Arrange
        var body = _fixture.Create<string>();
        var headers = _fixture.Create<IHeaderDictionary>();
        SetRequestDetails(headers, body);

        var outputBundleJson = _fixture.Create<string>();

        _fixture.Mock<IReferralService>().Setup(x => x.ProcessMessageAsync(It.IsAny<IHeaderDictionary>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(outputBundleJson);

        //Act
        var result = await _sut.ProcessMessage(CancellationToken.None);

        //Assert
        var contentResult = result.Should().BeOfType<ContentResult>().Subject;
        contentResult.StatusCode.Should().Be(200);
        contentResult.Content.Should().Be(outputBundleJson);
        contentResult.ContentType.Should().Be(FhirConstants.FhirMediaType);
    }

    [Fact]
    public void GetServiceRequestShouldThrowProxyNotImplementedException()
    {
        // Arrange
        var headers = _fixture.Create<IHeaderDictionary>();

        SetRequestDetails(headers);

        // Act
        var act = _sut.GetReferrals;

        // Assert
        var ex = act.Should().Throw<ProxyNotImplementedException>().Which;
        ex.Errors.Should().ContainSingle(e => e.Code == FhirHttpErrorCodes.ProxyNotImplemented);
        ex.Message.Should().Contain("not been implemented");
    }

    [Fact]
    public void GetAppointmentsShouldThrowProxyNotImplementedException()
    {
        // Arrange
        var headers = _fixture.Create<IHeaderDictionary>();

        SetRequestDetails(headers);

        // Act
        var act = _sut.GetAppointments;

        // Assert
        var ex = act.Should().Throw<ProxyNotImplementedException>().Which;
        ex.Errors.Should().ContainSingle(e => e.Code == FhirHttpErrorCodes.ProxyNotImplemented);
        ex.Message.Should().Contain("not been implemented");
    }

    private void SetRequestDetails(IHeaderDictionary headerDictionary, string? body = null)
    {
        _sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        foreach (var keyValuePair in headerDictionary)
        {
            _sut.Request.Headers.Add(keyValuePair);
        }

        if (body is null)
        {
            return;
        }

        _sut.ControllerContext.HttpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        _sut.ControllerContext.HttpContext.Request.ContentLength = body.Length;
    }
}
