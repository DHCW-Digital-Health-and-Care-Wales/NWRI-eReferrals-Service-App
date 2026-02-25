using AutoFixture;
using FluentAssertions;
using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.Constants;
using NWRI.eReferralsService.API.Errors;
using NWRI.eReferralsService.Unit.Tests.Extensions;

namespace NWRI.eReferralsService.Unit.Tests.Errors;

public class CapabilityStatementNotFoundErrorTests
{
    private readonly IFixture _fixture = new Fixture().WithCustomizations();

    [Fact]
    public void ShouldCorrectlyCreateCapabilityStatementNotFoundError()
    {
        // Arrange
        var resourcePath = _fixture.Create<string>();
        var cause = _fixture.Create<string>();
        var expectedDiagnostics =
            $"CapabilityStatement JSON resource was not found. ResourcePath='{resourcePath}'. Cause='{cause}'.";
        const string expectedDisplayMessage = "500: The Proxy encountered an internal error while processing the request.";

        // Act
        var error = new CapabilityStatementNotFoundError(resourcePath, cause);

        // Assert
        error.Code.Should().Be(FhirHttpErrorCodes.ProxyServerError);
        error.IssueType.Should().Be(OperationOutcome.IssueType.Exception);
        error.DiagnosticsMessage.Should().Be(expectedDiagnostics);
        error.Display.Should().Be(expectedDisplayMessage);
    }
}
