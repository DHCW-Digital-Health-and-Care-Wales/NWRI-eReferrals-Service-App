namespace NWRI.eReferralsService.API.Services;

public interface ICapabilityStatementProvider
{
    Task<string> GetCapabilityStatementAsync(CancellationToken ct);
}
