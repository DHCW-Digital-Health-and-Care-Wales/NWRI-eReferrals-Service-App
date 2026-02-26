namespace NWRI.eReferralsService.API.Services;

public interface ICapabilityStatementService
{
    Task<string> GetCapabilityStatementAsync(CancellationToken cancellationToken);
}
