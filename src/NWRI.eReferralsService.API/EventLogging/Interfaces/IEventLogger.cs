namespace NWRI.eReferralsService.API.EventLogging.Interfaces;

public interface IEventLogger
{
    void Audit(IAuditEvent auditEvent);
    void LogError(IErrorEvent errorEvent, Exception exception);
}
