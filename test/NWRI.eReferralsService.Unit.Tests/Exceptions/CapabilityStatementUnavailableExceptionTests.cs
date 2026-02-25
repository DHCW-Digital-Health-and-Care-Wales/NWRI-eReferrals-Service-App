using FluentAssertions;
using NWRI.eReferralsService.API.Errors;
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
            $"CapabilityStatement JSON resource was not found. ResourcePath='{resourcePath}'. Cause='{cause}'.";
        var error = new CapabilityStatementNotFoundError(resourcePath, cause);

        // Act
        var exception = new CapabilityStatementUnavailableException(error);

        // Assert
        exception.Message.Should().Be(expectedDiagnostics);
        exception.Errors.Should().ContainSingle()
            .Which.Should().BeOfType<CapabilityStatementNotFoundError>()
            .Which.DiagnosticsMessage.Should().Be(expectedDiagnostics);
    }

    [Fact]
    public void ShouldCreateWithLoadError()
    {
        // Arrange
        const string resourcePath = "Resources/Fhir/metadata-capability-statement-response.json";
        const string cause = "disk read failure";
        var expectedDiagnostics =
            $"CapabilityStatement JSON resource could not be loaded. ResourcePath='{resourcePath}'. Cause='{cause}'.";
        var error = new CapabilityStatementLoadError(resourcePath, cause);

        // Act
        var exception = new CapabilityStatementUnavailableException(error);

        // Assert
        exception.Message.Should().Be(expectedDiagnostics);
        exception.Errors.Should().ContainSingle()
            .Which.Should().BeOfType<CapabilityStatementLoadError>()
            .Which.DiagnosticsMessage.Should().Be(expectedDiagnostics);
    }
}
