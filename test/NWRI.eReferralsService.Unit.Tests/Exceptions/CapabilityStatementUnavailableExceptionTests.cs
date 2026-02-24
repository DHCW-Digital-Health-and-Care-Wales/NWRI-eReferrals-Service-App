using FluentAssertions;
using NWRI.eReferralsService.API.Constants;
using NWRI.eReferralsService.API.Errors;
using NWRI.eReferralsService.API.Exceptions;

namespace NWRI.eReferralsService.Unit.Tests.Exceptions;

public class CapabilityStatementUnavailableExceptionTests
{
    [Fact]
    public void ShouldCorrectlyCreateCapabilityStatementUnavailableException()
    {
        // Arrange
        const string resourcePath = "Resources/Fhir/metadata-capability-statement-response.json";
        const string expectedMessage = "CapabilityStatement resource is unavailable.";
        var expectedDiagnostics = $"CapabilityStatement JSON resource was not found. ResourcePath='{resourcePath}'.";
        var cause = new FileNotFoundException("file not found", "x");

        // Act
        var exception = new CapabilityStatementUnavailableException(cause, resourcePath);

        // Assert
        exception.Message.Should().Be(expectedMessage);
        exception.Cause.Should().BeSameAs(cause);

        exception.Errors.Should().NotBeNull();
        exception.Errors.Should().HaveCount(1);

        var error = exception.Errors.Single().Should().BeOfType<ProxyServerError>().Subject;
        error.Code.Should().Be(FhirHttpErrorCodes.ProxyServerError);
        error.DiagnosticsMessage.Should().Be(expectedDiagnostics);
    }
}
