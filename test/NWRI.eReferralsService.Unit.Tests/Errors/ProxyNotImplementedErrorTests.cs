using AutoFixture;
using FluentAssertions;
using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.Constants;
using NWRI.eReferralsService.API.Errors;
using NWRI.eReferralsService.Unit.Tests.Extensions;

namespace NWRI.eReferralsService.Unit.Tests.Errors;

public class ProxyNotImplementedErrorTests
{
    private readonly IFixture _fixture = new Fixture().WithCustomizations();

    [Fact]
    public void ShouldCorrectlyCreateProxyNotImplementedError()
    {
        // Arrange
        var exceptionMessage = _fixture.Create<string>();
        var expectedDiagnosticsMessage =
            $"Not Implemented error: {exceptionMessage}";
        const string expectedDisplayMessage =
            "501: BaRS did not recognize the request. This request has not been implemented within the Api.";

        // Act
        var error = new ProxyNotImplementedError(exceptionMessage);

        // Assert
        error.Code.Should().Be(FhirHttpErrorCodes.ProxyNotImplemented);
        error.IssueType.Should().Be(OperationOutcome.IssueType.NotSupported);
        error.DiagnosticsMessage.Should().Be(expectedDiagnosticsMessage);
        error.Display.Should().Be(expectedDisplayMessage);
    }
}
