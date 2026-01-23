namespace NWRI.eReferralsService.API.EventLogging.Interfaces;

public interface IEventLogger
{
    void Audit(IAuditEvent auditEvent);
    void Error(IErrorEvent errorEvent, Exception exception);
}
