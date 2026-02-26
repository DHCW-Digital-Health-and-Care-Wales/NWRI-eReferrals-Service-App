using NWRI.eReferralsService.API.Models.WPAS.Requests;
using NWRI.eReferralsService.API.Models.WPAS.Responses;

namespace NWRI.eReferralsService.API.Services;

public interface IWpasApiClient
{
    Task<WpasCreateReferralResponse> CreateReferralAsync(WpasCreateReferralRequest request, CancellationToken cancellationToken);
    Task<WpasCancelReferralResponse> CancelReferralAsync(WpasCancelReferralRequest request, CancellationToken cancellationToken);
}
