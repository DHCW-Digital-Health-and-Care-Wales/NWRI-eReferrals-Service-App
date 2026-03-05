using FluentAssertions;
using NWRI.eReferralsService.API.Exceptions;

namespace NWRI.eReferralsService.Unit.Tests.Exceptions;

public class CapabilityStatementUnavailableExceptionTests
{
    [Theory]
    [InlineData("File does not exist.")]
    [InlineData("disk read failure")]
    public void ShouldCreateWithExpectedDiagnostics(string cause)
    {
        // Arrange
        const string resourcePath = "Resources/Fhir/metadata-capability-statement-response.json";
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
