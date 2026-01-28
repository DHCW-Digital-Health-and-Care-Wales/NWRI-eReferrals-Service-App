using NWRI.eReferralsService.API.EventLogging.Interfaces;

namespace NWRI.eReferralsService.Integration.Tests.TestDoubles;

public sealed class NoopEventLogger : IEventLogger
{
    public void Audit(IAuditEvent auditEvent) { }

    public void LogError(IErrorEvent errorEvent, Exception exception) { }
}
