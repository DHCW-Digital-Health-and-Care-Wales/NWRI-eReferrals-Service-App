using System.Text.Json;
using FluentAssertions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using NWRI.eReferralsService.API.Models;
using NWRI.eReferralsService.API.Services;
// ReSharper disable NullableWarningSuppressionIsUsed

namespace NWRI.eReferralsService.Unit.Tests.Services;

public class WpasCreateReferralRequestMapperTests
{
    [Fact]
    public void MapShouldProduceSchemaValidPayloadFromExampleBundle()
    {
        var bundleJson = File.ReadAllText("TestData/example-bundle.json");
        var options = new JsonSerializerOptions().ForFhir(ModelInfo.ModelInspector);
        var bundle = JsonSerializer.Deserialize<Bundle>(bundleJson, options)!;
        var model = BundleCreateReferralModel.FromBundle(bundle);

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
}
