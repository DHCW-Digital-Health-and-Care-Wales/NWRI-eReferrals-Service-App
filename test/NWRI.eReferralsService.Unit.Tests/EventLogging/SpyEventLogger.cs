using NWRI.eReferralsService.API.EventLogging;
using NWRI.eReferralsService.API.EventLogging.Interfaces;

namespace NWRI.eReferralsService.Unit.Tests.EventLogging;

public sealed class SpyEventLogger : IEventLogger
{
    public List<IAuditEvent> AuditEvents { get; } = new();
    public List<(IErrorEvent ErrorEvent, Exception Exception)> ErrorEvents { get; } = new();

    public void Audit(IAuditEvent auditEvent) => AuditEvents.Add(auditEvent);

    public void Error(IErrorEvent errorEvent, Exception exception) => ErrorEvents.Add((errorEvent, exception));
}
