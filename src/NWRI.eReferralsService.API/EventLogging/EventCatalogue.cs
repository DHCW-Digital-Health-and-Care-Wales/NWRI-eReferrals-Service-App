using System.ComponentModel;
using NWRI.eReferralsService.API.EventLogging.Interfaces;

namespace NWRI.eReferralsService.API.EventLogging;

public static class EventCatalogue
{
    [Description("REQ_RECEIVED")]
    public record RequestReceived(
        string Method,
        string Path,
        long? RequestSize) : IAuditEvent;

    [Description("RESP_SENT")]
    public record ResponseSent(
        int StatusCode,
        long LatencyMs) : IAuditEvent;

    [Description("AUTH_CLIENT_VERIFIED")]
    public record AuthClientVerified(
        string? ClientId,
        string? Issuer,
        string? CertificateThumbprint) : IAuditEvent;

    [Description("VAL_PAYLOAD_STARTED")]
    public record PayloadValidationStarted : IAuditEvent;

    [Description("VAL_HEADERS_OK")]
    public record HeadersValidated : IAuditEvent;

    [Description("VAL_FHIR_SCHEMA_OK")]
    public record FhirSchemaValidated : IAuditEvent;

    [Description("MAP_FHIR_TO_WPAS")]
    public record FhirMappedToWpasPayload : IAuditEvent;

    [Description("INT_WPAS_SUCCESS")]
    public record DataSuccessfullyCommittedToWpas(
        long ExecutionTimeMs,
        string? WpasReferralId) : IAuditEvent;

    [Description("AUDIT_REFERRAL_ACCEPTED")]
    public record AuditReferralAccepted(
        string? SourceSystem,
        string? UserRole,
        string? WpasReferralId,
        long ProcessingTimeTotalMs) : IAuditEvent;

    [Description("ERR_AUTH_FAILED")]
    public record AuthFailedError : IErrorEvent;

    [Description("ERR_VAL_MALFORMED_JSON")]
    public record ValMalformedJsonError : IErrorEvent;

    [Description("ERR_VAL_FHIR_VIOLATION")]
    public record ValFhirViolationError : IErrorEvent;

    [Description("ERR_INT_WPAS_TIMEOUT")]
    public record IntWpasTimeoutError : IErrorEvent;

    [Description("ERR_INT_WPAS_CONNECTION_FAIL")]
    public record IntWpasConnectionFailError : IErrorEvent;

    [Description("ERR_INTERNAL_HANDLER")]
    public record InternalHandlerError : IErrorEvent;
}
