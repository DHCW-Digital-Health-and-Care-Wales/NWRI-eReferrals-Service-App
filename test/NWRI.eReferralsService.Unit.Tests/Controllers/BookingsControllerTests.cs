using System.Text;
using AutoFixture;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NWRI.eReferralsService.API.Constants;
using NWRI.eReferralsService.API.Controllers;
using NWRI.eReferralsService.API.Exceptions;
using NWRI.eReferralsService.API.Queries;
using NWRI.eReferralsService.Unit.Tests.Extensions;

namespace NWRI.eReferralsService.Unit.Tests.Controllers;

public class BookingsControllerTests
{
    private readonly IFixture _fixture = new Fixture().WithCustomizations();

    private readonly BookingsController _sut;

    public BookingsControllerTests()
    {
        _fixture.OmitAutoProperties = true;
        _sut = _fixture.CreateWithFrozen<BookingsController>();

        _fixture.Register<IHeaderDictionary>(() => new HeaderDictionary { { _fixture.Create<string>(), _fixture.Create<string>() } });
    }

    [Fact]
    public void GetAppointmentsShouldThrowProxyNotImplementedException()
    {
        // Arrange
        var headers = _fixture.Create<IHeaderDictionary>();

        SetRequestDetails(headers);

        // Act
        Action act = () => _sut.GetAppointments();

        // Assert
        var ex = act.Should().Throw<ProxyNotImplementedException>().Which;
        ex.Errors.Should().ContainSingle(e => e.Code == FhirHttpErrorCodes.ProxyNotImplemented);
        ex.Message.Should().Contain("not been implemented");
    }

    [Fact]
    public void GetBookingSlotShouldThrowProxyNotImplementedException()
    {
        // Arrange
        var headers = _fixture.Create<IHeaderDictionary>();
        SetRequestDetails(headers);

        var query = new GetBookingSlotQuery
        {
            Status = "free",
            Start = new[] { "ge2022-03-01T12:00:00+00:00", "le2022-03-01T13:30:00+00:00" },
            Include = new[] { "Slot:schedule", "Schedule:actor:HealthcareService" }
        };

        // Act
        Action act = () => _sut.GetBookingSlot(query);

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
