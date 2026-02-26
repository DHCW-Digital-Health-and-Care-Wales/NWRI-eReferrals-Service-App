using System.Text.Json;
using FluentAssertions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using NWRI.eReferralsService.API.Mappers;
using NWRI.eReferralsService.API.Models;
// ReSharper disable NullableWarningSuppressionIsUsed

namespace NWRI.eReferralsService.Unit.Tests.Services;

public class WpasCreateReferralRequestMapperTests
{
    private static BundleCreateReferralModel CreateValidModelFromExampleBundle()
    {
        var bundleJson = File.ReadAllText("TestData/example-bundle.json");
        var options = new JsonSerializerOptions().ForFhir(ModelInfo.ModelInspector);
        var bundle = JsonSerializer.Deserialize<Bundle>(bundleJson, options)!;
        return BundleCreateReferralModel.FromBundle(bundle);
    }

    [Fact]
    public void MapShouldProduceSchemaValidPayloadFromExampleBundle()
    {
        var model = CreateValidModelFromExampleBundle();

        var mapper = new WpasCreateReferralRequestMapper();
        var payload = mapper.Map(model);

        payload.ContractDetails.ProviderOrganisationCode.Should().Be("7A4BV");
        payload.ReferralDetails.ReferringOrganisationCode.Should().Be("7A4BV");
        payload.ReferralDetails.OutpatientReferralSource.Should().Be("TP2VC");
        payload.PatientDetails.NhsNumber.Should().Be("3478526985");
        payload.PatientDetails.NhsNumberStatusIndicator.Should().Be("01");
        payload.PatientDetails.PatientName.Surname.Should().Be("Jones");
        payload.PatientDetails.PatientName.FirstName.Should().Be("Julie");
        payload.PatientDetails.BirthDate.Should().Be("19590504");
        payload.PatientDetails.Sex.Should().Be("F");
        payload.ReferralDetails.ServiceTypeRequested.Should().Be("6");
        payload.ReferralDetails.AdministrativeCategory.Should().Be("01");
        payload.ReferralDetails.ReferrerCode.Should().Be("PT2489");
        payload.ReferralDetails.DateOfReferral.Should().Be("20240820");
        payload.ReferralDetails.MainSpecialty.Should().Be("130");
        payload.ReferralDetails.ReferrerPriorityType.Should().Be("2");
        payload.ReferralDetails.ReasonForReferral.Should().Be("Glaucoma");
        payload.ReferralDetails.ReferralIdentifier.Length.Should().BeLessOrEqualTo(12);
    }

    [Theory]
    [InlineData("Dry eye", "Dry eye ")]
    [InlineData("Glaucoma", "Glaucoma")]
    [InlineData("Glaucoma suspect", "Glaucoma")]
    [InlineData("   Dry eye   ", "Dry eye ")]
    public void MapShouldFormatReasonForReferralAsFixedWidthLeftJustified(string inputDisplay, string expected)
    {
        var bundleJson = File.ReadAllText("TestData/example-bundle.json");
        var options = new JsonSerializerOptions().ForFhir(ModelInfo.ModelInspector);
        var bundle = JsonSerializer.Deserialize<Bundle>(bundleJson, options)!;

        var condition = bundle.Entry
            .Select(e => e.Resource)
            .OfType<Condition>()
            .First();

        condition.Code!.Coding.First().Display = inputDisplay;

        var model = BundleCreateReferralModel.FromBundle(bundle);

        var mapper = new WpasCreateReferralRequestMapper();
        var payload = mapper.Map(model);

        payload.ReferralDetails.ReasonForReferral.Should().Be(expected);
        payload.ReferralDetails.ReasonForReferral.Length.Should().Be(8);
    }

    [Fact]
    public void MapShouldThrowWhenConditionsMissing()
    {
        var model = CreateValidModelFromExampleBundle();
        model.Conditions = [];

        var mapper = new WpasCreateReferralRequestMapper();
        var act = () => mapper.Map(model);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MapShouldThrowWhenReceivingPerformingOrganizationMissing()
    {
        var model = CreateValidModelFromExampleBundle();
        model.Organizations = model.Organizations!
            .Where(o => !StringComparer.InvariantCultureIgnoreCase.Equals(o.Name, "Receiving/performing Organization"))
            .ToList();

        var mapper = new WpasCreateReferralRequestMapper();
        var act = () => mapper.Map(model);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MapShouldThrowWhenSenderOrganizationMissing()
    {
        var model = CreateValidModelFromExampleBundle();
        model.Organizations = model.Organizations!
            .Where(o => !StringComparer.InvariantCultureIgnoreCase.Equals(o.Name, "Sender Organization"))
            .ToList();

        var mapper = new WpasCreateReferralRequestMapper();
        var act = () => mapper.Map(model);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MapShouldThrowWhenPatientAddressMissing()
    {
        var model = CreateValidModelFromExampleBundle();
        model.Patient!.Address = [];

        var mapper = new WpasCreateReferralRequestMapper();
        var act = () => mapper.Map(model);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MapShouldSetEmptyBirthDateWhenPatientBirthDateMissing()
    {
        var model = CreateValidModelFromExampleBundle();
        model.Patient!.BirthDate = null;

        var mapper = new WpasCreateReferralRequestMapper();
        var payload = mapper.Map(model);

        payload.PatientDetails.BirthDate.Should().BeEmpty();
    }

    [Fact]
    public void MapShouldThrowWhenPatientNhsNumberMissing()
    {
        var model = CreateValidModelFromExampleBundle();

        model.Patient!.Identifier = model.Patient.Identifier
            .Where(i => !string.Equals(i.System, "https://fhir.nhs.uk/Id/nhs-number", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var mapper = new WpasCreateReferralRequestMapper();
        var act = () => mapper.Map(model);

        act.Should().Throw<InvalidOperationException>();
    }
}
