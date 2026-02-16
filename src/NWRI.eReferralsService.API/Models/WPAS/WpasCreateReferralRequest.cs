using System.Text.Json.Serialization;

namespace NWRI.eReferralsService.API.Models.WPAS;

public sealed class WpasCreateReferralRequest
{
    public required string RecordId { get; init; }
    public required ContractDetailsModel ContractDetails { get; init; }
    public required PatientDetailsModel PatientDetails { get; init; }
    public required ReferralDetailsModel ReferralDetails { get; init; }

    public sealed class ContractDetailsModel
    {
        public required string ProviderOrganisationCode { get; init; }
    }

    public sealed class PatientDetailsModel
    {
        public required string NhsNumber { get; init; }
        public required string NhsNumberStatusIndicator { get; init; }
        public required PatientNameModel PatientName { get; init; }
        public required string BirthDate { get; init; }
        public required string Sex { get; init; }
        public required UsualAddressModel UsualAddress { get; init; }

        public sealed class PatientNameModel
        {
            public required string Surname { get; init; }
            public required string FirstName { get; init; }
        }

        public sealed class UsualAddressModel
        {
            public required string NoAndStreet { get; init; }
            public required string Town { get; init; }
            public required string Postcode { get; init; }
            public required string Locality { get; init; }
        }
    }

    public sealed class ReferralDetailsModel
    {
        // TODO: TBC - Typo?
        [JsonPropertyName("OupatientReferralSource")]
        public required string OutpatientReferralSource { get; init; }
        public required string ReferringOrganisationCode { get; init; }
        public required string ServiceTypeRequested { get; init; }
        public required string ReferrerCode { get; init; }
        public required string AdministrativeCategory { get; init; }
        public required string DateOfReferral { get; init; }
        public required string MainSpecialty { get; init; }
        public required string ReferrerPriorityType { get; init; }
        public required string ReasonForReferral { get; init; }
        public required string ReferralIdentifier { get; init; }
    }
}
