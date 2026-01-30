namespace NWRI.eReferralsService.API.Services;

public interface IReferralService
{
    Task<string> ProcessMessageAsync(IHeaderDictionary headers, string requestBody, CancellationToken cancellationToken);
    Task<string> GetReferralAsync(IHeaderDictionary headers, string? id);
}
