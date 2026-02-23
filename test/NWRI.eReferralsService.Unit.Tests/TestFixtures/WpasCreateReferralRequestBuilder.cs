using NWRI.eReferralsService.API.Models.WPAS.Requests;

namespace NWRI.eReferralsService.Unit.Tests.TestFixtures;

public static class WpasCreateReferralRequestBuilder
{
    public const string ValidReferralId = "140:12345678";

    public static WpasCreateReferralRequest CreateValid(string providerOrganisationCode = "TP2VC")
    {
        return new WpasCreateReferralRequest
        {
            RecordId = "77220d53-3fd2-41d1-b8b3-878e6771ef75",
            ContractDetails = new ContractDetails
            {
                ProviderOrganisationCode = providerOrganisationCode
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
                ReferralIdentifier = ValidReferralId
            }
        };
    }
}
