using NWRI.eReferralsService.API.EventLogging.Interfaces;

namespace NWRI.eReferralsService.API.EventLogging;

public sealed class AuditContextAccessor : IAuditContextAccessor
{
    private static readonly AsyncLocal<AuditContextHolder> AuditContextCurrent = new();

    public AuditContext? Current
    {
        get => AuditContextCurrent.Value?.Context;
        set
        {
            var holder = AuditContextCurrent.Value;
            if (holder is not null)
            {
                holder.Context = null;
            }

            if (value is not null)
            {
                AuditContextCurrent.Value = new AuditContextHolder { Context = value };
            }
        }
    }

    private sealed class AuditContextHolder
    {
        public AuditContext? Context;
    }
}
