namespace NWRI.eReferralsService.API.Services;

public interface ICapabilityStatementProvider
{
    Task<string> GetCapabilityStatementJsonAsync(CancellationToken ct);
}
