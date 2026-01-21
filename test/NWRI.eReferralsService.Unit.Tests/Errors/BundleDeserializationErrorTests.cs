using AutoFixture;
using FluentAssertions;
using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.Constants;
using NWRI.eReferralsService.API.Errors;
using NWRI.eReferralsService.Unit.Tests.Extensions;

namespace NWRI.eReferralsService.Unit.Tests.Errors;

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


