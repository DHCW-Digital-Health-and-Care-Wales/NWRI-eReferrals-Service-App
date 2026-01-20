using AutoFixture;
using FluentAssertions;
using Hl7.Fhir.Model;
using WCCG.eReferralsService.API.Constants;
using WCCG.eReferralsService.API.Errors;
using WCCG.eReferralsService.Unit.Tests.Extensions;

namespace WCCG.eReferralsService.Unit.Tests.Errors;

public class ApiCallErrorTests
{
    private readonly IFixture _fixture = new Fixture().WithCustomizations();

    [Fact]
    public void ShouldCorrectlyCreateApiCallError()
    {
        //Arrange
        var errorMessage = _fixture.Create<string>();
        const string expectedDisplayMessage = "503: The Receiver is currently unavailable.";

        //Act
        var error = new ApiCallError(errorMessage);

        //Assert
        error.Code.Should().Be(FhirHttpErrorCodes.ReceiverUnavailable);
        error.IssueType.Should().Be(OperationOutcome.IssueType.Transient);
        error.DiagnosticsMessage.Should().Be($"Unexpected receiver error: {errorMessage}");
        error.Display.Should().Be(expectedDisplayMessage);
    }
}
