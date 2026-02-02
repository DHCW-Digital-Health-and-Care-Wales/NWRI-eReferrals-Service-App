using FluentAssertions;
using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.Constants;
using NWRI.eReferralsService.API.Errors;

namespace NWRI.eReferralsService.Unit.Tests.Errors;

public class ProxyNotImplementedErrorTests
{
    [Fact]
    public void ShouldCorrectlyCreateProxyNotImplementedError()
    {
        // Arrange
        const string expectedDisplayMessage =
            "501: BaRS did not recognize the request. This request has not been implemented within the Api.";

        // Act
        var error = new ProxyNotImplementedError();

        // Assert
        error.Code.Should().Be(FhirHttpErrorCodes.ProxyNotImplemented);
        error.IssueType.Should().Be(OperationOutcome.IssueType.NotSupported);
        error.Display.Should().Be(expectedDisplayMessage);
    }
}
