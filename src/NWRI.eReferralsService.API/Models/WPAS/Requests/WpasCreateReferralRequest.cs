namespace NWRI.eReferralsService.API.Models.WPAS.Requests;

public record WpasCreateReferralRequest
{
    public required string RecordId { get; init; }
    public required ContractDetails ContractDetails { get; init; }
    public required PatientDetails PatientDetails { get; init; }
    public required ReferralDetails ReferralDetails { get; init; }
}
