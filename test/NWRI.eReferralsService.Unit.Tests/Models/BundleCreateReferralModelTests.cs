using System.Text.Json;
using AutoFixture;
using FluentAssertions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using NWRI.eReferralsService.API.Models;
using NWRI.eReferralsService.Unit.Tests.Extensions;

namespace NWRI.eReferralsService.Unit.Tests.Models;

public class BundleCreateReferralModelTests
{
    private readonly IFixture _fixture = new Fixture().WithCustomizations();

    [Fact]
    public void ShouldSetAllProperties()
    {
        //Arrange
        var bundleJson = File.ReadAllText("TestData/example-bundle.json");

        var options = new JsonSerializerOptions()
            .ForFhir(ModelInfo.ModelInspector);
        var bundle = JsonSerializer.Deserialize<Bundle>(bundleJson, options)!;

        //Act
        var model = BundleCreateReferralModel.FromBundle(bundle);

        //Assert
        model.MessageHeader.Should().NotBeNull();
        model.ServiceRequest.Should().NotBeNull();
        model.Patient.Should().NotBeNull();
        model.Encounter.Should().NotBeNull();
        model.CarePlan.Should().NotBeNull();
        model.HealthcareService.Should().NotBeNull();
        model.Organizations.Should().NotBeNull();
        model.Practitioners.Should().NotBeNull();
        model.PractitionerRoles.Should().NotBeNull();
        model.Consents.Should().NotBeNull();
    }

    [Fact]
    public void ShouldSetNullIfNotExists()
    {
        //Arrange
        var bundle = new Bundle { Id = _fixture.Create<string>() };

        //Act
        var model = BundleCreateReferralModel.FromBundle(bundle);

        //Assert
        model.MessageHeader.Should().BeNull();
        model.ServiceRequest.Should().BeNull();
        model.Patient.Should().BeNull();
        model.Encounter.Should().BeNull();
        model.CarePlan.Should().BeNull();
        model.HealthcareService.Should().BeNull();
        model.IncidentLocation.Should().BeNull();

        model.Organizations.Should().BeEmpty();
        model.Practitioners.Should().BeEmpty();
        model.PractitionerRoles.Should().BeEmpty();
        model.Observations.Should().BeEmpty();
        model.SceneSafetyFlags.Should().BeEmpty();
        model.Flags.Should().BeEmpty();
        model.MedicationStatements.Should().BeEmpty();
        model.AllergyIntolerances.Should().BeEmpty();
        model.Questionnaires.Should().BeEmpty();
        model.QuestionnaireResponses.Should().BeEmpty();
        model.Consents.Should().BeEmpty();
        model.Conditions.Should().BeEmpty();
        model.Tasks.Should().BeEmpty();
        model.Communications.Should().BeEmpty();
        model.Procedures.Should().BeEmpty();
    }
}
