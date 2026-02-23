using FluentAssertions;
using NWRI.eReferralsService.API.Constants;
using NWRI.eReferralsService.API.Errors;
using NWRI.eReferralsService.API.Exceptions;
using static Hl7.Fhir.Model.VerificationResult;

namespace NWRI.eReferralsService.Unit.Tests.Exceptions;

public class CapabilityStatementUnavailableExceptionTests
{
    [Fact]
    public void ShouldCorrectlyCreateCapabilityStatementUnavailableException()
    {
        // Arrange
        const string expectedMessage = "CapabilityStatement resource is unavailable.";
        const string expectedDiagnostics = $"Proxy server error: {expectedMessage}";

        // Act
        var exception = new CapabilityStatementUnavailableException();

        // Assert
        exception.Message.Should().Be(expectedMessage);

        exception.Errors.Should().NotBeNull();
        exception.Errors.Should().HaveCount(1);

        var error = exception.Errors.Single().Should().BeOfType<ProxyServerError>().Subject;
        error.Code.Should().Be(FhirHttpErrorCodes.ProxyServerError);
        error.DiagnosticsMessage.Should().Be(expectedDiagnostics);
    }
}
