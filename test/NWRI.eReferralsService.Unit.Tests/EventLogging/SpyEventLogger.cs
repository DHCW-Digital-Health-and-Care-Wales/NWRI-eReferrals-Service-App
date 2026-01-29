using NWRI.eReferralsService.API.EventLogging.Interfaces;

namespace NWRI.eReferralsService.Unit.Tests.EventLogging;

public sealed class SpyEventLogger : IEventLogger
{
    public List<IAuditEvent> AuditEvents { get; } = [];
    public List<(IErrorEvent LogErrorEvent, Exception? Exception)> LogErrorEvents { get; } = [];

    public void Audit(IAuditEvent auditEvent) => AuditEvents.Add(auditEvent);
    public void LogError(IErrorEvent errorEvent, Exception? exception) => LogErrorEvents.Add((errorEvent, exception));
}
