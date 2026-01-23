namespace NWRI.eReferralsService.API.EventLogging.Interfaces;

public interface IAuditContextAccessor
{
    AuditContext? Current { get; set; }
}
