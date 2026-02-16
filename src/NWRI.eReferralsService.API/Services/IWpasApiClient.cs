namespace NWRI.eReferralsService.API.Services;

using Models.WPAS;

public interface IWpasApiClient
{
    Task<WpasCreateReferralResponse?> CreateReferralAsync(WpasCreateReferralRequest request, CancellationToken cancellationToken);
    Task<WpasCancelReferralResponse?> CancelReferralAsync(WpasCancelReferralRequest request, CancellationToken cancellationToken);
}
