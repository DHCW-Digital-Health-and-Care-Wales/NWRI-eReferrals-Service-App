namespace NWRI.eReferralsService.API.Services;

public interface IReferralService
{
    Task<string> ProcessMessageAsync(IHeaderDictionary headers, string requestBody);
    Task<string> GetReferralAsync(IHeaderDictionary headers, string? id);
}
