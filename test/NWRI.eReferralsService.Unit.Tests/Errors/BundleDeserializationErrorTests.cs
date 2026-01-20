using AutoFixture;
using FluentAssertions;
using Hl7.Fhir.Model;
using WCCG.eReferralsService.API.Constants;
using WCCG.eReferralsService.API.Errors;
using WCCG.eReferralsService.Unit.Tests.Extensions;

namespace WCCG.eReferralsService.Unit.Tests.Errors;

public class BundleDeserializationErrorTests
{
    private readonly IFixture _fixture = new Fixture().WithCustomizations();

    [Fact]
    public void ShouldCorrectlyCreateBundleDeserializationError()
    {
        //Arrange
        var exceptionMessage = _fixture.Create<string>();
        const string expectedDisplayMessage = "400: The API was unable to process the request.";

        //Act
        var error = new BundleDeserializationError(exceptionMessage);

        //Assert
        error.Code.Should().Be(FhirHttpErrorCodes.SenderBadRequest);
        error.IssueType.Should().Be(OperationOutcome.IssueType.Structure);
        error.DiagnosticsMessage.Should().Contain(exceptionMessage);
        error.Display.Should().Be(expectedDisplayMessage);
    }
}
