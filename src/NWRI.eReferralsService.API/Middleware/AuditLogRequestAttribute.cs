namespace NWRI.eReferralsService.API.Middleware;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AuditLogRequestAttribute : Attribute;
