using FluentAssertions;
using NWRI.eReferralsService.API.Errors;
using NWRI.eReferralsService.API.Exceptions;
using static Hl7.Fhir.Model.VerificationResult;

namespace NWRI.eReferralsService.Unit.Tests.Exceptions;

public class CapabilityStatementUnavailableExceptionTests
{
    [Fact]
    public void ShouldCorrectlyCreateCapabilityStatementUnavailableException()
    {
        //Arrange
        var expectedMessage = $"Proxy Error.";

        // Act
        var exception = new CapabilityStatementUnavailableException();

        // Assert
        exception.Message.Should().Be(expectedMessage);

        exception.Errors.Should().NotBeNull();
        exception.Errors.Should().HaveCount(1);

        exception.Errors.Single().Should().BeOfType<ProxyServerError>()
            .Which.Code.Should().Be(API.Constants.FhirHttpErrorCodes.ProxyServerError);
    }
}
