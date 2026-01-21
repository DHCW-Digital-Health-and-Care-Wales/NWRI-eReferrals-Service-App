using AutoFixture;
using FluentAssertions;
using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.Constants;
using NWRI.eReferralsService.API.Errors;
using NWRI.eReferralsService.Unit.Tests.Extensions;

namespace NWRI.eReferralsService.Unit.Tests.Errors;

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


