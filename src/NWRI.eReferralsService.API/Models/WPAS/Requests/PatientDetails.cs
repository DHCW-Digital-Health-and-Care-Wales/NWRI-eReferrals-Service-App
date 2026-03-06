namespace NWRI.eReferralsService.API.Models.WPAS.Requests;

public record PatientDetails
{
    public required string NhsNumber { get; init; }
    public required string NhsNumberStatusIndicator { get; init; }
    public required PatientName PatientName { get; init; }
    public required string BirthDate { get; init; }
    public required string Sex { get; init; }
    public required UsualAddress UsualAddress { get; init; }
}
