namespace NWRI.eReferralsService.API.Models.WPAS;

public sealed class WpasCreateReferralResponse : IWpasReferralResponse
{
    public string? System { get; init; }
    public string? AssigningAuthority { get; init; }
    public string? OrganisationCode { get; init; }
    public string? OrganisationName { get; init; }
    public string? ReferralId { get; init; }
    public string? ReferralCreationTimestamp { get; init; }
}
