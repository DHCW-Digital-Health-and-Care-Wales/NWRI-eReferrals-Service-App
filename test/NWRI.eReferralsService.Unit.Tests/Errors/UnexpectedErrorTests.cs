using AutoFixture;
using FluentAssertions;
using Hl7.Fhir.Model;
using WCCG.eReferralsService.API.Constants;
using WCCG.eReferralsService.API.Errors;
using WCCG.eReferralsService.Unit.Tests.Extensions;

namespace WCCG.eReferralsService.Unit.Tests.Errors;

public class UnexpectedErrorTests
{
    private readonly IFixture _fixture = new Fixture().WithCustomizations();

    [Fact]
    public void ShouldCorrectlyCreateUnexpectedError()
    {
        //Arrange
        var exceptionMessage = _fixture.Create<string>();
        var expectedDetailsMessage = $"Unexpected error: {exceptionMessage}";
        const string expectedDisplayMessage = "500: The Receiver has encountered an error processing the request.";

        //Act
        var error = new UnexpectedError(exceptionMessage);

        //Assert
        error.Code.Should().Be(FhirHttpErrorCodes.ReceiverServerError);
        error.IssueType.Should().Be(OperationOutcome.IssueType.Transient);
        error.DiagnosticsMessage.Should().Be(expectedDetailsMessage);
        error.Display.Should().Be(expectedDisplayMessage);
    }
}
