namespace NWRI.eReferralsService.API.Services;

public interface IWpasApiClient
{
    Task<string> CreateReferralAsync(string requestBody, CancellationToken cancellationToken);
    Task<string> CancelReferralAsync(string requestBody, CancellationToken cancellationToken);
}
