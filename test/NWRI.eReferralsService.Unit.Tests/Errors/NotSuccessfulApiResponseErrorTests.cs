using AutoFixture;
using FluentAssertions;
using Hl7.Fhir.Model;
using WCCG.eReferralsService.API.Constants;
using WCCG.eReferralsService.API.Errors;
using WCCG.eReferralsService.Unit.Tests.Extensions;

namespace WCCG.eReferralsService.Unit.Tests.Errors;

public class NotSuccessfulApiResponseErrorTests
{
    private readonly IFixture _fixture = new Fixture().WithCustomizations();

    [Fact]
    public void ShouldCorrectlyCreateNotSuccessfulApiResponseError()
    {
        //Arrange
        const string errorCode = FhirHttpErrorCodes.ReceiverUnavailable;
        const string expectedDisplayMessage = "503: The Receiver is currently unavailable.";

        var errorMessage = _fixture.Create<string>();

        //Act
        var error = new NotSuccessfulApiResponseError(errorCode, errorMessage);

        //Assert
        error.Code.Should().Be(errorCode);
        error.IssueType.Should().Be(OperationOutcome.IssueType.Transient);
        error.DiagnosticsMessage.Should().Be($"Receiver error. {errorMessage}");
        error.Display.Should().Be(expectedDisplayMessage);
    }
}
