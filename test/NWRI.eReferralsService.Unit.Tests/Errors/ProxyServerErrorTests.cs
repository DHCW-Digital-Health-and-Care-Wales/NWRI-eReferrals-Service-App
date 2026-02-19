using FluentAssertions;
using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.Constants;
using NWRI.eReferralsService.API.Errors;

namespace NWRI.eReferralsService.Unit.Tests.Errors;

public class ProxyServerErrorTests
{
    [Fact]
    public void ShouldCorrectlyCreateProxyServerError()
    {
        // Arrange
        const string expectedDisplayMessage = "500: Proxy Error.";

        // Act
        var error = new ProxyServerError();

        // Assert
        error.Code.Should().Be(FhirHttpErrorCodes.ProxyServerError);
        error.IssueType.Should().Be(OperationOutcome.IssueType.Exception);
        error.DiagnosticsMessage.Should().BeEmpty();
        error.Display.Should().Be(expectedDisplayMessage);
    }
}
