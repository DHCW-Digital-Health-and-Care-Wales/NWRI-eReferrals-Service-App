using NWRI.eReferralsService.API.EventLogging.Interfaces;

namespace NWRI.eReferralsService.Unit.Tests.EventLogging;

public sealed class SpyEventLogger : IEventLogger
{
    public List<IAuditEvent> AuditEvents { get; } = new();
    public List<(IErrorEvent LogErrorEvent, Exception Exception)> LogErrorEvents { get; } = new();

    public void Audit(IAuditEvent auditEvent) => AuditEvents.Add(auditEvent);

    public void LogError(IErrorEvent errorEvent, Exception exception) => LogErrorEvents.Add((errorEvent, exception));
}
