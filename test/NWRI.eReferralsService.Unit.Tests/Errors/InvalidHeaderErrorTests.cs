using AutoFixture;
using FluentAssertions;
using Hl7.Fhir.Model;
using WCCG.eReferralsService.API.Constants;
using WCCG.eReferralsService.API.Errors;
using WCCG.eReferralsService.Unit.Tests.Extensions;

namespace WCCG.eReferralsService.Unit.Tests.Errors;

public class InvalidHeaderErrorTests
{
    private readonly IFixture _fixture = new Fixture().WithCustomizations();

    [Fact]
    public void ShouldCorrectlyCreateHeaderValidationError()
    {
        //Arrange
        var validationMessage = _fixture.Create<string>();
        const string expectedDisplayMessage = "400: The API was unable to process the request.";

        //Act
        var error = new InvalidHeaderError(validationMessage);

        //Assert
        error.Code.Should().Be(FhirHttpErrorCodes.SenderBadRequest);
        error.IssueType.Should().Be(OperationOutcome.IssueType.Invalid);
        error.DiagnosticsMessage.Should().Be(validationMessage);
        error.Display.Should().Be(expectedDisplayMessage);
    }
}
