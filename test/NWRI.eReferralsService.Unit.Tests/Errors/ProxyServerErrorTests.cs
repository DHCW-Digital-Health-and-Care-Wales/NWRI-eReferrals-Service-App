using AutoFixture;
using FluentAssertions;
using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.Constants;
using NWRI.eReferralsService.API.Errors;
using NWRI.eReferralsService.Unit.Tests.Extensions;

namespace NWRI.eReferralsService.Unit.Tests.Errors;

public class ProxyServerErrorTests
{
    private readonly IFixture _fixture = new Fixture().WithCustomizations();

    [Fact]
    public void ShouldCorrectlyCreateProxyServerError()
    {
        //Arrange
        var exceptionMessage = _fixture.Create<string>();
        var expectedDetailsMessage = $"Proxy server error: {exceptionMessage}";
        const string expectedDisplayMessage = "500: The Proxy encountered an internal error while processing the request.";

        // Act
        var error = new ProxyServerError(exceptionMessage);

        // Assert
        error.Code.Should().Be(FhirHttpErrorCodes.ProxyServerError);
        error.IssueType.Should().Be(OperationOutcome.IssueType.Exception);
        error.DiagnosticsMessage.Should().Be(expectedDetailsMessage);
        error.Display.Should().Be(expectedDisplayMessage);
    }
}
