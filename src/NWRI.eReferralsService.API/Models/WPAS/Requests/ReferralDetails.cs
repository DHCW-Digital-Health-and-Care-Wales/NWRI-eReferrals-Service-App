namespace NWRI.eReferralsService.API.Models.WPAS.Requests;

public record ReferralDetails
{
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
