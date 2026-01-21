using FluentAssertions;
using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.Constants;

namespace NWRI.eReferralsService.Unit.Tests.Constants;

public class ValidationMessagesTests
{
    [Fact]
    public void ShouldUsePropertyNameAsProvidedForMissingEntityField()
    {
        var message = ValidationMessages.MissingEntityField<ServiceRequest>(nameof(ServiceRequest.BasedOn));

        message.Should().Be("ServiceRequest.BasedOn is required");
    }

    [Fact]
    public void ShouldUseProvidedLabelForMissingEntityField()
    {
        var message = ValidationMessages.MissingEntityField<ServiceRequest>("occurrencePeriod");

        message.Should().Be("ServiceRequest.occurrencePeriod is required");
    }
}


