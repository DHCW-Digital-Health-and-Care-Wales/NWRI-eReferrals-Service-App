namespace NWRI.eReferralsService.API.Models.WPAS.Responses;

public record WpasCreateReferralResponse : WpasReferralResponse
{
    public string? System { get; init; }
    public string? AssigningAuthority { get; init; }
    public string? OrganisationCode { get; init; }
    public string? OrganisationName { get; init; }
    public string? ReferralCreationTimestamp { get; init; }
}
