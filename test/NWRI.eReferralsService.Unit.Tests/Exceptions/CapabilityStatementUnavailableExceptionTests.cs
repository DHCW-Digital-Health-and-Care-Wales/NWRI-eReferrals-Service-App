using FluentAssertions;
using NWRI.eReferralsService.API.Exceptions;

namespace NWRI.eReferralsService.Unit.Tests.Exceptions;

public class CapabilityStatementUnavailableExceptionTests
{
    [Fact]
    public void ShouldCreateWithNotFoundError()
    {
        // Arrange
        const string resourcePath = "Resources/Fhir/metadata-capability-statement-response.json";
        const string cause = "File does not exist.";
        var expectedDiagnostics =
            $"CapabilityStatement resource unavailable. ResourcePath='{resourcePath}'. Cause='{cause}'.";

        // Act
        var exception = new CapabilityStatementUnavailableException(resourcePath, cause);

        // Assert
        exception.Message.Should().Be(expectedDiagnostics);
        exception.Errors.Should().ContainSingle()
            .Which.DiagnosticsMessage.Should().Be(expectedDiagnostics);
    }

    [Fact]
    public void ShouldCreateWithLoadError()
    {
        // Arrange
        const string resourcePath = "Resources/Fhir/metadata-capability-statement-response.json";
        const string cause = "disk read failure";
        var expectedDiagnostics =
            $"CapabilityStatement resource unavailable. ResourcePath='{resourcePath}'. Cause='{cause}'.";

        // Act
        var exception = new CapabilityStatementUnavailableException(resourcePath, cause);

        // Assert
        exception.Message.Should().Be(expectedDiagnostics);
        exception.Errors.Should().ContainSingle()
            .Which.DiagnosticsMessage.Should().Be(expectedDiagnostics);
    }
}
