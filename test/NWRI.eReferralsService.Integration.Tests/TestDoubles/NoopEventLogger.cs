using NWRI.eReferralsService.API.EventLogging;
using NWRI.eReferralsService.API.EventLogging.Interfaces;

namespace NWRI.eReferralsService.Integration.Tests.TestDoubles;

public sealed class NoopEventLogger : IEventLogger
{
    public void Audit(IAuditEvent auditEvent) { }

    public void Error(IErrorEvent errorEvent, Exception exception) { }
}
