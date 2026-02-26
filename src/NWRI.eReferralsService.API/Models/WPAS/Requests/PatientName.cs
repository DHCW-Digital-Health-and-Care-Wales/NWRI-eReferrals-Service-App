namespace NWRI.eReferralsService.API.Models.WPAS.Requests;

public record PatientName
{
    public required string Surname { get; init; }
    public required string FirstName { get; init; }
}
