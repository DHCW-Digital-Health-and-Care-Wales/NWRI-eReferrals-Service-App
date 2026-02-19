using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using NWRI.eReferralsService.API.Models.WPAS;
using NWRI.eReferralsService.API.Validators;

namespace NWRI.eReferralsService.Integration.Tests.Services;

public class WpasWpasJsonSchemaValidatorTests
{
    [Fact]
    public void ValidateShouldReturnInvalidWhenPayloadViolatesSchema()
    {
        var sut = CreateSut();

        var payload = new WpasCreateReferralRequest
        {
            RecordId = "77220d53-3fd2-41d1-b8b3-878e6771ef75",
            ContractDetails = new WpasCreateReferralRequest.ContractDetailsModel
            {
                ProviderOrganisationCode = "T"
            },
            PatientDetails = new WpasCreateReferralRequest.PatientDetailsModel
            {
                NhsNumber = "3478526985",
                NhsNumberStatusIndicator = "01",
                PatientName = new WpasCreateReferralRequest.PatientDetailsModel.PatientNameModel
                {
                    Surname = "Jones",
                    FirstName = "Julie"
                },
                BirthDate = "19590504",
                Sex = "F",
                UsualAddress = new WpasCreateReferralRequest.PatientDetailsModel.UsualAddressModel
                {
                    NoAndStreet = "22 Brightside Crescent",
                    Town = "Overtown",
                    Postcode = "LS10 4YU",
                    Locality = ""
                }
            },
            ReferralDetails = new WpasCreateReferralRequest.ReferralDetailsModel
            {
                OutpatientReferralSource = "15",
                ReferringOrganisationCode = "TP2VC",
                ServiceTypeRequested = "6",
                ReferrerCode = "01-99999",
                AdministrativeCategory = "01",
                DateOfReferral = "20240820",
                MainSpecialty = "130",
                ReferrerPriorityType = "2",
                ReasonForReferral = "glau-sre",
                ReferralIdentifier = "140:12345678"
            }
        };

        var result = sut.ValidateWpasCreateReferralRequest(payload);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().BeNull();
    }

    [Fact]
    public void ValidateShouldReturnValidForKnownGoodPayload()
    {
        var sut = CreateSut();

        var payload = CreateKnownGoodPayload();
        var result = sut.ValidateWpasCreateReferralRequest(payload);

        result.IsValid.Should().BeTrue();
    }

    private static WpasCreateReferralRequest CreateKnownGoodPayload()
    {
        return new WpasCreateReferralRequest
        {
            RecordId = "77220d53-3fd2-41d1-b8b3-878e6771ef75",
            ContractDetails = new WpasCreateReferralRequest.ContractDetailsModel
            {
                ProviderOrganisationCode = "TP2VC"
            },
            PatientDetails = new WpasCreateReferralRequest.PatientDetailsModel
            {
                NhsNumber = "3478526985",
                NhsNumberStatusIndicator = "01",
                PatientName = new WpasCreateReferralRequest.PatientDetailsModel.PatientNameModel
                {
                    Surname = "Jones",
                    FirstName = "Julie"
                },
                BirthDate = "19590504",
                Sex = "F",
                UsualAddress = new WpasCreateReferralRequest.PatientDetailsModel.UsualAddressModel
                {
                    NoAndStreet = "22 Brightside Crescent",
                    Town = "Overtown",
                    Postcode = "LS10 4YU",
                    Locality = ""
                }
            },
            ReferralDetails = new WpasCreateReferralRequest.ReferralDetailsModel
            {
                OutpatientReferralSource = "15",
                ReferringOrganisationCode = "TP2VC",
                ServiceTypeRequested = "6",
                ReferrerCode = "01-99999",
                AdministrativeCategory = "01",
                DateOfReferral = "20240820",
                MainSpecialty = "130",
                ReferrerPriorityType = "2",
                ReasonForReferral = "glau-sre",
                ReferralIdentifier = "140:12345678"
            }
        };
    }

    private static IWpasJsonSchemaValidator CreateSut()
    {
        var hostEnvironment = new TestHostEnvironment(AppContext.BaseDirectory);
        return new WpasWpasJsonSchemaValidator(hostEnvironment);
    }

    private sealed class TestHostEnvironment(string contentRootPath) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "IntegrationTests";
        public string ContentRootPath { get; set; } = contentRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
