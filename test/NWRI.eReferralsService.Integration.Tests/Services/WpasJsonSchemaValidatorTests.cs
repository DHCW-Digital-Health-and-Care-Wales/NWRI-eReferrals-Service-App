using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using NWRI.eReferralsService.API.Models.WPAS.Requests;
using NWRI.eReferralsService.API.Validators;

namespace NWRI.eReferralsService.Integration.Tests.Services;

public class WpasJsonSchemaValidatorTests
{
    [Fact]
    public void ValidateShouldReturnInvalidWhenPayloadViolatesSchema()
    {
        var sut = CreateSut();

        var payload = new WpasCreateReferralRequest
        {
            RecordId = "77220d53-3fd2-41d1-b8b3-878e6771ef75",
            ContractDetails = new ContractDetails
            {
                ProviderOrganisationCode = "T"
            },
            PatientDetails = new PatientDetails
            {
                NhsNumber = "3478526985",
                NhsNumberStatusIndicator = "01",
                PatientName = new PatientName
                {
                    Surname = "Jones",
                    FirstName = "Julie"
                },
                BirthDate = "19590504",
                Sex = "F",
                UsualAddress = new UsualAddress
                {
                    NoAndStreet = "22 Brightside Crescent",
                    Town = "Overtown",
                    Postcode = "LS10 4YU",
                    Locality = ""
                }
            },
            ReferralDetails = new ReferralDetails
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
            ContractDetails = new ContractDetails
            {
                ProviderOrganisationCode = "TP2VC"
            },
            PatientDetails = new PatientDetails
            {
                NhsNumber = "3478526985",
                NhsNumberStatusIndicator = "01",
                PatientName = new PatientName
                {
                    Surname = "Jones",
                    FirstName = "Julie"
                },
                BirthDate = "19590504",
                Sex = "F",
                UsualAddress = new UsualAddress
                {
                    NoAndStreet = "22 Brightside Crescent",
                    Town = "Overtown",
                    Postcode = "LS10 4YU",
                    Locality = ""
                }
            },
            ReferralDetails = new ReferralDetails
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

    private static WpasJsonSchemaValidator CreateSut()
    {
        var hostEnvironment = new TestHostEnvironment(AppContext.BaseDirectory);
        return new WpasJsonSchemaValidator(hostEnvironment);
    }

    private sealed class TestHostEnvironment(string contentRootPath) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "IntegrationTests";
        public string ContentRootPath { get; set; } = contentRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
