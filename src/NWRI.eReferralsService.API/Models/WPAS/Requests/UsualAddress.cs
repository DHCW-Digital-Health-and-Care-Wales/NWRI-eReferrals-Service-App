namespace NWRI.eReferralsService.API.Models.WPAS.Requests;

public record UsualAddress
{
    public required string NoAndStreet { get; init; }
    public required string Town { get; init; }
    public required string Postcode { get; init; }
    public required string Locality { get; init; }
}
