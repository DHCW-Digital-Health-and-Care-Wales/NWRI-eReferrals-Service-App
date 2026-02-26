using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.Models.WPAS.Responses;

namespace NWRI.eReferralsService.API.Services;

public interface IReferralWorkflowProcessor
{
    Task<WpasCreateReferralResponse> ProcessCreateAsync(Bundle bundle, CancellationToken cancellationToken);
    Task<WpasCancelReferralResponse> ProcessCancelAsync(Bundle bundle, CancellationToken cancellationToken);
}
